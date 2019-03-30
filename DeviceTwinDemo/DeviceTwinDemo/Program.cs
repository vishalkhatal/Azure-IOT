using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTwinDemo
{
    class Program
    {
        static RegistryManager registryManager;
        static string connectionString = "IOT hub connection string";
        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddTagsAndQuery().Wait();
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
        public static async Task AddTagsAndQuery()
        {
            var twin = await registryManager.GetTwinAsync("MyDotnetDevice");
            var patch =
                @"{
             tags: {
                 location: {
                     region: 'India',
                     plant: 'Hyderabad'
                 }
             }
         }";
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);

            var query = registryManager.CreateQuery("SELECT * FROM devices WHERE tags.location.plant = 'Hyderabad'", 100);
            var twinsInHyderabad = await query.GetNextAsTwinAsync();
            Console.WriteLine("Devices in Hyderabad: {0}", string.Join(", ", twinsInHyderabad.Select(t => t.DeviceId)));
          }
    }
}
