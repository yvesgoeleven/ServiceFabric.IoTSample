using DeviceList.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceList
{
    /// <remarks>
    /// Each ActorID maps to an instance of this class.
    /// The IProjName  interface (in a separate DLL that client code can
    /// reference) defines the operations exposed by ProjName objects.
    /// </remarks>
    internal class DeviceList : StatefulActor<DeviceList.ActorState>, IDeviceList
    {
        [DataContract]
        internal sealed class ActorState
        {
            [DataMember]
            public List<string> Ids { get; set; }
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new ActorState { Ids =  new List<string>() };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }


        [Readonly]
        Task<string[]> IDeviceList.ListActorIds()
        {
            return Task.FromResult(State.Ids.ToArray());
        }

        Task IDeviceList.Add(string actorId)
        {
            State.Ids.Add(actorId);

            return Task.FromResult(true);
        }
    }
}
