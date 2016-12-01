using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatorLib
{
    public class DB
    {
        public CloudStorageAccount Acct;
        public CloudBlobClient Blb;

        public DB(string cn="DefaultEndpointsProtocol=https;AccountName=mediatorstore;AccountKey=7xDJDWKmjYiPBmbhBSdB4tMVYtNSk1S/T2VRvLYZWiNxQ42Zrpg3RYlT4nfHawA8rSHOsrzYSv5k+yNWPcg1ZQ==;")
        {
            Acct = CloudStorageAccount.Parse(cn);
            Blb = Acct.CreateCloudBlobClient();
        }

        public async Task<CloudBlockBlob> Upload(string cntnr, string file, Stream str)
        {
            CloudBlobContainer container = Blb.GetContainerReference(cntnr);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob f = container.GetBlockBlobReference(file);
            await f.UploadFromStreamAsync(str);
            return f;
        }
    }
}
