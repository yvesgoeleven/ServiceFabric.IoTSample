using System;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventHubProcessorService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class EventHubProcessorService : StatefulService
    {
        private const string ConnectionString = "";

        private const string EventHubName = "IoTSample";
        private const string ConsumerGroupName = null;

        private CompositeCommunicationListener compositeListener;

        public EventHubProcessorService()
        {
            this.compositeListener = new CompositeCommunicationListener();
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service replica.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    parameters => compositeListener, "eventhubs")
            };
            //return new[]
            //{
            //    new ServiceReplicaListener(
            //        parameters => new EventHubCommunicationListener(
            //            ConnectionString,
            //            EventHubName,
            //            ConsumerGroupName,
            //            StateManager,
            //            ServiceInitializationParameters.ServiceName,
            //            ServiceInitializationParameters.PartitionId
            //            ), "eventhub"
            //        ),
            //};
        }

        /// <summary>
        /// This is the main entry point for your service's partition replica. 
        /// RunAsync executes when the primary replica for this partition has write status.
        /// </summary>
        /// <param name="cancelServicePartitionReplica">Canceled when Service Fabric terminates this partition's replica.</param>
        protected override async Task RunAsync(CancellationToken cancelServicePartitionReplica)
        {
            var name = string.Concat(EventHubName, "-", ConnectionString);
            var eventHubListener = new EventHubCommunicationListener(
                ConnectionString,
                EventHubName,
                ConsumerGroupName,
                StateManager,
                ServiceInitializationParameters.ServiceName,
                ServiceInitializationParameters.PartitionId
                );

            await this.compositeListener.AddListenerAsync(name, eventHubListener);

            while (!cancelServicePartitionReplica.IsCancellationRequested)
            {

                await Task.Delay(TimeSpan.FromSeconds(1), cancelServicePartitionReplica);
            }
        }
    }
}
