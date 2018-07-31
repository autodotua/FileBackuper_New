using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace FileBackuper
{
    public class LogInfo
    {
        public DateTime Time { get;  set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskInfo.Statuses Status { get; set; }
        public string Infomation { get;  set; }
        [JsonIgnore]
        public string TaskName { get; set; }
        [JsonIgnore]
        public string DisplayStatus
        {
            get
            {
                switch(Status)
                {
                    case TaskInfo.Statuses.BackingUp:
                        return "正在备份";
                    case TaskInfo.Statuses.Error:
                        return "错误";
                    case TaskInfo.Statuses.InTheLine:
                        return "排队中";
                    case TaskInfo.Statuses.Pause:
                        return "暂停中";
                    case TaskInfo.Statuses.Waiting:
                        return "等待中";
                    default:
                        return "未知";
                }
            }
        }
    }
}


