using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using static FileBackuper.LogHelper;

namespace FileBackuper
{
    class BackupCore
    {
        public string status = "正在备份";
        public BackupCore(TaskInfo task)
        {
            this.task = task;
        }
        TaskInfo task;

        bool stop = false;

        public void TryToStop()
        {
            stop = true;
        }

        private bool CheckStop()
        {
            if (stop)
            {
                AppendLog(task, "已停止备份程序");
                //task.Status = TaskInfo.Statuses.Pause;
                return true;
            }
            return false;
        }

        //Dictionary<string, string> rootDirectories = new Dictionary<string, string>();
        List<FileInfo> sourceFiles = new List<FileInfo>();
        Dictionary<FileInfo, bool> backupedFiles = new Dictionary<FileInfo, bool>();


        public void Backup()
        {
            try
            {
                AppendLog(task, "开始备份");
                ListAllFiles();

                //挑出其中的文件，其余的列举文件
                if (CheckStop())
                {
                    return;
                }

                status = "正在寻找文件";
                //列举目标目录文件
                try
                {
                    ListBackupedFiles();
                }
                catch (Exception ex)
                {
                    AppendLog(task, "在列举备份目录里的文件时发生异常：" + ex.ToString());
                    AppendLog(task, "备份失败");
                    return;
                }
                if (CheckStop())
                {
                    return;
                }
                //列举差异项
                try
                {
                    ListDifferences();
                    status = "正在重命名旧文件";
                    RenameOldBackupedFiles();
                }
                catch (Exception ex)
                {
                    AppendLog(task, "在列举不同文件并重命名时发生异常：" + ex.ToString());
                    AppendLog(task, "备份失败");
                    return;
                }

                task.DisplayStatus = "正在备份";
                //将不同的部分复制到目标文件夹
                MoveDifferences();



                SaveSnapshot();
            }
            finally
            {
                Save(task);

                AppendLog(task, "备份结束");
            }

        }

        private void ListAllFiles()
        {
            foreach (var i in task.WhiteList)
            {
                if (Directory.Exists(i))
                {
                    listSourceFiles(i);
                }
                else if (File.Exists(i))
                {
                    sourceFiles.Add(new FileInfo(i));
                }
                else
                {
                    AppendLog(task, "找不到部分白名单目录：" + i);
                }
                AppendLog(task, "共发现" + sourceFiles.Count + "个需要检查的文件");

            }
        }

        /// <summary>
        /// 列举源目录文件
        /// </summary>
        /// <param name="path"></param>
        private void listSourceFiles(string path)
        {
            // int n = 0;
            foreach (var i in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                if (!task.BlackList.Any(p => i.StartsWith(p)))
                {
                    sourceFiles.Add(new FileInfo(i));
                }
            }

        }
        /// <summary>
        /// 列举目标目录里面已经备份过的文件
        /// </summary>
        /// <param name="path"></param>
        private void ListBackupedFiles()
        {
            if (Directory.Exists(task.TargetBackupDirectory))
            {
                AppendLog(task, "开始枚举备份过的文件");
                var files = Directory.EnumerateFiles(task.TargetBackupDirectory, "*", SearchOption.AllDirectories);
                AppendLog(task, "在备份目录中找到" + files.Count() + "个文件");
                foreach (var filePath in files)
                {
                    FileInfo file = new FileInfo(filePath);
                    if (!filePath.Contains("#OldBackupedFile#")
                        && file.Directory.Name != "FileList"
                        && file.Directory.Name != "Logs"
                        //&& file.Name != $"FileBackuper_Log_{task.Name}.json"
                        && file.Name != $"FileBackuper_Config_{task.Name}.json")
                    {
                        backupedFiles.Add(new FileInfo(filePath), false);
                    }

                }
                AppendLog(task, "枚举备份过的文件完成");
            }
            else
            {
                AppendLog(task, "目标目录不存在");
            }
            //int n = 0;

        }

        HashSet<string> renameNeededBackupedFiles = new HashSet<string>();
        HashSet<string> updateNeededSourceFiles = new HashSet<string>();
        /// <summary>
        /// 列举源目录和目标目录不同的文件，并且把相同的但是修改时间不同的文件改名用于备份
        /// </summary>
        private void ListDifferences()
        {
            int count = sourceFiles.Count;
            int index = 0;
            AppendLog(task, "开始寻找差异项");
            foreach (var file in sourceFiles)
            {
                index++;
                status = "正在寻找差异项" + index + "/" + count;
                FileInfo targetFile = new FileInfo(SourceFilePathToTargetFilePath(file.FullName));
                FileInfo backupedFile = backupedFiles.Keys.FirstOrDefault(p => p.FullName == targetFile.FullName);
                if (backupedFile != null) //如果找到相同文件名的文件
                {
                    backupedFiles[backupedFile] = true;
                    if (file.LastWriteTime != targetFile.LastWriteTime)//如果修改时间相同
                    {
                        renameNeededBackupedFiles.Add(targetFile.FullName);
                        updateNeededSourceFiles.Add(file.FullName);
                    }
                }
                else
                {
                    updateNeededSourceFiles.Add(file.FullName);
                }
                if (CheckStop())
                {
                    return;
                }
            }

            status = "正在寻找差异项";
            foreach (var item in backupedFiles.Where(p => !p.Value))
            {
                renameNeededBackupedFiles.Add(item.Key.FullName);
            }

            AppendLog(task, $"找到{updateNeededSourceFiles.Count}个需要更新的文件，{renameNeededBackupedFiles.Count}个需要重命名的已备份文件");
        }

        private void RenameOldBackupedFiles()
        {
            AppendLog(task, "开始重命名旧的备份文件");
            foreach (var filePath in renameNeededBackupedFiles)
            {
                FileInfo file = new FileInfo(filePath);
                string newFilePath = file.DirectoryName + "\\#OldBackupedFile#" + file.LastWriteTime.ToString(Properties.Resources.DateTimeFormat) + "#" + file.Name;

                try
                {
                    if(File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }
                    File.Move(filePath, newFilePath);
                    AppendLog(task, $"重命名{filePath}为{newFilePath}成功");

                }
                catch (Exception ex)
                {
                    AppendLog(task, $"重命名{filePath}为{newFilePath}失败：{ex.Message}");
                }
            }
            AppendLog(task, "完成重命名旧的备份文件");
        }

        /// <summary>
        /// 备份差异项
        /// </summary>
        private void MoveDifferences()
        {
            int count = updateNeededSourceFiles.Count; ;
            int current = 0;
            int ok = 0;
            foreach (var filePath in updateNeededSourceFiles)
            {
                if (CheckStop())
                {
                    return;
                }
                current++;
                FileInfo sourceFile = new FileInfo(filePath);
                FileInfo targetFile = new FileInfo(SourceFilePathToTargetFilePath(sourceFile.FullName));
                try
                {
                    if (!targetFile.Directory.Exists)
                    {
                        targetFile.Directory.Create();
                    }
                    status = "正在复制" + current + "/" + count;
                    //复制文件
                    File.Copy(sourceFile.FullName, targetFile.FullName);
                    ok++;
                    AppendLog(task, $"复制{sourceFile.FullName}到{targetFile.FullName}成功");
                }
                catch (Exception ex)
                {
                    AppendLog(task, $"复制{sourceFile.FullName}到{targetFile.FullName}失败：{ex.Message}");
                }
            }
            AppendLog(task, $"文件复制完成，需要{count}个，成功{ok}个");

        }

        public void SaveSnapshot()
        {
            StringBuilder str = new StringBuilder();
            AppendLog(task, "开始取当前文件列表");
            var files = Directory.EnumerateFiles(task.TargetBackupDirectory, "*", SearchOption.AllDirectories);
            AppendLog(task, "共找到" + files.Count() + "个文件");
            foreach (var filePath in files)
            {
                FileInfo file = new FileInfo(filePath);
                if (!filePath.Contains("#OldBackupedFile#")
                    && file.Directory.Name != "FileList"
                    && file.Name != $"FileBackuper_Log_{task.Name}.json"
                    && file.Name != $"FileBackuper_Config_{task.Name}.json")
                {
                    str.AppendLine(filePath);
                }

            }
            str = str.Replace(task.TargetBackupDirectory, "");
            AppendLog(task, "获取当前文件列表完成");
            if (!Directory.Exists(task.TargetBackupDirectory + "\\FileList"))
            {
                Directory.CreateDirectory(task.TargetBackupDirectory + "\\FileList");
            }
            File.WriteAllText(task.TargetBackupDirectory + "\\FileList\\" + DateTime.Now.ToString(Properties.Resources.DateTimeFormat) + ".txt", str.ToString());
        }


        private string SourceFilePathToTargetFilePath(string fileFullName)
        {
            if (fileFullName.StartsWith("\\\\"))
            {

                return task.TargetBackupDirectory + "\\" + fileFullName.Replace("\\\\", "\\");
            }
            // else if(fileFullName.StartsWith("ftp://"))
            return task.TargetBackupDirectory + "\\" + fileFullName.Replace(":", "");
        }
    }
}
