using Microsoft.ServiceBus.Messaging;

namespace EventHubProcessorService
{
    public class EventProcessorFactory : IEventProcessorFactory
    {
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor();
        }
    }
}