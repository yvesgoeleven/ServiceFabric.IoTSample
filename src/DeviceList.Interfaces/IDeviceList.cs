using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace DeviceList.Interfaces
{
    public interface IDeviceList : IActor
    {
        Task<string[]> ListActorIds();

        Task Add(string actorId);
    }
}
