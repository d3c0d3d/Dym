using System;
using System.IO;
using System.Linq;
using LiteDB;

namespace Dym
{
    public class ModuleStorage : IDisposable
    {
        private LiteDatabase _db;

        public ModuleStorage()
        {
            _db = new LiteDatabase(Constants.ModuleStorageConnStr);
        }

        public void SaveOrUpdateFile(string id, string filename, Stream stream)
        {
            _db.FileStorage.Upload(id, filename, stream);
        }
        public void SaveOrUpdateFile(string id, string filename, Version version, Stream stream)
        {
            _db.FileStorage.Upload(id, $"{filename}_{version}", stream);
        }

        public byte[] GetFile(string id)
        {
            if (FileExists(id))
                using (MemoryStream ms = new MemoryStream())
                {
                    _db.FileStorage.OpenRead(id).CopyTo(ms);
                    return ms.ToArray();
                }
            else
                return null;
        }

        public bool FileExists(string id)
        {
            return _db.FileStorage.FindById(id) != null;
        }

        public Version GetVersionInFile(string id)
        {
            if (!FileExists(id))
                return null;
            var file = _db.FileStorage.FindById(id);
            if (file.Filename.Contains("_"))
            {
                var extractVersion = file.Filename.Split('_').LastOrDefault();
                return new Version(extractVersion);
            }
            return null;

        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
