using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;

namespace IOTHubMessageProcessor
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Event Hub-compatible endpoint
        // az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT Hub name}
        private readonly static string s_eventHubsCompatibleEndpoint = "sb://xxx.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=NmUSMzkzk1KqEmirTGemTeR/ff2CDmV09RNptaxBHDo=";

        // Event Hub-compatible name
        // az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT Hub name}
        private readonly static string s_eventHubsCompatiblePath = "iothub-ehub-xxxx-xxx-1b15c2c1d5";


        // az iot hub policy show --name iothubowner --query primaryKey --hub-name {your IoT Hub name}
        private readonly static string s_iotHubSasKey = "xxxxx/ff2CDmV09RNptaxBHDo=";
        private readonly static string s_iotHubSasKeyName = "iothubowner";
        private static EventHubClient s_eventHubClient;

        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();


            // Create an EventHubClient instance to connect to the
            // IoT Hub Event Hubs-compatible endpoint.
            var connectionString = new EventHubsConnectionStringBuilder(new Uri(s_eventHubsCompatibleEndpoint), s_eventHubsCompatiblePath, s_iotHubSasKeyName, s_iotHubSasKey);
            s_eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());

            // Create a PartitionReciever for each partition on the hub.
            var runtimeInfo = s_eventHubClient.GetRuntimeInformationAsync();
            var d2cPartitions = runtimeInfo.Result.PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }

            // Wait for all the PartitionReceivers to finsih.
            Task.WaitAll(tasks.ToArray());
        }

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            // Create the receiver using the default consumer group.
            // For the purposes of this sample, read only messages sent since 
            // the time the receiver is created. Typically, you don't want to skip any messages.
            var eventHubReceiver = s_eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
            Console.WriteLine("Create receiver on partition: " + partition);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine("Listening for messages on: " + partition);
                // Check for EventData - this methods times out if there is nothing to retrieve.
                var events = await eventHubReceiver.ReceiveAsync(100);

                // If there is data in the batch, process it.
                if (events == null) continue;

                foreach (EventData eventData in events)
                {
                    string data = Encoding.UTF8.GetString(eventData.Body.Array);
                    Console.WriteLine("Message received on partition {0}:", partition);
                    Console.WriteLine("  {0}:", data);
                    Console.WriteLine("Application properties (set by device):");
                    foreach (var prop in eventData.Properties)
                    {
                        Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                    }
                    Console.WriteLine("System properties (set by IoT Hub):");
                    foreach (var prop in eventData.SystemProperties)
                    {
                        Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                    }
                }
            }
        }

    }
}
