using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.ObjectDetection.Data
{
    class Output
    {
        public readonly string Label;
        public readonly float Confidence;
        public float X1;
        public float Y1;
        public float X2;
        public float Y2;

        public Output(string label, float confidence)
        {
            Label = label;
            Confidence = confidence;
        }
    }
}
