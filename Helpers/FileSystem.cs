using System;
using System.IO;
using System.Threading.Tasks;

namespace AzUp.Helpers
{
    internal class FileSystem
    {
        public static async Task WalkDirectory(string dirPath, Func<FileInfo,Task> fileAction, Action<Exception> errorAction)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            try
            {
                foreach (var fileInfo in dirInfo.EnumerateFiles())
                {
                    try
                    {
                        await fileAction(fileInfo);
                    }
                    catch (UnauthorizedAccessException UnAuthTop)
                    {
                        Console.WriteLine("{0}", UnAuthTop.Message);
                    }
                }

                foreach (var childDirInfo in dirInfo.EnumerateDirectories("*"))
                {
                    await WalkDirectory(childDirInfo.FullName, fileAction, errorAction);
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                errorAction(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                errorAction(ex);
            }
            catch (PathTooLongException ex)
            {
                errorAction(ex);
            }
        }
    }
}