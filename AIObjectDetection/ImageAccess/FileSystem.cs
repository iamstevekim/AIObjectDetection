using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace AICore.ImageAccess
{
    class FileSystem : IImageAccess
    {
        // Unique to FileSystemWatcher based implementation
        public delegate void FileRenamedHandler(string oldFileName, string newFileName);
        public delegate void FileDeletedHandler(string fileName);

        public event FileRenamedHandler FileRenamed;
        public event FileDeletedHandler FileDeleted;

        private readonly FileSystemWatcher Watcher;

        public int file_access_delay = 10;

        private string SaveDirectory;
        private string ErrorDirectory;
        public FileSystem(FileSystemSettings settings) : this(settings.SourceDirectory, settings.Filter, settings.SaveDirectory, settings.ErrorDirectory, settings.ProcessExistingFiles) { }

        public FileSystem(string sourceDir, string filter, string saveDir, string errDir, bool processExistingFiles = false) : base()
        {
            if (processExistingFiles)
                LoadExistingFiles(sourceDir, filter);

            SaveDirectory = saveDir;
            ErrorDirectory = errDir;
            Watcher = new FileSystemWatcher();
            Watcher.Created += new FileSystemEventHandler(OnCreated);
            Watcher.Changed += new FileSystemEventHandler(OnChanged);
            Watcher.Renamed += new RenamedEventHandler(OnRenamed);
            Watcher.Deleted += new FileSystemEventHandler(OnDeleted);

            Watcher.Filter = filter;
            TryUpdatePath(sourceDir);
        }

        private void LoadExistingFiles(string path, string filter)
        {
            
            DirectoryInfo dir = new DirectoryInfo(path);
            FileSystemInfo[] existingFiles = dir.GetFileSystemInfos(filter);
            Array.Sort<FileSystemInfo>(existingFiles, 
                delegate (FileSystemInfo a, FileSystemInfo b)
                {
                return a.LastWriteTime.CompareTo(b.LastWriteTime);
                });
            
            foreach (FileSystemInfo file in existingFiles)
            {
                PendingIds.Add(file.Name);
            }
        }

        public bool TryUpdatePath(string path)
        {
            if (IsPathValid(path))
            {
                if (Watcher.EnableRaisingEvents == true)
                    Watcher.EnableRaisingEvents = false;

                Watcher.Path = path;
                Watcher.EnableRaisingEvents = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsPathValid(string path)
        {
            if (path == string.Empty)
                return false;
            else if (!Directory.Exists(path))
                return false;
            else
                return true;
        }

        private bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // File is unavailable because it is:
                //  Still being written to
                //  or being processed by another thread
                //  or does not exist
                return true;
            }

            // file is not locked
            return false;
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            PendingIds.Add(e.Name);
            ImageAvailable(e.Name);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Console.WriteLine("on changed fired");
            // Not currently used
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            string oldFileName = e.OldName;
            string newFileName = e.Name;
            if (PendingIds.Contains(oldFileName))
                PendingIds.Remove(oldFileName);

            // This will place the renamed file at the end of the list
            PendingIds.Add(newFileName);
            FileRenamed?.Invoke(oldFileName, newFileName);
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            string fileName = e.Name;
            if (PendingIds.Contains(fileName))
                PendingIds.Remove(fileName);

            FileDeleted?.Invoke(fileName);
        }

        private bool TryRemoveFile(string fullPath)
        {
            FileInfo file = new FileInfo(fullPath);
            if (!IsFileLocked(file))
            {
                file.Delete();
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryGetFile(string fullPath, out byte[] outBytes)
        {
            FileInfo file = new FileInfo(fullPath);
            if (!file.Exists)
            {
                outBytes = null;
                return false;
            }
            else
            {
                while (IsFileLocked(file))
                {
                    System.Threading.Thread.Sleep(file_access_delay);
                }
                outBytes = File.ReadAllBytes(file.FullName);
                return true;
            }
        }

        private bool TryGetFile(string fullPath, out string outString)
        {
            FileInfo file = new FileInfo(fullPath);
            if (!file.Exists)
            {
                outString = null;
                return false;
            }
            else
            {
                while (IsFileLocked(file))
                {
                    System.Threading.Thread.Sleep(file_access_delay);
                }
                outString = File.ReadAllText(file.FullName);
                return true;
            }
        }

        private bool TryMoveFile(string origFullPath, string destFullPath)
        {
            if (origFullPath.Equals(destFullPath, StringComparison.OrdinalIgnoreCase))
                return true;

            FileInfo file = new FileInfo(origFullPath);
            try
            {
                file.MoveTo(destFullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool TryGetImage(string fileName, out byte[] outImageBytes)
        {
            if (PendingIds.Contains(fileName))
                PendingIds.Remove(fileName);

            string fullPath = Path.Combine(Watcher.Path, fileName);
            return TryGetFile(fullPath, out outImageBytes);
        }

        public override bool TryRemoveImage(string fileName)
        {
            if (PendingIds.Contains(fileName))
                PendingIds.Remove(fileName);

            string fullPath = Path.Combine(Watcher.Path, fileName);
            return TryRemoveFile(fullPath);
        }

        public override bool TrySaveImage(string fileName)
        {
            string origFullPath = Path.Combine(Watcher.Path, fileName);
            string destFullPath = Path.Combine(SaveDirectory, fileName);
            return TryMoveFile(origFullPath, destFullPath);
        }

        public override bool TryGetMetaData(string fileName, out string outMetaData)
        {
            string fullPath = Path.Combine(SaveDirectory, fileName);
            return TryGetFile(fullPath, out outMetaData);
        }

        public override bool TryRemoveMetaData(string fileName)
        {
            string fullPath = Path.Combine(SaveDirectory, fileName);
            return TryRemoveFile(fullPath);
        }

        public override bool TrySaveMetaData(string fileName, string metaData)
        {
            string fullPath = Path.Combine(SaveDirectory, fileName);
            TextWriter writer = null;
            try
            {
                writer = new StreamWriter(fullPath, false);
                writer.Write(metaData);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public override bool TryGetSavedImage(string fileName, out byte[] outImageBytes)
        {
            string fullPath = Path.Combine(SaveDirectory, fileName);
            return TryGetFile(fullPath, out outImageBytes);
        }

        public override bool TryRemoveSavedImage(string fileName)
        {
            string fullPath = Path.Combine(SaveDirectory, fileName);
            return TryRemoveFile(fullPath);
        }

        public override bool TryErroredImage(string fileName)
        {
            string origFullPath = Path.Combine(Watcher.Path, fileName);
            string destFullPath = Path.Combine(ErrorDirectory, fileName);
            return TryMoveFile(origFullPath, destFullPath);
        }
    }
}
