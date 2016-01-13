using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Newtonsoft.Json;

namespace EventHubProcessorService.EventProcessorHost
{
    public class ReliableStateLeaseRepository
    {
        private const string KeyFormat = "_lease-{0}-{1}-{2}-{3}";
        private readonly IReliableStateManager _stateManager;
        private IReliableDictionary<string, string> _leases;

        public ReliableStateLeaseRepository(IReliableStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public async Task<Lease> GetOrCreateAsync(string serviceBusNamespace, string consumerGroupName, string eventHubName, string partitionId)
        {
            await EnsureLeasesDictionaryExists();

            var leaseId = string.Format(KeyFormat, serviceBusNamespace, consumerGroupName, eventHubName, partitionId);
            using (var tx = _stateManager.CreateTransaction())
            {
                var result = await _leases.TryGetValueAsync(tx, leaseId);
                var lease = result.HasValue ? JsonConvert.DeserializeObject<ReliableStateLease>(result.Value) : new ReliableStateLease { LeaseId = leaseId, PartitionId = partitionId};
                await tx.CommitAsync();
                return lease;
            }
        }

        public async Task SaveAsync(ReliableStateLease lease)
        {
            await EnsureLeasesDictionaryExists();

            using (var tx = _stateManager.CreateTransaction())
            {
                var json = JsonConvert.SerializeObject(lease);
                await _leases.AddOrUpdateAsync(tx, lease.LeaseId, json, (key, val) => json);
                await tx.CommitAsync();
            }
        }


        private async Task EnsureLeasesDictionaryExists()
        {
            if (_leases == null)
            {
                _leases = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("leases");
            }
        }
    }
}