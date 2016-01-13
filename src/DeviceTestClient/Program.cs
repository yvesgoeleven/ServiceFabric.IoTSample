using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device.Interfaces;
using Microsoft.ServiceFabric.Actors;

namespace DeviceTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var actorId = new ActorId("49a0400b-3cc1-478b-9db3-9ccb267d37f9");
            var applicationName = "fabric:/IoTSample";

            var device = ActorProxy.Create<IDevice>(actorId, applicationName);
            await device.SubscribeAsync(new DeviceActivityEventsHandler());

            var x = "";
            Console.WriteLine("Hit a to activate the device or x to exit.");

            while (x != "x")
            {
                if (x == "a")
                {
                    await device.MarkAsActive();
                }

                var isActive = await device.IsActive();

                Console.WriteLine("Device is " + (isActive ? "active" : "inactive") );
                
                x = Console.ReadLine();
            }
            
        }
    }

    class DeviceActivityEventsHandler : IDeviceActivityEvents
    {
        public void DeviceActivityDetected(ActorId actorId, DeviceDetails details)
        {
            Console.WriteLine($"Device {actorId} became active");
        }

        public void DeviceInactivityDetected(ActorId actorId, DeviceDetails details)
        {
            Console.WriteLine($"Device {actorId} became inactive");
        }
    }
}
