using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileBackuper
{
    public static class GlobalDatas
    {
        public static Settings set = new Settings();

        public static ObservableCollection<TaskInfo> taskInfos = new ObservableCollection<TaskInfo>();//需要绑定的数据

       public static BackgroundWork background = new BackgroundWork();

        public static WpfCodes.Program.TrayIcon tray;

        public static Exception SaveTasks()
        {
            try
            {
                foreach (var task in taskInfos)
                {
                    File.WriteAllText(task.ConfigJsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(task));
                }
                return null;
            }
            catch(Exception ex)
            {
                return ex;
            }
        }
    }
}
