﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AICore.ImageProcessing
{
    class ImageProcessor : ILogging
    {
        public delegate void ObjectDetectionResultHandler(ImageData imageData, ObjectDetection.Data.Output[] output);
        public event ObjectDetectionResultHandler ObjectDetectionResult;
        public event LogErrorHandler LogErrorDelegate;
        public event LogTraceHandler LogTraceDelegate;

        private ObjectDetection.IObjectDetection ObjectDetector;

        private Queue ImagesToProcess;  // Do not iterate and remove from the Queue manually. 

        private bool IsProcessingThreadRunning;
        private Thread ImgProcessingThread;

        public ImageProcessor(ObjectDetection.IObjectDetection objectDetector)
        {
            ImagesToProcess = Queue.Synchronized(new Queue());

            ObjectDetector = objectDetector;
        }

        public void ProcessObjectDetection(ImageData imageData)
        {
            ImagesToProcess.Enqueue(imageData);
            CheckAndStartProcessing();
        }

        private void CheckAndStartProcessing()
        {
            if (!IsProcessingThreadRunning)
            {
                if (ImagesToProcess.Count > 0)
                {
                    CreateProcessingThread();
                    IsProcessingThreadRunning = true;
                }
            }
        }

        private void CreateProcessingThread()
        {
            ImgProcessingThread = new Thread(ProcessImageQueue);
            ImgProcessingThread.IsBackground = true;
            ImgProcessingThread.Start();
        }

        private void ProcessImageQueue()
        {
            do
            {
                ImageData img = (ImageData)ImagesToProcess.Dequeue();
                ProcessImage(img);
            } while (ImagesToProcess.Count > 0);
            IsProcessingThreadRunning = false;
        }

        private void ProcessImage(ImageData imageData)
        {
            //AIController.logWriter.Log($"[ImageProcessor] Begin Object Detection for file: {imageData.FileName}");
            Stopwatch totalProcessingTime = Stopwatch.StartNew();
            Stopwatch processImageTime = Stopwatch.StartNew();

            ObjectDetection.Data.Output[] result = ObjectDetector.ProcessImageAsync(new ObjectDetection.Data.Input(imageData.ImageBytes, imageData.Id, imageData.MinConfidence)).Result;
            //System.Console.WriteLine($"Obj Detect time: {processImageTime.Elapsed.TotalMilliseconds.ToString()}");
            processImageTime.Stop();

            result = PostProcessing(imageData, result);

            totalProcessingTime.Stop();
            Console.WriteLine($"Object Detection Processing Time: {processImageTime.Elapsed.TotalMilliseconds} total time: {totalProcessingTime.Elapsed.TotalMilliseconds} ");
            //AIController.logWriter.Log($"[ImageProcessor] End Object Detection for file: {imageData.FileName}");

            ObjectDetectionResult?.Invoke(imageData, result);
        }

        private ObjectDetection.Data.Output[] PostProcessing(ImageData imageData, ObjectDetection.Data.Output[] objsDetected)
        {
            objsDetected = RemoveFalsePositives(imageData.FalsePositives, objsDetected);

            objsDetected = RemovePreviouslyDetectedStationaryObjects(ref imageData.LastDetectedObjs, objsDetected);

            return objsDetected;
        }

        private bool IsFalsePositive(ObjectDetection.Data.Output detectedObj)
        {
            //if (!FalsePositives.ContainsKey(detectedObj.Label))
            //{
            //    return false;
            //}

            //List<FalsePositive> falsePositives = FalsePositives[detectedObj.Label];
            //for (int i = 0; i < falsePositives.Count; i++)
            //{
            //    var w = falsePositives[i].X2 - falsePositives[i].X1;
            //    var h = falsePositives[i].Y2 - falsePositives[i].Y1;

            //    float fpArea = w * h;

            //    var x = Math.Min(falsePositives[i].X1, detectedObj.X1);
            //    var y = Math.Min(falsePositives[i].Y1, detectedObj.Y1);
            //    var x2 = Math.Max(falsePositives[i].X2, detectedObj.X2);
            //    var y2 = Math.Max(falsePositives[i].Y2, detectedObj.Y2);

            //    var ww = Math.Max(0, x2 - x);
            //    var hh = Math.Max(0, y2 - y);

            //    var area = ww * hh;
            //    var overlap = area / fpArea;
            //    if (overlap > .9f && overlap < 1.1f)
            //    {
            //        //overlap detected
            //        Console.WriteLine("False Positive Removed");
            //        LogTrace($"[IsFalsePositive] False Positive Detected: {detectedObj.Label} Confidence: {detectedObj.Confidence} X1: {detectedObj.X1} Y1: {detectedObj.Y1} X2: {detectedObj.X2} Y2: {detectedObj.Y2}");
            //        return true;
            //    }
            //}

            return false;
        }

        private ObjectDetection.Data.Output[] RemoveFalsePositives(SortedList<string, List<FalsePositive>> falsePositives, ObjectDetection.Data.Output[] objsDetected)
        {
            List<ObjectDetection.Data.Output> returnObjs = new List<ObjectDetection.Data.Output>(objsDetected);
            foreach (ObjectDetection.Data.Output objDetected in objsDetected)
            {
                if (!falsePositives.ContainsKey(objDetected.Label))
                {
                    continue;
                }
                List<FalsePositive> fp = falsePositives[objDetected.Label];

                for (int i = 0; i < fp.Count; i++)
                {
                    for (int j = returnObjs.Count - 1; j >= 0; j--)
                    {
                        float x1 = Math.Min(fp[i].X1, returnObjs[j].X1);
                        float y1 = Math.Min(fp[i].Y1, returnObjs[j].Y1);
                        float x2 = Math.Max(fp[i].X2, returnObjs[j].X2);
                        float y2 = Math.Max(fp[i].Y2, returnObjs[j].Y2);

                        float w = Math.Max(0, x2 - x1);
                        float h = Math.Max(0, y2 - y1);

                        float area = w * h;
                        //float overlap = area / falsePositives[i].Area;
                        float overlap = fp[i].Area / area;
                        //if (overlap > .9f && overlap < 1.1f)
                        if (overlap > .9f)
                        {
                            //overlap detected
                            Console.WriteLine("False Positive Removed");
                            LogTrace($"[RemoveFalsePositives] False Positive Detected: {returnObjs[j].Label} Confidence: {returnObjs[j].Confidence} X1: {returnObjs[j].X1} Y1: {returnObjs[j].Y1} X2: {returnObjs[j].X2} Y2: {returnObjs[j].Y2}");
                            returnObjs.Remove(returnObjs[j]);
                            break;
                        }
                    }
                }
            }

            return returnObjs.ToArray();
        }

        private ObjectDetection.Data.Output[] RemovePreviouslyDetectedStationaryObjects(ref ObjectDetection.Data.Output[] lastDetectedObjs, ObjectDetection.Data.Output[] objsDetected)
        {
            if (objsDetected.Length == 0)
            {
                // do nothing
            }
            else if (lastDetectedObjs.Length == 0)
            {
                LogTrace($"[RemovePreviouslyDetectedStationaryObjects] New Last Detected Objects");
                lastDetectedObjs = objsDetected.ToArray();
            }
            else
            {
                HashSet<int> matchedIndex = new HashSet<int>();
                // Compare labels and object detections
                for (int i = 0; i < lastDetectedObjs.Length; i++)
                {
                    float x_1 = lastDetectedObjs[i].X1;
                    float y_1 = lastDetectedObjs[i].Y1;
                    float x_2 = lastDetectedObjs[i].X2;
                    float y_2 = lastDetectedObjs[i].Y2;

                    float w = x_2 - x_1;
                    float h = y_2 - y_1;

                    float area = w * h;
                    for (int j = 0; j < objsDetected.Length; j++)
                    {
                        if (matchedIndex.Contains(j))
                        {
                            continue;
                        }
                        // compare i against all of j
                        float xx1 = Math.Max(x_1, objsDetected[j].X1);
                        float yy1 = Math.Max(y_1, objsDetected[j].Y1);
                        float xx2 = Math.Min(x_2, objsDetected[j].X2);
                        float yy2 = Math.Min(y_2, objsDetected[j].Y2);

                        float ww = xx2 - xx1;
                        float hh = yy2 - yy1;

                        float compareArea = ww * hh;

                        float overlap = (float)compareArea / area;
                        if (overlap > 0.9f)
                        {
                            LogTrace($"[RemovePreviouslyDetectedStationaryObjects] Stationary Object Detected: {objsDetected[j].Label} Confidence: {objsDetected[j].Confidence} X1: {objsDetected[j].X1} Y1: {objsDetected[j].Y1} X2: {objsDetected[j].X2} Y2: {objsDetected[j].Y2}");
                            matchedIndex.Add(j);
                            break;
                        }
                    }

                    if (matchedIndex.Count != i + 1)
                    {
                        break;
                    }
                }
                if (matchedIndex.Count == lastDetectedObjs.Length)
                    objsDetected = new ObjectDetection.Data.Output[] { };
                else
                {
                    LogTrace($"[RemovePreviouslyDetectedStationaryObjects] New Last Detected Objects");
                    lastDetectedObjs = objsDetected.ToArray();
                }

            }
            return objsDetected;
        }

        public bool IsProcessingImage()
        {
            return IsProcessingThreadRunning;
        }

        private void LogError(string msg)
        {
            LogErrorDelegate?.BeginInvoke($"ImageProcessor Error: {msg}", (r) => { }, null);
        }

        private void LogTrace(string msg)
        {
            LogTraceDelegate?.BeginInvoke($"ImageProcessor Trace: {msg}", (r) => { }, null);
        }
    }
}
