using System.IO;
using Newtonsoft.Json;

namespace AICore.ObjectDetection.TensorFlowSharp
{
    class TensorFlowSharpSettings
    {
        [JsonProperty("modelDataFilePath")]
        public readonly string ModelDataFilePath;
        [JsonProperty("inputParameter")]
        public readonly string InputParameter;
        [JsonProperty("outputParameters")]
        public readonly string[] OutputParameters;
        [JsonProperty("labelsFilePath")]
        public readonly string LabelsFilePath;
        [JsonProperty("processingImgWidth")]
        public readonly int ProcessingImgWidth;
        [JsonProperty("processingImgHeight")]
        public readonly int ProcessingImgHeight;
        [JsonProperty("minimumConfidence")]
        public readonly float MinimumConfidence;
        [JsonProperty("interestedLabelsFilePath")]
        public readonly string InterestedLabelsFilePath;

        public byte[] ModelData => File.ReadAllBytes(ModelDataFilePath);
        public string[] Labels => File.ReadAllLines(LabelsFilePath);
        public string[] InterestedLabels => File.ReadAllLines(InterestedLabelsFilePath);

        private static string EscapeFilePath(string filePath)
        {
            return filePath.Replace("\\", "\\\\");
        }

        public TensorFlowSharpSettings()
        {
            ModelDataFilePath = string.Empty;
            InputParameter = string.Empty;
            OutputParameters = new string[] { };
            LabelsFilePath = string.Empty;
            ProcessingImgWidth = 0;
            ProcessingImgHeight = 0;
            MinimumConfidence = 0f;
            InterestedLabelsFilePath = string.Empty;
        }
    }
}
