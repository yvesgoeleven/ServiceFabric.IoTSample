using Microsoft.ServiceFabric.Actors;

namespace Device.Interfaces
{
    public interface IDeviceActivityEvents : IActorEvents
    {
        void DeviceActivityDetected(ActorId actorId, DeviceDetails details);

        void DeviceInactivityDetected(ActorId actorId, DeviceDetails details);
    }
}