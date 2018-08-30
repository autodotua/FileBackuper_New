using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfCodes.Program;

namespace FileBackuper
{
    public class Settings : SettingsBase
    {

        public List<string> ConfigPaths { get; set; } = new List<string>();

        public int MaxLogLines { get; set; } = 1000;
    }
}
