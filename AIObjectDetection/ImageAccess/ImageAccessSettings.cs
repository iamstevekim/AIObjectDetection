using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace AICore.ImageAccess
{
    enum ImageAccessType
    {
        FileSystem
    }

    class ImageAccessSettings
    {
        [JsonProperty("type")]
        public readonly ImageAccessType Type;
        [JsonProperty("fileSystemSettings")]
        public readonly FileSystemSettings FileSystemSettings;

        public ImageAccessSettings()
        {

        }
    }

    class FileSystemSettings
    {
        [JsonProperty("sourceDirectory")]
        public readonly string SourceDirectory;
        [JsonProperty("filter")]
        public readonly string Filter;
        [JsonProperty("saveDirectory")]
        public readonly string SaveDirectory;
        [JsonProperty("errorDirectory")]
        public readonly string ErrorDirectory;
        [JsonProperty("processExistingFiles")]
        public readonly bool ProcessExistingFiles;

        public FileSystemSettings()
        {
            SourceDirectory = string.Empty;
            Filter = string.Empty;
            SaveDirectory = string.Empty;
            ErrorDirectory = string.Empty;
            ProcessExistingFiles = false;
        }
    }
}
