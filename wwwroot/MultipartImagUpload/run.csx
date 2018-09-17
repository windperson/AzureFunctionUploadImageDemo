#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Blob;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, Stream outputBlob, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    HttpResponseMessage result = null; 

    if(req.Content.IsMimeMultipartContent())
    {
        // memory stream of the incomping request 
        var streamProvider = new System.Net.Http.MultipartMemoryStreamProvider();

        log.Info($" ***\t before await on ReadMultpart...");
        await req.Content.ReadAsMultipartAsync(streamProvider);
        log.Info($" ***\t after await on ReadMultpart...");

        //using a stream saves the 'last' iamge if multiple are uplaoded
        foreach(var content in streamProvider.Contents)
        {
            var stream = await content.ReadAsStreamAsync();
            log.Info($"stream length = {stream.Length}"); //just to verify

            // save the stream to output blob, which will save it to Azure stroage blob
            stream.CopyTo(outputBlob);
        }
        result = req.CreateResponse(HttpStatusCode.OK,"uploaded");
    }
    else
    {
        log.Info($" ***\t ERROR!!! bad format request ");
        result = req.CreateResponse(HttpStatusCode.NotAcceptable,"This request is not properly formatted");
    }

    return result;
}
