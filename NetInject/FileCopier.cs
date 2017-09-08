using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace NetInject
{
    internal interface IFileCopier
    {
        string CopyFile(string source, string target);

        void CopyFolder(string source, string target);
    }

    internal class FileCopier : IFileCopier
    {
        public string CopyFile(string source, string target)
        {
            if (Directory.Exists(target))
                target = Path.Combine(target, Path.GetFileName(source));
            FileSystem.CopyFile(source, target, overwrite: true);
            return target;
        }

        public void CopyFolder(string source, string target)
        {
            FileSystem.CopyDirectory(source, target, overwrite: true);
        }
    }
}