using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace FileBackuper
{
    public class FileTree
    {
        public FileTree(FileInfo file)
        {
            File = file;
            //if(!File.Attributes.HasFlag(FileAttributes.Directory))
            //{
            //    Info = new BackupedFileInfo(file);
            //}
        }
        public FileTree(DirectoryInfo directory)
        {
            Directory = directory;
        }
        // public BackupedFileInfo Info { get; private set; }
        public FileInfo File { get; private set; }
        public DirectoryInfo Directory { get; private set; }
        public List<FileTree> SubFileTrees { get; set; } = new List<FileTree>();
        public object Image
        {
            get
            {
                if (IsFile)
                {
                    if (Exist)
                    {
                        return WpfControls.Common.CloneXaml(App.Current.FindResource("imgFile"));
                    }
                    return WpfControls.Common.CloneXaml(App.Current.FindResource("imgError"));
                }

                return WpfControls.Common.CloneXaml(App.Current.FindResource("imgFolder"));
            }
        }
        public bool IsFile => File != null;
        public bool Exist
        {
            get
            {
                if (IsFile)
                {
                    return File.Exists;
                }
                return true;
            }
        }
        public string Name
        {
            get
            {
                if (IsFile)
                {
                    string name = File.Name;
                    if(name.StartsWith("#OldBackupedFile#"))
                    {
                        try
                        {
                            name = name.Substring(33);
                        }
                        catch
                        {

                        }
                    }
                    return name;
                }
                return Directory.Name;
            }
        }
        public string LastWriteTime
        {
            get
            {
                if (IsFile)
                {
                    if (Exist)
                    {
                        return File.LastWriteTime.ToString();
                    }
                    return "";
                }
                return Directory.LastWriteTime.ToString();
            }
        }


        public FileTree GetSubTree(DirectoryInfo directory)
        {
            FileTree tree = SubFileTrees.FirstOrDefault(p => p.Directory?.FullName == directory.FullName);
            if (tree == null)
            {
                FileTree child = new FileTree(directory);
                SubFileTrees.Add(child);
                return child;
            }
            else
            {
                return tree;
            }
        }
    }
}
