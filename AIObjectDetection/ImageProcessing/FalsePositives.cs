using Newtonsoft.Json;

namespace AICore.ImageProcessing
{
    class FalsePositives
    {
        [JsonProperty("falsePositives")]
        public readonly FalsePositive[] Items;

        public FalsePositives()
        {
            Items = new FalsePositive[] { };
        }
    }

    class FalsePositive
    {
        [JsonProperty("label")]
        public readonly string Label;
        [JsonProperty("originalWidth")]
        public readonly int OriginalWidth;
        [JsonProperty("originalHeight")]
        public readonly int OriginalHeight;
        [JsonProperty("x1")]
        public readonly int X1;
        [JsonProperty("y1")]
        public readonly int Y1;
        [JsonProperty("x2")]
        public readonly int X2;
        [JsonProperty("y2")]
        public readonly int Y2;

        private int InternalArea = -1;
        public int Area
        {
            get
            {
                if (InternalArea == -1)
                {
                    var w = X2 - X1;
                    var h = Y2 - Y1;
                    InternalArea = w * h;
                }

                return InternalArea;
            }
        }
        public FalsePositive()
        {
            Label = string.Empty;
            OriginalWidth = 0;
            OriginalHeight = 0;
            X1 = 0;
            Y1 = 0;
            X2 = 0;
            Y2 = 0;
        }
    }
}
