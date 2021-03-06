﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTHubMessageProcessor
{
    public class BlobStorageServices
    {
        public CloudBlobContainer GetCloudBlobContainer()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=myportfolio;AccountKey=KM4a2u8MtNbWMaLlV32iofcceQdlAbkx+/rm5euDOebfYydrJf6mbpmaj0SIyOBMw==;EndpointSuffix=core.windows.net");

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("documents");
            if (blobContainer.CreateIfNotExists())
            {
                blobContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });//Allowing public Access  

            }
            return blobContainer;

        }

    }

}
