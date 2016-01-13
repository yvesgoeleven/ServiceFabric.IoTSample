using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace Device.Interfaces
{
    public interface IDevice : IActor, IActorEventPublisher<IDeviceActivityEvents>
    {
        Task<DeviceDetails> GetDetails();

        Task SetDetails(DeviceDetails details);

        Task<bool> IsActive();

        Task MarkAsActive();
    }
}
