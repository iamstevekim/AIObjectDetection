using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AICore.ImageAccess;
using AICore.ImageProcessing;
using AICore.ObjectDetection;
using AICore.Logging;
using AICore.Notification;
using AICore.Cameras;

namespace AICore
{
    public class AIController
    {
        private string RootDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private readonly IImageAccess ImgAccess;
        private readonly ImageProcessor ImgProcessor;
        private readonly HashSet<string> InterestedObjects;
        private readonly CameraManagement CameraManager;

        private int ErrorCounter = 0;

        private readonly ILogWriter ErrorLogWriter;
        private readonly ILogWriter TraceLogWriter;

        private readonly INotification httpNotifier;
        public AIController()
        {
            ErrorLogWriter = new ErrorLogFileWriter(RootDirectory, "AICore", 500000);
            TraceLogWriter = new TraceLogFileWriter(RootDirectory, "AICore", 100000);

            httpNotifier = InitializeNotifier();
            CameraManager = InitializeCameraManager();
            InterestedObjects = InitializeInterestedObjects();
            ImgProcessor = InitializeImageProcessor();
            ImgAccess = InitializeImageAccessor();

            LoadNextAvailableImage();
        }

        private ImageProcessor InitializeImageProcessor()
        {
            string objectDetectionSettingsFullpath = Path.Combine(RootDirectory, ObjectDetectionFactory.ObjectDetectionSettingsFileName);
            string objectDetectionSettingsStr = File.ReadAllText(objectDetectionSettingsFullpath);
            objectDetectionSettingsStr = objectDetectionSettingsStr.Replace("\\", "\\\\");
            ObjectDetectionSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ObjectDetectionSettings>(objectDetectionSettingsStr);
            IObjectDetection objDetector = ObjectDetectionFactory.CreateObjectDetector(settings);

            ImageProcessor imgProcessor = new ImageProcessor(objDetector);
            imgProcessor.ObjectDetectionResult += ObjectDetectionResult;
            imgProcessor.LogErrorDelegate += LogError;
            imgProcessor.LogTraceDelegate += LogTrace;

            return imgProcessor;
        }

        private IImageAccess InitializeImageAccessor()
        {
            string imageAccessSettingsFullPath = Path.Combine(RootDirectory, ImageAccessFactory.ImageAccessSettingsFileName);
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

        private CameraManagement InitializeCameraManager()
        {
            string cameraSettingsFullPath = Path.Combine(RootDirectory, CameraManagement.CameraSettingsFileName);
            string cameraSettingsStr = File.ReadAllText(cameraSettingsFullPath);
            cameraSettingsStr = cameraSettingsStr.Replace("\\", "\\\\");
            CameraSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<CameraSettings>(cameraSettingsStr);
            CameraManagement cameraManager = new CameraManagement(settings);
            return cameraManager;
        }

        #region "Image Provider"

        private void ImageAvailable(string id)
        {
            //Console.WriteLine($"File {id} available for processing");

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
                Camera c = CameraManager.GetCamera(id);
                if (c != null)
                    ImgProcessor.ProcessObjectDetection(new ImageData(imageBytes, id, c.MinConfidence, c.FalsePositives, c.LastDetectedObjs));
                else
                {
                    ImgAccess.TryErroredImage(id);
                    LoadNextAvailableImage();
                }    
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
            Camera c = CameraManager.GetCamera(imageData.Id);
            if (c != null)
                c.LastDetectedObjs = imageData.LastDetectedObjs;

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
                Console.WriteLine($"Error during Image processing for file: {imageData.Id}");
                LogError($"Error during Image processing for file: {imageData.Id}");
                if (ErrorCounter >= 100)
                {
                    Console.WriteLine("Error Counter exceeded, deleting errored file.");
                    LogError("Error Counter exceeded, deleting errored file.");
                    ImgAccess.TryRemoveImage(imageData.Id);
                }
                else
                {
                    ErrorCounter++;
                    // Move file to examine later
                    LogError("Moved file to error folder.");
                    ImgAccess.TryErroredImage(imageData.Id);
                }
                return false;
            }
            else
            {
                if (output.Length == 0)
                {
                    // Debug.WriteLine($"No objects found in file: {imageData.FileName} - deleting file");
                    ImgAccess.TryRemoveImage(imageData.Id);
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
                        Console.WriteLine($"Objects of interest detected in file: {imageData.Id} - saving file for review");
                        LogError($"Objects of interest detected in file: {imageData.Id} - saving file for review");
                        ImgAccess.TrySaveImage(imageData.Id);
                        string result = Newtonsoft.Json.JsonConvert.SerializeObject(output);
                        string jsonFileName = Path.GetFileNameWithoutExtension(imageData.Id) + ".json";
                        ImgAccess.TrySaveMetaData(jsonFileName, result);
                    }
                    else
                    {
                        //Console.WriteLine($"No objects of interest detected in file: {imageData.FileName} - Objects found: {string.Join(",",objects.ToArray())} - deleting file");
                        //logWriter.Log($"No objects of interest detected in file: {imageData.FileName} - Objects found: {string.Join(",", objects.ToArray())} - deleting file");
                        ImgAccess.TryRemoveImage(imageData.Id);
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
        public string Id { get; }
        public float MinConfidence { get; }
        public SortedList<string, List<FalsePositive>> FalsePositives { get; }
        public ObjectDetection.Data.Output[] LastDetectedObjs;
        public ImageData(byte[] imageBytes, string id, float minConfidence, SortedList<string, List<FalsePositive>> falsePositives, ObjectDetection.Data.Output[] lastDetectedObjs)
        {
            ImageBytes = imageBytes;
            Id = id;
            MinConfidence = minConfidence;
            FalsePositives = falsePositives;
            LastDetectedObjs = lastDetectedObjs;
        }
    }
}

