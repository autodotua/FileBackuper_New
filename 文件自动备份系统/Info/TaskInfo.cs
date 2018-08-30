using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileBackuper
{
    public class TaskInfo : INotifyPropertyChanged
    {
        public string Name { get; set; } = "任务名称";
        public List<string> WhiteList { get; set; } = new List<string>();
        [JsonIgnore]
        public string DisplayWhiteList => String.Join("、",WhiteList);
        public List<string> BlackList { get; set; } = new List<string>();
        public string TargetRootDirectory { get; set; } = "";
        public bool RealTime { get; set; }
        [JsonIgnore]
        public string TargetBackupDirectory
        {
            get
            {
                if(targetBackupDirectory==null)
                {
                    InitializeTargetBackupDirectory();
                }
                return targetBackupDirectory;
            }
        }
        private string targetBackupDirectory;
        public void InitializeTargetBackupDirectory()
        {
            var directories = GetFullBackupDirectories();
            if (directories.Count() > 0)
            {
                targetBackupDirectory = directories.Last().Key.FullName;
            }
            else
            {
                DateTime now = DateTime.Now;
                targetBackupDirectory = Directory.CreateDirectory(TargetRootDirectory + "\\" + now.ToString(Properties.Resources.DateTimeFormat)).FullName;
            }
        }
        
        public IOrderedEnumerable<KeyValuePair<DirectoryInfo, DateTime>> GetFullBackupDirectories()
        {
                Dictionary<DirectoryInfo, DateTime> directories = new Dictionary<DirectoryInfo, DateTime>();
                foreach (var path in Directory.EnumerateDirectories(TargetRootDirectory))
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    if (DateTime.TryParseExact(directory.Name, Properties.Resources.DateTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result))
                    {
                        directories.Add(directory, result);
                    }
                }
                return directories.OrderBy(p => p.Value);
        }

        public int Interval { get; set; } = 60;
        [JsonIgnore]
        public string DisplayInterval => WpfCodes.Basic.Number.SecondToFitString(Interval);
        [JsonConverter(typeof(StringEnumConverter))]
        public Statuses Status { get; set; } = Statuses.Waiting;
        // public string LastResult { get;  set; }
        public bool Ignore { get; set; } = false;
        public DateTime LastBackupTime { get; set; } = DateTime.MinValue;
        [JsonIgnore]
        public string ConfigJsonPath => TargetRootDirectory + "\\" + "FileBackuper_Config_" + Name + ".json";


        [JsonIgnore]
        public string DisplayStatus
        {
            get => displayStatus;
            set
            {
                displayStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayStatus"));
            }
        }

        private string displayStatus;

        public TaskInfo()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public enum Statuses
        {
            BackingUp,
            Pause,
            Waiting,
            InTheLine,
            Error
        }
    }
}


