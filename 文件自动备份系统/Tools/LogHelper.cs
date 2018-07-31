using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WpfCodes.Basic.String;

namespace FileBackuper
{
    public class LogHelper
    {
        public static LogPanel panel;

        public static Dictionary<TaskInfo, LogHelper> logHelpers = new Dictionary<TaskInfo, LogHelper>();

        public static void AppendLog(TaskInfo task, string info)
        {
            LogHelper log;
            if (!logHelpers.ContainsKey(task))
            {
                log = new LogHelper(task);
                logHelpers.Add(task, log);
            }
            else
            {
                log = logHelpers[task];
            }
            log.AppendLog(info);
        }
        public static void Save(TaskInfo task)
        {
            if (!logHelpers.ContainsKey(task))
            {
                throw new Exception("还未初始化");
            }
            else
            {
                logHelpers[task].Save();
            }
        }


        public static Dictionary<DateTime, FileInfo> GetLogs(TaskInfo task)
        {
            Dictionary<DateTime, FileInfo> fileTimes = new Dictionary<DateTime, FileInfo>();

            foreach (var filePath in Directory.EnumerateFiles(task.TargetRootDirectory + "\\" + "Logs"))
            {
                FileInfo file = new FileInfo(filePath);
                string timeString = file.Name.RemoveEnd(file.Extension);
                try
                {
                    fileTimes.Add(DateTime.ParseExact(timeString, Properties.Resources.DateTimeFormat, CultureInfo.CurrentCulture), file);

                }
                catch
                {

                }
            }
            return fileTimes;
        }

        public static List<LogInfo> GetLogContent(TaskInfo task, FileInfo file)
        {
            LogHelper logHelper = new LogHelper(task, file);
            return logHelper.logInfos;
        }



        public static string GetLogJsonPath(TaskInfo task, DateTime time)
        {
            return task.TargetRootDirectory + "\\" + "Logs\\" + time.ToString(Properties.Resources.DateTimeFormat) + ".json";
        }

        public List<LogInfo> logInfos = new List<LogInfo>();

        private TaskInfo task;

        public DateTime LogTime { get; set; }

        private LogHelper(TaskInfo task)
        {
            if (logHelpers.ContainsKey(task))
            {
                throw new Exception("重复的实例");
            }
            this.task = task;
            LogTime = DateTime.Now;
            FileInfo logFile = new FileInfo(GetLogJsonPath(task, LogTime));
            if (!logFile.Exists)
            {
                if (!logFile.Directory.Exists)
                {
                    logFile.Directory.Create();
                }
                logFile.Create();
            }
            //try
            //{
            //    logInfos = JsonConvert.DeserializeObject<List<LogInfo>>(File.ReadAllText(task.LogJsonPath));
            //}
            //catch (Exception ex)
            //{
            //    WpfControls.Dialog.DialogHelper.ShowException($"任务{task.Name}的日志无法读取", ex);
            //}

            lastSaveTime = DateTime.Now;
        }

        private LogHelper(TaskInfo task, FileInfo logFile)
        {
            this.task = task;
            LogTime = DateTime.ParseExact(logFile.Name.RemoveEnd(".json"), Properties.Resources.DateTimeFormat, CultureInfo.CurrentCulture);
            if (!logFile.Exists)
            {
                throw new FileNotFoundException("不存在此日志文件");
            }
            else
            {
                logInfos = JsonConvert.DeserializeObject<List<LogInfo>>(File.ReadAllText(logFile.FullName));
            }

        }


        private DateTime lastSaveTime;

        private void AppendLog(string info)
        {
            DateTime now = DateTime.Now;
            LogInfo logInfo = new LogInfo()
            {
                Time = now,
                Status = task.Status,
                Infomation = info,
                TaskName = task.Name,
            };
            logInfos.Add(logInfo);
            App.Current.Dispatcher.Invoke(() =>
            {
                if (App.Current.MainWindow != null)
                {
                    panel.Add(logInfo);
                }
            });
            //if((DateTime.Now- lastSaveTime).TotalSeconds>60)
            //{
            //   File.WriteAllText(JsonFilePath, JsonConvert.SerializeObject(logInfos));
            //    lastSaveTime = DateTime.Now;
            //}
        }

        private void Save()
        {
            try
            {

                File.WriteAllText(GetLogJsonPath(task, LogTime), JsonConvert.SerializeObject(logInfos));
                lastSaveTime = DateTime.Now;
            }
            catch { }
        }
    }
}
