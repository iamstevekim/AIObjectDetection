using AICore.ImageAccess;
using System;

namespace AICore
{
    public class AIController
    {
        private string RootDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private readonly IImageAccess ImgAccess;
        public AIController()
        {
            ImgAccess = InitializeImageAccessor();
        }

        private IImageAccess InitializeImageAccessor()
        {
            string imageAccessSettingsFullPath = System.IO.Path.Combine(RootDirectory, "ImageAccess", ImageAccessFactory.ImageAccessSettingsFileName);
            string imageAccessSettingsStr = System.IO.File.ReadAllText(imageAccessSettingsFullPath);
            imageAccessSettingsStr = imageAccessSettingsStr.Replace("\\", "\\\\");
            ImageAccessSettings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageAccessSettings>(imageAccessSettingsStr);
            IImageAccess imgAccess = ImageAccessFactory.CreateImageAccessor(settings);
            imgAccess.ImageAvailableDelegate += ImageAvailable;
            return imgAccess;
        }

        private void ImageAvailable(string id)
        {
            Console.WriteLine($"File {id} available for processing");
        }
    }
}

