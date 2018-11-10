using System;
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

            uint filesCount = 0, uploadedCount = 0;
            await Helpers.FileSystem.WalkDirectory(
                m_sourceDir,
                async(filePath) =>
                {
                    filesCount++;
                    if (await UploadFile(filePath))
                        uploadedCount++;
                },
                (ex) => Console.WriteLine($"Error: {ex.Message}"));

            Console.WriteLine($"Uploaded {uploadedCount} of {filesCount} files");
        }

        private async Task<bool> UploadFile(FileInfo fileInfo)
        {
            if (m_fileStatusDb.IsFileUploaded(fileInfo))
            {
                Console.WriteLine($"Ignoring {fileInfo.FullName} - already uploaded");
                return false;
            }
            else
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
        }
    }
}