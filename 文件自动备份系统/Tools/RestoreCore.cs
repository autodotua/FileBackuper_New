using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileBackuper
{
    public class RestoreCore
    {
        List<string> filePaths;
        //Dictionary<string, string> realFilePaths = new Dictionary<string, string>();
        Dictionary<string, FileInfo> realFileInfos = new Dictionary<string, FileInfo>();
        //Dictionary<string, FileInfo> notExistFileInfo = new Dictionary<string, FileInfo>();
        DateTime backupedTime;
        TaskInfo task;
        public RestoreCore(TaskInfo task, string fileListPath, DateTime backupedTime)
        {
            filePaths = new List<string>(File.ReadAllLines(fileListPath));
            this.task = task;
            this.backupedTime = backupedTime;
        }

        private void SearchFiles()
        {
            foreach (var path in filePaths)
            {
                // realFilePaths.Add(path, null);
                realFileInfos.Add(path, null);
                //notExistFileInfo.Add(path, null);
                string fileFullName = task.TargetBackupDirectory + path;
                FileInfo fileInfo = new FileInfo(fileFullName);
                if (fileInfo.Exists && backupedTime > fileInfo.LastWriteTime)
                {
                    // realFilePaths[path] = fileFullName;
                    realFileInfos[path] = fileInfo;
                }
                else
                {
                    if (fileInfo.Directory.Exists)
                    {
                        Regex r = new Regex("^#OldBackupedFile#[0-9]{8}-[0-9]{6}#$");
                        FileInfo findedFile = fileInfo.Directory.EnumerateFiles()
                            .Where(p => r.IsMatch(p.Name.TrimEnd(fileInfo.Name.ToCharArray())) && p.LastWriteTime < backupedTime)
                            .OrderByDescending(p => p.LastWriteTime).FirstOrDefault();
                        if (findedFile != null)
                        {
                            // realFilePaths[path] = findedFile.FullName;
                            realFileInfos[path] = findedFile;
                        }
                    }
                }

                if (realFileInfos[path] == null)
                {
                    realFileInfos[path] = fileInfo;
                    //notExistFileInfo[path] = fileInfo;
                }
            }
        }


        private string StringToUnicode(string rawString)
        {
            StringBuilder result = new StringBuilder(rawString.Length * 8);
            foreach (char c in rawString)
            {
                result.Append("\\u");
                foreach (int i in Encoding.Unicode.GetBytes(new char[] { c }))
                {
                    result.Append(i.ToString("X2"));
                }
            }
            return result.ToString();
        }

        FileTree tree;

        public FileTree GetFileTree()
        {
            SearchFiles();
            tree = new FileTree(new DirectoryInfo(task.TargetBackupDirectory));

            //foreach (var path in filePaths)
            //{
            //    if (realFileInfos.ContainsKey(path))
            //    {
            //        InsertToFileTree(realFileInfos[path]);
            //    }
            //    else
            //    {
            //        InsertToFileTree(notExistFileInfo[path]);
            //    }
            //}

            foreach (var file in realFileInfos)
            {
                InsertToFileTree(file.Value);
            }
            return tree.SubFileTrees[0];
        }


        private void InsertToFileTree(FileInfo file)
        {
            Stack<DirectoryInfo> directories = new Stack<DirectoryInfo>();
            DirectoryInfo directory = new DirectoryInfo(file.FullName);
            while ((directory = directory.Parent).FullName != task.TargetBackupDirectory)
            {
                directories.Push(directory);
            }

            FileTree currentTree = tree;
            while (directories.Count > 0)
            {
                directory = directories.Pop();
                currentTree = currentTree.GetSubTree(directory);
            }
            currentTree.SubFileTrees.Add(new FileTree(file));
        }
    }
}
