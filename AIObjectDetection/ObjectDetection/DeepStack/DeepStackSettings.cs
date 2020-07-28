using Newtonsoft.Json;
namespace AICore.ObjectDetection.DeepStack
{
    class DeepStackSettings
    {
        [JsonProperty("deepStackUrl")]
        public readonly string DeepStackUrl;

        public DeepStackSettings()
        {
            DeepStackUrl = string.Empty;
        }
    }

}
