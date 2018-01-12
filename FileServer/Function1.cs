
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace FileServer1
{
    public static class Function1
    {
        public class ImageInfo
        {
            public string Directory { get; set; }
            public string File { get; set; }
            public int Size { get; set; }
            public int DownloadCount { get; set; }
            public bool DownloadPermitted { get; set; }
        };

        [FunctionName("DownloadFile")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "DownloadFile/{file}")]HttpRequest req,
            [Blob("storedata/StockImages", FileAccess.Read, Connection = "imageStorage")]CloudBlobDirectory blobDirectory,
            [Table("fileserver", Connection = "imageStorage")]CloudTable fileserver,
            string file,
            TraceWriter log)
        {
            var pk = "images";

            // force the input to be a guid
            if (!Guid.TryParse(file, out var fileGuid))
            {
                return new BadRequestResult();
            }

            var fg = fileGuid.ToString();

            var key = TableQuery.GenerateFilterCondition("PartitionKey", "eq", pk);
            var row = TableQuery.GenerateFilterCondition("RowKey", "eq", file);
            var allowed = TableQuery.GenerateFilterConditionForBool("DownloadPermitted", "eq", true);
            var queryCondition = TableQuery.CombineFilters(TableQuery.CombineFilters(key, "and", row), "and", allowed);
            var query = new TableQuery().Where(queryCondition);

            try
            {
                var entries = (await fileserver.ExecuteQuerySegmentedAsync(query, Resolver.From<ImageInfo>(), null));
                var entry = entries.FirstOrDefault();

                if (entry == null)
                {
                    log.Error($"no reference found for {file}");
                    return new BadRequestResult();
                }

                return await GetFile(blobDirectory, entry.Directory, entry.File, log);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [FunctionName("CreateEntry")]
        public async static Task<IActionResult> CreateEntry([HttpTrigger(AuthorizationLevel.Function, "put", Route = "api/CreateEntry")]HttpRequest req,
            [Table("fileserver", Connection = "imageStorage")]CloudTable fileserver,
            TraceWriter log)
        {
            var pk = "images";


            await fileserver.ExecuteAsync(TableOperation.Insert(
                new TableEntityAdapter<ImageInfo>(new ImageInfo { Directory = "No Directory Set", File = "no file set" }, pk, Guid.NewGuid().ToString())
                                ));

            return new OkResult();
        }

        private async static Task<IActionResult> GetFile(CloudBlobDirectory blobDirectory, string directory, string filename, TraceWriter log)
        {
            log.Info($"Getting file: {directory}/{filename}");
            var file = blobDirectory.GetBlockBlobReference($"{directory}/{filename}");

            if (!await file.ExistsAsync())
            {
                return await Task.FromResult<IActionResult>(new BadRequestResult());
            }

            await file.FetchAttributesAsync();
            var name = file.Name.Split('/').LastOrDefault();

            var stream = new MemoryStream();
            await file.DownloadToStreamAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(stream, file.Properties.ContentType) { FileDownloadName = filename };
        }
    }
}
