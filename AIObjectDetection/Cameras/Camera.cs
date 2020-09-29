using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AICore.Cameras
{
    class Camera
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("prefix")]
        public string Prefix;
        [JsonProperty("minConfidence")]
        public float MinConfidence;
        [JsonProperty("falsePositives")]
        public ImageProcessing.FalsePositive[] InternalFalsePositives
        {
            set
            {
                foreach (ImageProcessing.FalsePositive falsePositive in value)
                {
                    if (!FalsePositives.ContainsKey(falsePositive.Label))
                        FalsePositives.Add(falsePositive.Label, new List<ImageProcessing.FalsePositive>());

                    FalsePositives[falsePositive.Label].Add(falsePositive);
                }
            }
        }

        public SortedList<string, List<ImageProcessing.FalsePositive>> FalsePositives;
        public ObjectDetection.Data.Output[] LastDetectedObjs;

        public Camera()
        {
            FalsePositives = new SortedList<string, List<ImageProcessing.FalsePositive>>();
            LastDetectedObjs = new ObjectDetection.Data.Output[0];
        }
    }
}
