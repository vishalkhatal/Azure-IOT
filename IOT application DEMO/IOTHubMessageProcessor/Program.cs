using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IOTHubMessageProcessor
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Event Hub-compatible endpoint
        // az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT Hub name}
        private readonly static string s_eventHubsCompatibleEndpoint = "sb://iothub-ns-iotkmsdemo-625264-8d8ea15e12.servicebus.windows.net/";

        // Event Hub-compatible name
        // az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT Hub name}
        private readonly static string s_eventHubsCompatiblePath = "iotkmsdemo";


        // az iot hub policy show --name iothubowner --query primaryKey --hub-name {your IoT Hub name}
        private readonly static string s_iotHubSasKey = "rVLanu9awUMulI8abTV/0n1JzXxzj0DqKqope7oaAZM=";
        private readonly static string s_iotHubSasKeyName = "iothubowner";
        private static EventHubClient s_eventHubClient;

        static void Main()
        {
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
            Program webjobObj = new Program();
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
                        if(Convert.ToString(prop.Key)== "MessageType" && Convert.ToString(prop.Value)=="Alert door Open")
                        {
                            webjobObj.handleAlertMessages();
                        }
                    }
                    //Console.WriteLine("System properties (set by IoT Hub):");
                    //foreach (var prop in eventData.SystemProperties)
                    //{
                    //    Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                    //}
                   
                }
            }
        }
        public async void Upload(string filename)
        {
            BlobStorageServices blobStorageServices = new BlobStorageServices();
            CloudBlobContainer blobContainer = blobStorageServices.GetCloudBlobContainer();
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(filename);
            await blob.UploadFromFileAsync(filename);
        }
        public async void handleAlertMessages()
        {
            bool isAttemptSuccessfull = false;
            int attemptNo = 0;
            while (!isAttemptSuccessfull && attemptNo < 3)
            {
                try
                {
                    var result = await SendEmailforDoorOpenNotification();
                    isAttemptSuccessfull = result;
                }
                catch (Exception ex)
                {
                    // logger
                }
                attemptNo++;
            }
        }
        public async Task<bool> SendEmailforDoorOpenNotification()
        {
            bool sentStatus = false;
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("fridgenotification@gmail.com");
                mail.To.Add("iamvishalkhatal@gmail.com");
                mail.Subject = "Alert : Fridge door open";
                mail.Body = "Hi Vishal, Someone keep your frige door open.Please take immediate action by informing family members to close it or use mobile app to send door close command to fridge.";

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("vishalnotesofbe@gmail.com", "a1shal@2232020");
                SmtpServer.EnableSsl = true;
                sentStatus = true;
                SmtpServer.Send(mail);
                Console.WriteLine("Mail sent Successfully to vishal !!!");

            }
            catch (Exception ex)
            {
                sentStatus = false;
            }
            return sentStatus;
        }


    }
}
