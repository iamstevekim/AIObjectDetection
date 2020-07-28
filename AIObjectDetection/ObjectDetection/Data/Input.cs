using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.ObjectDetection.Data
{
    class Input
    {
        public Input(byte[] imageData, string id)
        {
            ImageData = imageData;
            Id = id;
        }
        public readonly byte[] ImageData;
        public readonly string Id;
    }
}
