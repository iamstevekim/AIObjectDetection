using System.Collections.Generic;

namespace AICore.ImageAccess
{
    abstract class IImageAccess
    {
        public delegate void ImageAvailableHandler(string id);
        public event ImageAvailableHandler ImageAvailableDelegate;

        protected void ImageAvailable(string id)
        {
            ImageAvailableDelegate?.Invoke(id);
        }

        protected List<string> PendingIds;

        protected IImageAccess()
        {
            PendingIds = new List<string>();
        }

        public string[] EnumeratePendingIds()
        {
            return PendingIds.ToArray();
        }

        // public abstract string[] SavedIds();

        public abstract bool TryGetImage(string id, out byte[] outImageBytes);

        public abstract bool TryRemoveImage(string id);

        public abstract bool TrySaveImage(string id);

        public abstract bool TryErroredImage(string id);

        public abstract bool TryGetSavedImage(string id, out byte[] outImageBytes);

        public abstract bool TryRemoveSavedImage(string id);

        public abstract bool TryGetMetaData(string id, out string outMetaData);

        public abstract bool TryRemoveMetaData(string id);

        public abstract bool TrySaveMetaData(string id, string metaData);
        
    }
}
