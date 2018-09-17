#r "System.IO"
#r "System.Runtime"
#r "System.Threading.Tasks"
#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;


// used to get rows from table
public class ImageText : TableEntity
{
    public string Text { get; set; }
    public string Uri {get; set; }
}

public class SimpleImageText
{
    public string Text { get; set; }
    public string Uri {get; set; }
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, IQueryable<ImageText> inputTable, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    var result = new List<SimpleImageText>();

    var query = from ImageText in inputTable select ImageText;
    log.Info($"original query --> {JsonConvert.SerializeObject(query)}");

    foreach (ImageText imageText in query)
    {
        result.Add( new SimpleImageText(){Text = imageText.Text, Uri = imageText.Uri});
        //log.Info($"{JsonConvert.SerializeObject()}");
    }
    log.Info($"list of results --> {JsonConvert.SerializeObject(result)}");

    return req.CreateResponse(HttpStatusCode.OK, result);
}
