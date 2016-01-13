using Device.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Fabric;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DeviceList.Interfaces;

namespace Device
{
    internal class Device : StatefulActor<Device.DeviceState>, IDevice, IRemindable
    {
        [DataContract]
        internal sealed class DeviceState
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Description { get; set; }

            [DataMember]
            public DateTime? LastActivityDate { get; set; }

            [DataMember]
            public bool IsActive { get; set; }

        }

        protected override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                // This is the first time this actor has ever been activated.
                // Set the actor's initial state values.
                this.State = new DeviceState
                {
                    Name = "Unknown device",
                    Description = ""
                };

                var deviceList = ActorProxy.Create<IDeviceList>(new ActorId("list"));
                deviceList.Add(Id.GetStringId());
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }


        [Readonly] // improves performance by not performing serialization and replication of the actor's state.
        public Task<DeviceDetails> GetDetails()
        {
            var details = new DeviceDetails()
            {
                Name = State.Name,
                Description = State.Description
            };
            return Task.FromResult(details);
        }

        public Task SetDetails(DeviceDetails details)
        {
            State.Name = details.Name;
            State.Description = details.Description;
            return Task.FromResult(true);
        }

        [Readonly] // improves performance by not performing serialization and replication of the actor's state.
        Task<bool> IDevice.IsActive()
        {
            return Task.FromResult(State.IsActive);
        }

        Task IDevice.MarkAsActive()
        {
            State.LastActivityDate = DateTime.UtcNow;
            var wasActive = State.IsActive;
            State.IsActive = true;

            if(!wasActive)
            {
               return this.RegisterReminderAsync(
               "DeviceActivityDetection",
               null,
               TimeSpan.FromSeconds(5),
               TimeSpan.FromSeconds(5),
               ActorReminderAttributes.None);
            }

            return Task.FromResult(true);
        }

        private Task DetectActivity()
        {
            var wasActive = State.IsActive;
            var isActive = !(State.LastActivityDate < DateTime.UtcNow.AddSeconds(-30));

            State.IsActive = isActive;

            var events = GetEvent<IDeviceActivityEvents>();

            if (wasActive && !isActive)
            {
                events.DeviceInactivityDetected(this.Id, new DeviceDetails()
                {
                    Name = State.Name,
                    Description = State.Description
                });

                return UnregisterReminderAsync();
            }


            if (isActive && !wasActive)
            {
                events.DeviceActivityDetected(this.Id, new DeviceDetails()
                {
                    Name = State.Name,
                    Description = State.Description
                });
            }

            return Task.FromResult(true);
        }

        public Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            switch (reminderName)
            {
                case "DeviceActivityDetection":
                    return this.DetectActivity();

                default:
                    throw new InvalidOperationException("Unknown reminder: " + reminderName);
            }
        }

        private Task UnregisterReminderAsync()
        {
            IActorReminder reminder;
            try
            {
                reminder = this.GetReminder("DeviceActivityDetection");
            }
            catch (FabricException)
            {
                reminder = null;
            }

            return (reminder == null) ? Task.FromResult(true) : this.UnregisterReminderAsync(reminder);
        }
    }
}
