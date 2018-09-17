#r "System.IO"
#r "System.Runtime"
#r "System.Threading.Tasks"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.WindowsAzure.Storage.Table;

public class ImageText : TableEntity
{
    public string Text { get; set; }
    public string Uri {get; set; }
}

public static async Task Run(ICloudBlob myBlob, ICollector<ImageText> outputTable, TraceWriter log) 
{
    try
    {
        using(var imageFileStream = new MemoryStream())
        {
            myBlob.DownloadToStream(imageFileStream);
            log.Info($"stream length = {imageFileStream.Length}"); // just to verify

            var visionClient = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(@"0b0ab174560140ccaac2bdba3bc85bfb"),
                new System.Net.Http.DelegatingHandler[] { }
            );
            visionClient.Endpoint = "https://westcentralus.api.cognitive.microsoft.com";

            // reset stream position to begining 
            imageFileStream.Seek(0, SeekOrigin.Begin);
            // Upload an image and perform OCR
            var textHeaders = await visionClient.RecognizeTextInStreamAsync(imageFileStream, TextRecognitionMode.Printed);

            var recognitionResult = await GetOcrResultsAsync(visionClient, textHeaders.OperationLocation, log);
            log.Info($"Recognized {recognitionResult.Lines.Count} lines of text");

            var saveText = ExtractText(recognitionResult.Lines);

            outputTable.Add(new ImageText()
                            {
                                PartitionKey = "OcrFunctions",
                                RowKey = myBlob.Name,
                                Text = saveText,
                                Uri = myBlob.Uri.ToString()
                            });            
        }
    }
    catch(Exception ex)
    {
        log.Info($"ex=\r\n{ex.ToString()}");
    }
}

static async Task<RecognitionResult> GetOcrResultsAsync(
    ComputerVisionClient computerVision, string operationLocation, TraceWriter log)
{
     const int numberOfCharsInOperationId = 36;
    // Retrieve the URI where the recognized text will be
    // stored from the Operation-Location header
    string operationId = operationLocation.Substring(
        operationLocation.Length - numberOfCharsInOperationId);

    log.Info("Calling GetTextOperationResultAsync()");

    TextOperationResult result =
                await computerVision.GetTextOperationResultAsync(operationId);

    // Wait for the operation to complete
    int i = 0;
    int maxRetries = 10;
    while ((result.Status == TextOperationStatusCodes.Running ||
            result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
    {
        log.Info($"Server status: {result.Status}, waiting {i} seconds...");
        await Task.Delay(1000);

        result = await computerVision.GetTextOperationResultAsync(operationId);
    }
    return result.RecognitionResult;
}

static string ExtractText(IList<Line> lines)
{
    StringBuilder stringBuilder = new StringBuilder();
    foreach (Line line in lines)
    {
        stringBuilder.Append(line.Text);
        stringBuilder.Append("\r\n");
    }
    return stringBuilder.ToString();
}