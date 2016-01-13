using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace TelemetryTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ServiceBusConnectionStringBuilder("");
            builder.TransportType = TransportType.Amqp;
            
            var factory = MessagingFactory.CreateFromConnectionString(builder.ToString());
            var client = factory.CreateEventHubClient("iotsample");

            Console.WriteLine("Hit enter to send a batch of messages, x to exit");
            var x = Console.ReadLine();

            while (x != "x")
            {
                var random = new Random();
                var messages = new List<EventData>();
                for (int i = 0; i < 1000; i++)
                {
                    var message = new
                    {
                        deviceId = random.Next(0, 1)
                    };
                    var body = JsonConvert.SerializeObject(message);
                    messages.Add(new EventData(Encoding.UTF8.GetBytes(body)));
                }

                client.SendBatch(messages);

                Console.WriteLine("Batch sent, hit enter to send the next batch, x to exit");
                x = Console.ReadLine();
            }

        }
    }
}
