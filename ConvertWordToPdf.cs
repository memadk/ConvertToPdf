using Azure.Storage.Blobs;
using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using GemBox.Document;

namespace memadk.Function
{
    public class ConvertWordToPdf
    {
        [FunctionName("ConvertWordToPdf")]
        public async Task Run(
            [BlobTrigger("documents/{name}")] Stream myBlob,
            string name, 
            ILogger log)
        {
            string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            if(name.EndsWith(".docx"))
            {
                log.LogInformation($"Converting {name} to PDF");
                ComponentInfo.SetLicense("FREE-LIMITED-KEY");
                var document = DocumentModel.Load(myBlob);
                var options = SaveOptions.PdfDefault;
                using (var stream = new MemoryStream())
                {
                    document.Save(stream, options);
                    var pdfName = name.Replace(".docx", ".pdf");
                    var blobClient = new BlobContainerClient(Connection, "documents");
                    var blob = blobClient.GetBlobClient(pdfName);
                    if(await blob.ExistsAsync())
                    {
                        log.LogInformation($"{pdfName} already exists, renaming...");
                        pdfName = pdfName.Replace(".pdf", $"-{DateTime.Now.ToString("yyyyMMddHHmmss")}.pdf");
                        blob = blobClient.GetBlobClient(pdfName);
                        log.LogInformation($"New name: {pdfName}");
                    }
                    await blob.UploadAsync(stream);
                }

            }
            else
            {
                log.LogInformation($"{name} is not a word document");
            }
            
        }
    }
}
