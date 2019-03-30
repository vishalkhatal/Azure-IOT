using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace How_to_upload_File_on_IOT_hub
{
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        private static DeviceClient s_deviceClient;
        private readonly static string s_connectionString = "HostName=devtke.azure-devices.net;DeviceId=ESRT0001;SharedAccessKey=jJeDypYTXq58sfyi45ZrkXwts41pO849wNrlIDkVzL8=";

        static void Main()
        {
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
            SendToBlobAsync();
            Console.ReadKey();
        }
        private static async void SendToBlobAsync()
        {
            string fileName = "Pre.jpg";
            Console.WriteLine("Uploading file: {0}", fileName);
            var watch = System.Diagnostics.Stopwatch.StartNew();

            using (var sourceData = new FileStream(@"Pre.jpg", FileMode.Open))
            {
                await s_deviceClient.UploadToBlobAsync(fileName, sourceData);
            }

            watch.Stop();
            Console.WriteLine("Time to upload file: {0}ms\n", watch.ElapsedMilliseconds);
        }


    }
}
