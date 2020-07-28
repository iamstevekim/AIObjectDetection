using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.ObjectDetection.DeepStack
{
    class Response
    {
        public bool success { get; set; }
        public Object[] predictions { get; set; }
    }
}
