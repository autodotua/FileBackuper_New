using System.IO;
using System.Linq;
using static FileBackuper.GlobalDatas;

namespace FileBackuper
{
    class ConfigFileInfo
    {
        public ConfigFileInfo(string path)
        {
            Path = path;
            if (File.Exists(path) && (task = taskInfos.FirstOrDefault(p => p.ConfigJsonPath == path)) != null)
            {
                Exist = "√";
                Name = task.Name;
            }
        }
        TaskInfo task;
        public string Exist { get; private set; } = "×";
        public string Name { get; private set; } = "";
        public string Path { get; private set; }
    }
}
