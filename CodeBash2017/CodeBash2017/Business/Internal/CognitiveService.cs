using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using EPiServer.Core;
using Newtonsoft.Json;

namespace CodeBash2017.Business.Internal
{
    public class CognitiveService : IDisposable
    {
        public BingComputerVisionResponse AnalyzeImage(IContentMedia image)
        {
            using (var httpClient = new HttpClient())
            {
                // Using Bing Computer Vision service to look for cat images.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingComputerVisionKey"]);

                return BingComputerVision(image, httpClient);
            }
        }

        private static BingComputerVisionResponse BingComputerVision(IContentMedia image, HttpClient httpClient)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["visualFeatures"] = string.Join(",", 
                "Categories",
                "Tags",
                "Description",
                "Faces",
                "ImageType",
                "Color",
                "Adult"
            );
            queryString["details"] = "Celebrities";
            queryString["language"] = "en"; // Note: Only English (en) and Simplified Chinese (zh) are supported.

            var uri = $"https://api.projectoxford.ai/vision/v1.0/analyze?{queryString}";

            byte[] byteData;
            using (var xstream = image.BinaryData.OpenRead())
            {
                byteData = new byte[xstream.Length];
                xstream.Read(byteData, 0, (int)xstream.Length);
            }

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = httpClient.PostAsync(uri, content).Result;
                var jsonString = response.Content.ReadAsStringAsync().Result;
                var deserialized = JsonConvert.DeserializeObject<BingComputerVisionResponse>(jsonString);

                return deserialized;
            }
        }

        public void Dispose()
        {
        }
    }
}