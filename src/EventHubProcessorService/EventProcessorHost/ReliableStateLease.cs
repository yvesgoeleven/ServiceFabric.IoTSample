using Microsoft.ServiceBus.Messaging;

namespace EventHubProcessorService.EventProcessorHost
{
    public class ReliableStateLease : Lease
    {
        public string LeaseId { get; set; }
    }
}
