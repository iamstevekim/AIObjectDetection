using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace AICore.ObjectDetection
{
    enum ObjectDetectionType
    {
        DeepStack,
        TensorFlowSharp
    }
    class ObjectDetectionSettings
    {
        [JsonProperty("type")]
        public readonly ObjectDetectionType Type;
        [JsonProperty("deepStackSettings")]
        public readonly DeepStack.DeepStackSettings DeepStackSettings;
        [JsonProperty("tensorFlowSharpSettings")]
        public readonly TensorFlowSharp.TensorFlowSharpSettings TensorFlowSharpSettings;

        public ObjectDetectionSettings()
        {

        }
    }
}
