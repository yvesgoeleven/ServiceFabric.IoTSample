using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace EventHubProcessorService.EventProcessorHost
{
    public class EventHubPartitionPartitionAllocationStrategy
    {
        private readonly Uri _serviceName;
        private readonly Guid _partitionId;

        public EventHubPartitionPartitionAllocationStrategy(Uri serviceName, Guid partitionId)
        {
            _serviceName = serviceName;
            _partitionId = partitionId;
        }

        public async Task<IEnumerable<string>> AllocateAsync(EventHubClient eventHubClient, FabricClient fabricClient)
        {
            var runtimeInformation = await eventHubClient.GetRuntimeInformationAsync();
            var eventHubPartitions = runtimeInformation.PartitionIds.OrderBy(p => p).ToArray();
            var serviceFabricPartitions = (await fabricClient.QueryManager.GetPartitionListAsync(_serviceName))
                                           .Select(p => p.PartitionInformation.Id.ToString())
                                           .OrderBy(p => p).ToArray();

            // when there are more service fabric partitions then eventhub partitions, 
            // just assign them one by one and the remainder of the partitions will stay dormant

            if (serviceFabricPartitions.Length >= eventHubPartitions.Length)
            {
                var index = Array.IndexOf(serviceFabricPartitions, _partitionId.ToString());

                return new [] { eventHubPartitions[index] };
            }
            else
            {
                // otherwise distribute eventhub partitions evenly across service fabric partitions

                var remainder = eventHubPartitions.Length % serviceFabricPartitions.Length;
                var numberOfEventHubPartitionsPerServiceFabricPartition = eventHubPartitions.Length / serviceFabricPartitions.Length;
                if (remainder > 0)  numberOfEventHubPartitionsPerServiceFabricPartition++;
                var index = Array.IndexOf(serviceFabricPartitions, _partitionId.ToString());

                var allocated = new List<string>();
                for (var i = 0; i < numberOfEventHubPartitionsPerServiceFabricPartition; i++)
                {
                    var idx = (index*numberOfEventHubPartitionsPerServiceFabricPartition) + i;
                    if (eventHubPartitions.Length >= idx)
                    {
                        allocated.Add(eventHubPartitions[idx]);
                    }
                }

                return allocated.ToArray();
            }
        }
    }
}