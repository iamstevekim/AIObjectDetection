using System;

namespace AICore.ObjectDetection
{
    class ObjectDetectionFactory
    {
        public static string ObjectDetectionSettingsFileName = "ObjectDetectionSettings.json";
        public static IObjectDetection CreateObjectDetector(ObjectDetectionSettings settings)
        {
            switch(settings.Type)
            {
                case ObjectDetectionType.DeepStack:
                    return new DeepStack.DeepStackClient(settings.DeepStackSettings);
                case ObjectDetectionType.TensorFlowSharp:
                    return new TensorFlowSharp.TensorFlowSharpClient(settings.TensorFlowSharpSettings);
                default:
                    throw new Exception($"Unrecognized Object Detection Type: {settings.Type}");
            }
        }
    }
}
