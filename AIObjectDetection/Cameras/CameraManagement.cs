using System.Collections.Generic;

namespace AICore.Cameras
{
    class CameraManagement
    {
        public static string CameraSettingsFileName = "CameraSettings.json";

        public SortedList<string, Camera> Cameras;
        public SortedList<string, string> PrefixManager;
        public CameraManagement(CameraSettings cameraSettings)
        {
            Cameras = new SortedList<string, Camera>();
            PrefixManager = new SortedList<string, string>();

            foreach (Camera c in cameraSettings.Cameras)
            {
                Cameras.Add(c.Id, c);
                PrefixManager.Add(c.Prefix, c.Id);
            }
        }

        public Camera GetCamera(string imageId)
        {
            string cameraId = GetCameraId(imageId);
            if (Cameras.ContainsKey(cameraId))
                return Cameras[cameraId];
            else
                return null;
        }

        public string GetCameraId(string imageId)
        {
            foreach (string prefix in PrefixManager.Keys)
            {
                string prefixCheck = imageId.Substring(0, prefix.Length);
                if (string.CompareOrdinal(prefix, prefixCheck) == 0)
                    return PrefixManager[prefix];
            }

            return string.Empty;
        }
    }
}
