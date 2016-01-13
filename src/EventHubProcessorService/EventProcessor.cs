using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Device.Interfaces;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;

namespace EventHubProcessorService
{
    public class EventProcessor : IEventProcessor
    {
        Subject<dynamic> pump = new Subject<dynamic>();

        private static readonly TimeSpan ActivityDetectionSpan = TimeSpan.FromSeconds(5);
        private Func<IObservable<dynamic>, IObservable<dynamic>> query = messages =>
            from m in messages
            where m.deviceId != null
            group m by m.deviceId
                into g
            from b in g.Buffer(ActivityDetectionSpan, ActivityDetectionSpan)
            where b.Count > 0
            select b.First();

        public Task OpenAsync(PartitionContext context)
        {
            var subscription = query(pump);
            subscription.Subscribe(async d =>
            {
                string deviceId = d.deviceId;
                var actorId = new ActorId(deviceId);
                var device = ActorProxy.Create<IDevice>(actorId);
                await device.MarkAsActive();
            });

            return Task.FromResult(0);
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            return Task.FromResult(0);
        }
       

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var raw in messages)
            {
                ServiceEventSource.Current.Message("Processing message");

                var deserialized = Deserialize(raw);
                if (deserialized != null)
                {
                    pump.OnNext(deserialized);
                }
            }

            await context.CheckpointAsync();
        }

        private dynamic Deserialize(EventData raw)
        {
            try
            {
                var body = Encoding.UTF8.GetString(raw.GetBytes());
                return JsonConvert.DeserializeObject<dynamic>(body);
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}
