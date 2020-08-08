using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AICore.ImageAccess;
using AICore.ImageProcessing;
using AICore.ObjectDetection;
using AICore.Logging;
using AICore.Notification;

namespace AICore
{
    public class AIController
    {
        private string RootDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private readonly IImageAccess ImgAccess;
        private readonly ImageProcessor ImgProcessor;
        private readonly HashSet<string> InterestedObjects;

        private int ErrorCounter = 0;

        private readonly ILogWriter ErrorLogWriter;
        private readonly ILogWriter TraceLogWriter;

        private readonly INotification httpNotifier;
        public AIController()
        {
            ErrorLogWriter = new ErrorLogFileWriter(RootDirectory, "AICore", 500000);
            TraceLogWriter = new TraceLogFileWriter(RootDirectory, "AICore", 100000);

            httpNotifier = InitializeNotifier();
            InterestedObjects = InitializeInterestedObjects();
            ImgProcessor = InitializeImageProcessor();
            ImgAccess = InitializeImageAccessor();
        }

        private ImageProcessor InitializeImageProcessor()
        {
            string objectDetectionSettingsFullpath = Path.Combine(RootDirectory, "ObjectDetection", ObjectDetectionFactory.ObjectDetectionSettingsFileName);
            string objectDetectionSettingsStr = File.ReadAllText(objectDetectionSettingsFullpath);
            objectDetectionSettingsStr = objectDetectionSettingsStr.Replace("\\", "\\\\");
            ObjectDetectionSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectDetectionSettings>(objectDetectionSettingsStr);
            IObjectDetection objDetector = ObjectDetectionFactory.CreateObjectDetector(settings);

            string falsePositivesFullPath = Path.Combine(RootDirectory, "ImageProcessing", "FalsePositives.json");
            string falsePositivesStr = File.ReadAllText(falsePositivesFullPath);
            FalsePositives falsePositives = Newtonsoft.Json.JsonConvert.DeserializeObject<FalsePositives>(falsePositivesStr);

            ImageProcessor imgProcessor = new ImageProcessor(objDetector, falsePositives);
            imgProcessor.ObjectDetectionResult += ObjectDetectionResult;
            imgProcessor.LogErrorDelegate += LogError;
            imgProcessor.LogTraceDelegate += LogTrace;

            return imgProcessor;
        }

        private IImageAccess InitializeImageAccessor()
        {
            string imageAccessSettingsFullPath = Path.Combine(RootDirectory, "ImageAccess", ImageAccessFactory.ImageAccessSettingsFileName);
            string imageAccessSettingsStr = File.ReadAllText(imageAccessSettingsFullPath);
            imageAccessSettingsStr = imageAccessSettingsStr.Replace("\\", "\\\\");
            ImageAccessSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageAccessSettings>(imageAccessSettingsStr);
            IImageAccess imgAccess = ImageAccessFactory.CreateImageAccessor(settings);
            imgAccess.ImageAvailableDelegate += ImageAvailable;
            return imgAccess;
        }

        private HashSet<string> InitializeInterestedObjects()
        {
            HashSet<string> interestedObjects = new HashSet<string>();
            interestedObjects.Add("person");
            interestedObjects.Add("bicycle");
            interestedObjects.Add("car");
            interestedObjects.Add("motorcycle");
            interestedObjects.Add("airplane");
            interestedObjects.Add("bus");
            interestedObjects.Add("train");
            interestedObjects.Add("truck");
            interestedObjects.Add("bird");
            interestedObjects.Add("cat");
            interestedObjects.Add("dog");
            interestedObjects.Add("sports ball");
            interestedObjects.Add("baseball bat");
            return interestedObjects;
        }

        private INotification InitializeNotifier()
        {
            return NotificationFactory.CreateNotifier();
        }

        #region "Image Provider"

        private void ImageAvailable(string id)
        {
            Console.WriteLine($"File {id} available for processing");

            // Optional - Check associated camera before processing image
            //            If no check is made, potential to process images for an non existent camera
            //            wasted resources
            if (!ImgProcessor.IsProcessingImage())
            {
                ProcessObjectDetection(id);
            }
        }

        #endregion

        #region "Image Processor"

        private void ProcessObjectDetection(string id)
        {
            if (ImgAccess.TryGetImage(id, out byte[] imageBytes))
            {
                ImgProcessor.ProcessObjectDetection(new ImageData(imageBytes, id));
            }
            else
            {
                // Failed to get file bytes
                LogError($"Failed to load file: {id}");
            }
        }

        private void ObjectDetectionResult(ImageData imageData, ObjectDetection.Data.Output[] output)
        {
            // Get camera info based on filename
            bool result = HandleObjectDetectionResult(imageData, output);

            // Processing an Image is complete - Get next image if File Processor has queued items
            if (result)
                LoadNextAvailableImage();
        }

        private void LoadNextAvailableImage()
        {
            string[] availableImages = ImgAccess.EnumeratePendingIds();
            if (availableImages.Count() > 0)
            {
                //string nextImageFileName = ImgProvider.GetNextFileName(); EnumerateIds
                string nextImageId = availableImages[0];
                ProcessObjectDetection(nextImageId);
            }
        }

        private bool HandleObjectDetectionResult(ImageData imageData, ObjectDetection.Data.Output[] output)
        {
            if (output == null)
            {
                Console.WriteLine($"Error during Image processing for file: {imageData.FileName}");
                LogError($"Error during Image processing for file: {imageData.FileName}");
                if (ErrorCounter >= 100)
                {
                    Console.WriteLine("Error Counter exceeded, deleting errored file.");
                    LogError("Error Counter exceeded, deleting errored file.");
                    ImgAccess.TryRemoveImage(imageData.FileName);
                }
                else
                {
                    ErrorCounter++;
                    // Move file to examine later
                    LogError("Moved file to error folder.");
                    ImgAccess.TryErroredImage(imageData.FileName);
                }
                return false;
            }
            else
            {
                if (output.Length == 0)
                {
                    // Debug.WriteLine($"No objects found in file: {imageData.FileName} - deleting file");
                    ImgAccess.TryRemoveImage(imageData.FileName);
                }
                else
                {
                    bool interestedObjectDetected = false;
                    List<string> objects = new List<string>();
                    foreach (ObjectDetection.Data.Output o in output)
                    {
                        objects.Add(o.Label);
                        if (InterestedObjects.Contains(o.Label))
                        {
                            interestedObjectDetected = true;
                            break;
                        }
                    }

                    if (interestedObjectDetected)
                    {
                        Console.WriteLine($"Objects of interest detected in file: {imageData.FileName} - saving file for review");
                        LogError($"Objects of interest detected in file: {imageData.FileName} - saving file for review");
                        ImgAccess.TrySaveImage(imageData.FileName);
                        string result = Newtonsoft.Json.JsonConvert.SerializeObject(output);
                        string jsonFileName = Path.GetFileNameWithoutExtension(imageData.FileName) + ".json";
                        ImgAccess.TrySaveMetaData(jsonFileName, result);
                    }
                    else
                    {
                        //Console.WriteLine($"No objects of interest detected in file: {imageData.FileName} - Objects found: {string.Join(",",objects.ToArray())} - deleting file");
                        //logWriter.Log($"No objects of interest detected in file: {imageData.FileName} - Objects found: {string.Join(",", objects.ToArray())} - deleting file");
                        ImgAccess.TryRemoveImage(imageData.FileName);
                    }
                }
                return true;
            }
        }

        #endregion

        private async void LogError(string msg)
        {
            // TODO: Need Log Writer
            Console.WriteLine("Error: " + msg);
            await ErrorLogWriter.Log("Error: " + msg);
        }

        private async void LogTrace(string msg)
        {
            // TODO: Need Log Writer
            Console.WriteLine("Trace: " + msg);
            await TraceLogWriter.Log("Trace: " + msg);
        }
    }

    class ImageData
    {
        public byte[] ImageBytes { get; }
        public string FileName { get; }
        public ImageData(byte[] imageBytes, string fileName)
        {
            ImageBytes = imageBytes;
            FileName = fileName;
        }
    }
}

