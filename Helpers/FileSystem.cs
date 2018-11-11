using System;
using System.IO;
using System.Threading.Tasks;

namespace AzUp.Helpers
{
    internal class FileSystem
    {
        public static void WalkDirectory(string dirPath, Action<FileInfo> fileAction, Action<Exception> errorAction)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            try
            {
                foreach (var fileInfo in dirInfo.EnumerateFiles())
                {
                    try
                    {
                        fileAction(fileInfo);
                    }
                    catch (UnauthorizedAccessException UnAuthTop)
                    {
                        Console.WriteLine("{0}", UnAuthTop.Message);
                    }
                }

                foreach (var childDirInfo in dirInfo.EnumerateDirectories("*"))
                {
                    WalkDirectory(childDirInfo.FullName, fileAction, errorAction);
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