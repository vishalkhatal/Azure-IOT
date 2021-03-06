﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace DeviceSimulator
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        private static DeviceClient s_deviceClient;
        private readonly static string s_connectionString = "HostName=IOTKMSDEMO.azure-devices.net;DeviceId=MyDotnetDevice;SharedAccessKey=/+iV5k6Vn+Hz/eIRbwXHFHIru3+0Tmj/s/8cbnvk5x4=";

        static void Main()
        {
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();
            ReceiveC2dAsync();
            Console.ReadKey();
        }
        private static async void SendDeviceToCloudMessagesAsync()
        {
            var noOfMessaheToSend = 10;
            while (noOfMessaheToSend!=0)
            {
                // Create JSON message
                var telemetryDataPoint = new
                {
                    message = "Alert door Open",
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("MessageType", "Alert door Open");

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
                noOfMessaheToSend--;
            }
        }
        private static async void ReceiveC2dAsync()
        {
            Console.WriteLine("\nReceiving cloud to device messages from service");
            while (true)
            {
                Message receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received message: {0}", Encoding.ASCII.GetString(receivedMessage.GetBytes()));
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
            }
        }

    }
}
