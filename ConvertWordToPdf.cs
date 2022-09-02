using Azure.Storage.Blobs;
using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using GemBox.Document;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ConvertToPdf.models;
using System.Net;
using Newtonsoft.Json;

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

        [FunctionName("test-save")]
        [ProducesResponseType(typeof(TestResponse), (int) HttpStatusCode.OK)]
        public static async Task <IActionResult> TestSave(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            [RequestBodyType(typeof(TestRequest), "request")] HttpRequest req, ILogger log) {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TestRequest data = JsonConvert.DeserializeObject <TestRequest> (requestBody);
            var responseMessage = new TestResponse {
                Id = Guid.NewGuid(), TestData = data
            };
            return new OkObjectResult(responseMessage);
        }

        [SwaggerIgnore]
        [FunctionName("Swagger")]
        public static Task <HttpResponseMessage> Swagger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/json")] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swasBuckleClient) 
        {
            return Task.FromResult(swasBuckleClient.CreateSwaggerJsonDocumentResponse(req));
        }
        
        [SwaggerIgnore]
        [FunctionName("SwaggerUI")]
        public static Task < HttpResponseMessage > SwaggerUI(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swasBuckleClient) 
        {
            return Task.FromResult(swasBuckleClient.CreateSwaggerUIResponse(req, "swagger/json"));
        }
    }
}
