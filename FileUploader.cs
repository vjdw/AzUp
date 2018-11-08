using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzUp
{
    internal class FileUploader
    {
        private static string m_sourceDir;

        private static CloudBlobContainer m_targetBlobContainer;

        public FileUploader(string sourceDir, CloudBlobContainer targetBlobContainer)
        {
            m_sourceDir = sourceDir;
            Console.WriteLine($"Source directory is {sourceDir}");

            m_targetBlobContainer = targetBlobContainer;

            if (!m_sourceDir.EndsWith('/'))
                m_sourceDir = $"{m_sourceDir}/";
        }

        public async Task Run()
        {
            await m_targetBlobContainer.CreateIfNotExistsAsync();

            await Helpers.FileSystem.WalkDirectory(
                m_sourceDir,
                async(filePath) =>
                {
                    try
                    {
                        await UploadFile(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        // TODO: Backoff and retry
                        throw;
                    }
                },
                (ex) => Console.WriteLine($"Error: {ex.Message}"));
        }

        private static async Task UploadFile(FileInfo fileInfo)
        {
            var blobPath = fileInfo.FullName.Remove(0, m_sourceDir.Length);
            var blob = m_targetBlobContainer.GetBlockBlobReference(blobPath);
            using(var fileStream = File.OpenRead(fileInfo.FullName))
            {
                await blob.UploadFromStreamAsync(fileStream);
                Console.WriteLine($"Uploaded {fileInfo.FullName}.");
            }
        }
    }
}