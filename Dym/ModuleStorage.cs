using System;
using System.Collections.Generic;
using System.IO;
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
        public void SaveOrUpdateFile(string id, string filename, string asmHash, Version version, Stream stream)
        {            
            var bsonIdValue = new BsonValue(id);
            var bsonFileNameValue = new BsonValue(filename);
            var bsonAsmHashValue = new BsonValue(asmHash);
            var bsonVersionValue = new BsonValue(version.ToString());

            var bson = new BsonDocument() 
            {
                new KeyValuePair<string, BsonValue>(nameof(id), bsonIdValue),
                new KeyValuePair<string, BsonValue>(nameof(filename), bsonFileNameValue),
                new KeyValuePair<string, BsonValue>(nameof(asmHash), bsonAsmHashValue),
                new KeyValuePair<string, BsonValue>(nameof(version), bsonVersionValue) 
            };

            _db.FileStorage.Upload(id, filename, stream, bson);
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

        public Dictionary<string, string> FindAll()
        {
            var result = _db.FileStorage.FindAll();

            Dictionary<string, string> items = new Dictionary<string, string>();

            foreach (var ret in result)
            {
                string metaInfo = null;

                foreach (var metadata in ret.Metadata)
                {
                    metaInfo += $" {metadata.Key}={metadata.Value}";
                }

                items.Add(ret.Id,metaInfo);
            }
            return items;
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
