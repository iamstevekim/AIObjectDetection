using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace AICore.Cameras
{
    class CameraSettings
    {
        [JsonProperty("cameras")]
        public readonly Camera[] Cameras;
        public CameraSettings() { }

    }
}
