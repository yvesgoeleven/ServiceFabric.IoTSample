using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace EventHubProcessorService.EventProcessorHost
{
    public class CheckpointManager : ICheckpointManager
    {
        private readonly ReliableStateLeaseRepository _leaseRepository;

        public CheckpointManager(ReliableStateLeaseRepository leaseRepository)
        {
            _leaseRepository = leaseRepository;
        }

        public async Task CheckpointAsync(Lease lease, string offset, long sequenceNumber)
        {
            var reliableStateLease = lease as ReliableStateLease;
            if (reliableStateLease == null) return;

            reliableStateLease.Offset = offset;
            reliableStateLease.SequenceNumber = sequenceNumber;

            await _leaseRepository.SaveAsync(reliableStateLease);
        }
    }
}
