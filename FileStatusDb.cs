using System;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzUp
{
    internal class FileStatusDb : IDisposable
    {
        private LiteDatabase m_db;

        public FileStatusDb(string dbFilePath)
        {
            m_db = new LiteDatabase(dbFilePath);

            var files = m_db.GetCollection<FileMetadata>("file-metadata");
            files.EnsureIndex(_ => _.Path);
        }

        public void Dispose()
        {
            m_db.Dispose();
        }

        public void MarkFileUploaded(FileInfo fileInfo)
        {
            var file = new FileMetadata
            {
                Path = fileInfo.FullName,
                LastModified = fileInfo.LastWriteTimeUtc,
                UploadCompletedAt = DateTime.UtcNow
            };

            var files = m_db.GetCollection<FileMetadata>("file-metadata");
            files.Insert(file);
        }

        public bool IsFileUploaded(FileInfo fileInfo)
        {
            var dbFiles = m_db.GetCollection<FileMetadata>("file-metadata");
            var dbFile = dbFiles.FindOne(_ => _.Path == fileInfo.FullName);

            var nudgedFileLastWriteTime = fileInfo.LastWriteTimeUtc.AddSeconds(-1);
            return dbFile != null && nudgedFileLastWriteTime <= dbFile.LastModified;
        }

        private class FileMetadata
        {
            public int Id { get; set; }
            public string Path { get; set; }
            public DateTime LastModified { get; set; }
            public DateTime UploadCompletedAt { get; set; }
        }
    }
}