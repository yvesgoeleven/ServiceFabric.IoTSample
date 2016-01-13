using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using EventHubProcessorService.EventProcessorHost;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace EventHubProcessorService
{
    public class EventHubCommunicationListener : ICommunicationListener
    {
        private MessagingFactory _messagingFactory;
        private readonly string _connectionString;
        private readonly string _eventHubName;
        private EventHubClient _eventHubClient;
        private readonly string _consumerGroupName;
        private EventHubConsumerGroup _consumerGroup;
        private EventProcessorFactory _eventProcessorFactory;
        private CheckpointManager _checkpointManager;
        private readonly IReliableStateManager _reliableStateManager;
        private readonly Uri _serviceName;
        private readonly Guid _partitionId;
        private ReliableStateLeaseRepository _leaseRepository;

        public EventHubCommunicationListener(string connectionString, string eventHubName, string consumerGroupName, IReliableStateManager reliableStateManager, Uri serviceName, Guid partitionId)
        {
            _connectionString = connectionString;
            _eventHubName = eventHubName;
            _consumerGroupName = consumerGroupName;
            _reliableStateManager = reliableStateManager;
            _serviceName = serviceName;
            _partitionId = partitionId;
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var builder = new ServiceBusConnectionStringBuilder(_connectionString)
            {
                TransportType = TransportType.Amqp
            };
            _messagingFactory = MessagingFactory.CreateFromConnectionString(builder.ToString());
            _eventHubClient = _messagingFactory.CreateEventHubClient(_eventHubName);
            _consumerGroup = !string.IsNullOrEmpty(_consumerGroupName)
                ? _eventHubClient.GetConsumerGroup(_consumerGroupName)
                : _eventHubClient.GetDefaultConsumerGroup();

            _eventProcessorFactory = new EventProcessorFactory();
            _leaseRepository = new ReliableStateLeaseRepository(_reliableStateManager);
            _checkpointManager = new CheckpointManager(_leaseRepository); 

            var allocatedPartitions = await new EventHubPartitionPartitionAllocationStrategy(_serviceName, _partitionId)
                .AllocateAsync(_eventHubClient, new FabricClient());

            foreach (var partition in allocatedPartitions)
            {
                var lease = await _leaseRepository.GetOrCreateAsync(_connectionString, _consumerGroupName, _eventHubName, partition);

                await _consumerGroup.RegisterProcessorFactoryAsync(lease, _checkpointManager, _eventProcessorFactory);
            }

            return string.Concat(_eventHubName, " @ ", _connectionString);
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (_messagingFactory != null && !_messagingFactory.IsClosed)
            {
               await _messagingFactory.CloseAsync();
            }
        }

        public void Abort()
        {
            if (_messagingFactory != null && !_messagingFactory.IsClosed)
            {
                _messagingFactory.Close();
            }
        }
    }
}