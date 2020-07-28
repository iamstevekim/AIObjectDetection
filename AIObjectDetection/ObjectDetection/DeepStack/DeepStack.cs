using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using AICore.ObjectDetection.Data;

namespace AICore.ObjectDetection.DeepStack
{
    class DeepStackClient : IObjectDetection
    {
        static HttpClient client = new HttpClient();
        private string DeepStackUrl;

        public DeepStackClient(DeepStackSettings settings) : this(settings.DeepStackUrl) { }

        public DeepStackClient(string deepStackUrl)
        {
            DeepStackUrl = deepStackUrl;
        }

        public Task<Output[]> ProcessImageAsync(Input input)
        {
            throw new NotImplementedException();
        }

        public Output[] ProcessImage(Input input)
        {
            Response response = ObjectDetectionAsync(input.ImageData, input.Id).Result;

            System.Collections.Generic.List<Output> output = new System.Collections.Generic.List<Output>();
            if (!response.success)
            {
                return output.ToArray();
            }

            foreach (Object obj in response.predictions)
            {
                Output o = new Output(obj.label, obj.confidence);
                o.X1 = obj.x_min;
                o.Y1 = obj.y_min;
                o.X2 = obj.x_max;
                o.Y2 = obj.y_max;
                output.Add(o);
            }

            return output.ToArray();
        }

        private async Task<Response> ObjectDetectionAsync(byte[] imageData, string imageFileName)
        {
            MultipartFormDataContent request = new MultipartFormDataContent();
            request.Add(new StreamContent(new MemoryStream(imageData)), "image", imageFileName);

            Response response;
            try
            {
                HttpResponseMessage postResponse = await client.PostAsync(DeepStackUrl, request);
                string jsonStr = await postResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<Response>(jsonStr);
            }
            catch
            {
                // Should do something useful with the error
                response = null;
            }
            return response;
        }

        public async Task<string> FaceDetectionAsync()
        {
            return "Not Implemented";
        }

        public async Task<string> SceneDetectionAsync()
        {
            return "Not Implemented";
        }
    }
}
