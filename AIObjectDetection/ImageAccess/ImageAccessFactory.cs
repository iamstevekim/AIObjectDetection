using System;
using System.Collections.Generic;
using System.Text;

namespace AICore.ImageAccess
{
    class ImageAccessFactory
    {
        public static string ImageAccessSettingsFileName = "ImageAccessSettings.json";
        public static IImageAccess CreateImageAccessor(ImageAccessSettings settings)
        {
            switch (settings.Type)
            {
                case ImageAccessType.FileSystem:
                    return new FileSystem(settings.FileSystemSettings);
                default:
                    throw new Exception($"Unrecongnized Image Access Type: {settings.Type}");
            }

        }
    }
}
