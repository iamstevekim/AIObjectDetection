using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.ObjectDetection.Data
{
    class Input
    {
        public Input(byte[] imageData, string id, float minConfidence)
        {
            ImageData = imageData;
            Id = id;
            MinConfidence = minConfidence;
        }
        public readonly byte[] ImageData;
        public readonly string Id;
        public readonly float MinConfidence;
    }
}
