using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.ObjectDetection.DeepStack
{
    class Object
    {
        public string label { get; set; }
        public float confidence { get; set; }
        public int y_min { get; set; }
        public int x_min { get; set; }
        public int y_max { get; set; }
        public int x_max { get; set; }
    }
}
