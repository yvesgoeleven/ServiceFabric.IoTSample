using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Device.Interfaces;
using DeviceList.Interfaces;
using Microsoft.AspNet.Mvc;
using Microsoft.ServiceFabric.Actors;

namespace Dashboard.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var applicationName = "fabric:/IoTSample";
            var listActor = ActorProxy.Create<IDeviceList>(new ActorId("list"), applicationName);
            var list = await listActor.ListActorIds();
            var deviceInfo = new List<DeviceInfo>();

            foreach (var id in list)
            {
                var device = ActorProxy.Create<IDevice>(new ActorId(id), applicationName);
                var isActive = await device.IsActive();
                deviceInfo.Add(new DeviceInfo()
                {
                    DeviceId = id,
                    State = isActive? "Active" : "Inactive"
                });
            }

            return View(deviceInfo);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }

    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string State { get; set; }
    }
}
