using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzUp
{
    internal class FileUploader
    {
        // TODO: Handle multiple source directories
        private string m_sourceDir;
        private CloudBlobContainer m_targetBlobContainer;
        private FileStatusDb m_fileStatusDb;

        public FileUploader(string sourceDir, CloudBlobContainer targetBlobContainer)
        {
            m_sourceDir = sourceDir;
            Console.WriteLine($"Source directory is {sourceDir}");

            m_targetBlobContainer = targetBlobContainer;

            if (!m_sourceDir.EndsWith('/'))
                m_sourceDir = $"{m_sourceDir}/";

            m_fileStatusDb = new FileStatusDb("azup.db");
        }

        public async Task Run()
        {
            await m_targetBlobContainer.CreateIfNotExistsAsync();

            Queue<FileInfo> toBeUploaded = new Queue<FileInfo>();
            Console.WriteLine($"Looking for new files in {m_sourceDir}");
            Helpers.FileSystem.WalkDirectory(
                m_sourceDir,
                fileInfo =>
                {
                    if (ShouldFileBeUploaded(fileInfo))
                        toBeUploaded.Enqueue(fileInfo);
                },
                (ex) => Console.WriteLine($"Error: {ex.Message}"));

            Console.WriteLine($"{toBeUploaded.Count} in upload queue.");
            while (toBeUploaded.TryDequeue(out var fileInfo))
            {
                Console.WriteLine($"{toBeUploaded.Count} in upload queue. Uploading {fileInfo.FullName}");
                await UploadFile(fileInfo);
            }
        }

        private async Task<bool> UploadFile(FileInfo fileInfo)
        {
            try
            {
                var blobPath = fileInfo.FullName.Remove(0, m_sourceDir.Length);
                var blob = m_targetBlobContainer.GetBlockBlobReference(blobPath);
                using(var fileStream = File.OpenRead(fileInfo.FullName))
                {
                    await blob.UploadFromStreamAsync(fileStream);
                    await blob.SetStandardBlobTierAsync(StandardBlobTier.Archive);
                    Console.WriteLine($"Uploaded {fileInfo.FullName}.");
                    m_fileStatusDb.MarkFileUploaded(fileInfo);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // TODO: Backoff and retry
                throw;
            }
        }

        private bool ShouldFileBeUploaded(FileInfo fileInfo)
        {
            if (m_fileStatusDb.IsFileUploaded(fileInfo))
            {
                Console.WriteLine($"Ignoring {fileInfo.FullName} - already uploaded");
                return false;
            }

            return true;
        }
    }
}