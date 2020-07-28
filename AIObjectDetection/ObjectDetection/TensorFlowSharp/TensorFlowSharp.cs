using AICore.ObjectDetection.Data;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Linq;
using TensorFlow;

namespace AICore.ObjectDetection.TensorFlowSharp
{
    class TensorFlowSharpClient : IObjectDetection
    {
        private TFGraph Graph;
        private TFSession Session;

        private string[] Labels;
        private int ProcessingImgWidth;
        private int ProcessingImgHeight;
        private float MinThreshold;

        private string InputParameter;
        private string[] OutputParameters;

        private HashSet<string> InterestedLabels;

        public TensorFlowSharpClient(TensorFlowSharpSettings settings) :
            this(settings.ModelData, settings.InputParameter, settings.OutputParameters,
                settings.Labels, settings.ProcessingImgWidth, settings.ProcessingImgHeight,
                settings.MinimumConfidence, settings.InterestedLabels) { }

        public TensorFlowSharpClient(byte[] modelData, string inputParameter, string[] outputParameters, string[] labels,
            int processingImgWidth, int processingImgHeight, float minThreshold, string[] interestedLabels)
        {
            InitializeTensorflow(modelData);

            Labels = labels;
            ProcessingImgWidth = processingImgWidth;
            ProcessingImgHeight = processingImgHeight;
            MinThreshold = minThreshold;

            InputParameter = inputParameter;
            OutputParameters = outputParameters;

            LoadInterestedLabels(interestedLabels);
        }

        private void InitializeTensorflow(byte[] modelData)
        {
            Graph = new TFGraph();
            Graph.Import(new TFBuffer(modelData));
            Session = new TFSession(Graph);
        }

        public async Task<Output[]> ProcessImageAsync(Input input)
        {
            Bitmap image = null;
            using (MemoryStream ms = new MemoryStream(input.ImageData))
            {
                image = new Bitmap(ms);
            }

            TFTensor inputTensor = PrepareInput(image);
            TFTensor[] outputTensors = await Task.Run(() => ProcessImage(inputTensor));
            Output[] objs = ProcessResult(outputTensors);

            if (objs.Length == 0)
                return objs;
            else
                return ReadjustRatio(objs, image.Width, image.Height);
        }

        private TFTensor PrepareInput(Bitmap image)
        {
            var resizedImage = ResizeImage(image, ProcessingImgWidth, ProcessingImgHeight);
            return CreateFloatArrayTensorFromImage(resizedImage);
        }

        private TFTensor[] ProcessImage(TFTensor input)
        {
            var runner = Session.GetRunner();

            runner = runner.AddInput(Graph[InputParameter][0], input);
            foreach (string output in OutputParameters)
            {
                runner.Fetch(Graph[output][0]);
            }

            return runner.Run();
        }

        private Output[] ProcessResult(TFTensor[] result)
        {
            float[,,] matches = (float[,,])result[0].GetValue();
            float[,,] coordinates = (float[,,])result[1].GetValue();

            List<Output> returnObjs = new List<Output>();
            for (int labelIndex = 0; labelIndex < Labels.Length; labelIndex++)
            {
                string labelValue = GetLabel(labelIndex);
                if (IsInterestedLabel(labelValue))
                {
                    SortedList<float, List<float>> labelMatches = new SortedList<float, List<float>>();
                    for (int index = 0; index < result[0].GetTensorDimension(1); index++)
                    {
                        var confidence = matches[0, index, labelIndex];
                        if (confidence > MinThreshold)
                        {
                            // get all necessary values
                            if (!labelMatches.ContainsKey(confidence))
                            {
                                labelMatches.Add(confidence, new List<float>());
                            }
                            labelMatches[confidence].Add(index);
                        }
                    }
                    if (labelMatches.Count > 0)
                    {
                        var uniqueResults = NonMaxSuppression(coordinates, labelValue, labelMatches);
                        returnObjs.AddRange(uniqueResults);
                    }
                }
            }

            return returnObjs.ToArray();
        }

        private Output[] NonMaxSuppression(float[,,] coordinates, string label, SortedList<float, List<float>> confidenceIndex)
        {
            List<Output> returnObjs = new List<Output>();
            List<float> areas = new List<float>();
            SortedList<int, float[]> coordForArea = new SortedList<int, float[]>();
            for (int i = confidenceIndex.Count - 1; i >= 0; i--)
            {
                var confidence = confidenceIndex.Keys[i];
                var indexList = confidenceIndex[confidence];
                for (int j = 0; j < indexList.Count; j++)
                {
                    var index = (int)indexList[j];
                    if (areas.Count == 0)
                    {
                        // No areas exists, immediately add first match
                        float X1 = coordinates[0, index, 0];
                        float Y1 = coordinates[0, index, 1];
                        float X2 = coordinates[0, index, 2];
                        float Y2 = coordinates[0, index, 3];

                        float w = X2 - X1 + 1;
                        float h = Y2 - Y1 + 1;

                        float area = w * h;
                        areas.Add(area);
                        coordForArea.Add(areas.Count - 1, new float[] { X1, Y1, X2, Y2 });

                        var rObj = new Output(label, confidence);
                        rObj.X1 = X1;
                        rObj.Y1 = Y1;
                        rObj.X2 = X2;
                        rObj.Y2 = Y2;
                        returnObjs.Add(rObj);
                    }
                    else
                    {
                        var overlapped = false;
                        for (int k = 0; k < areas.Count; k++)
                        {
                            float[] coords = coordForArea[k];
                            float X1 = System.Math.Max(coordinates[0, index, 0], coords[0]);
                            float Y1 = System.Math.Max(coordinates[0, index, 1], coords[1]);
                            float X2 = System.Math.Min(coordinates[0, index, 2], coords[2]);
                            float Y2 = System.Math.Min(coordinates[0, index, 3], coords[3]);

                            float w = System.Math.Max(0, X2 - X1 + 1);
                            float h = System.Math.Max(0, Y2 - Y1 + 1);

                            float area = w * h;

                            var overlap = area / areas[k];
                            if (overlap > 0.3f)
                            {
                                // duplicates area - ignore
                                overlapped = true;
                                break;
                            }
                        }

                        if (!overlapped)
                        {
                            float X1 = coordinates[0, index, 0];
                            float Y1 = coordinates[0, index, 1];
                            float X2 = coordinates[0, index, 2];
                            float Y2 = coordinates[0, index, 3];

                            float w = X2 - X1 + 1;
                            float h = Y2 - Y1 + 1;

                            float area = w * h;
                            areas.Add(area);
                            coordForArea.Add(areas.Count - 1, new float[] { X1, Y1, X2, Y2 });

                            var rObj = new Output(label, confidence);
                            rObj.X1 = X1;
                            rObj.Y1 = Y1;
                            rObj.X2 = X2;
                            rObj.Y2 = Y2;
                            returnObjs.Add(rObj);
                        }
                    }
                }
            }
            return returnObjs.ToArray();
        }

        private Output[] ReadjustRatio(Output[] objs, int originalWidth, int originalHeight)
        {
            float xRatio = (float)originalWidth / ProcessingImgWidth;
            float yRatio = (float)originalHeight / ProcessingImgHeight;

            foreach (var obj in objs)
            {
                obj.X1 *= xRatio;
                obj.X2 *= xRatio;
                obj.Y1 *= yRatio;
                obj.Y2 *= yRatio;
            }

            return objs;
        }

        private string GetLabel(int value)
        {
            if (Labels.Length > value)
                return Labels[value];
            else
                return string.Empty;
        }

        private void LoadInterestedLabels(string[] interestedLabels)
        {
            if (interestedLabels == null || interestedLabels.Length == 0)
            {
                InterestedLabels = null;
            }
            else
            {
                InterestedLabels = new HashSet<string>();
                foreach (string label in interestedLabels)
                {
                    var unusedResult = AddInterestedLabel(label);
                    //Should indicate if the Label wasn't added
                }
            }
        }

        public bool AddInterestedLabel(string interestedLabel)
        {
            if (Labels.Contains(interestedLabel))
            {
                if (!InterestedLabels.Contains(interestedLabel))
                {
                    InterestedLabels.Add(interestedLabel);
                }
                // Return true even if the Label was allowed exists in the Interested Labels set
                return true;
            }
            else
                return false;
        }

        public bool RemoveInterestedLabel(string interestedLabel)
        {
            if (InterestedLabels.Contains(interestedLabel))
            {
                return InterestedLabels.Remove(interestedLabel);
            }
            else
            {
                return true;
            }
        }

        private bool IsInterestedLabel(string label)
        {
            if (InterestedLabels == null)
                return true;
            else
                return InterestedLabels.Contains(label);
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        private static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private static unsafe TFTensor CreateFloatArrayTensorFromImage(Bitmap image)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte* scan0 = (byte*)data.Scan0.ToPointer();

            var matrix = new float[1, image.Height, image.Width, 3];
            for (int i = 0; i < data.Height; i++)
            {
                for (int j = 0; j < data.Width; j++)
                {
                    byte* pixelData = scan0 + i * data.Stride + j * 3;
                    matrix[0, i, j, 0] = (float)pixelData[2] / 255;
                    matrix[0, i, j, 1] = (float)pixelData[1] / 255;
                    matrix[0, i, j, 2] = (float)pixelData[0] / 255;
                }
            }

            image.UnlockBits(data);

            TFTensor tensor = matrix;
            return tensor;
        }
    }
}
