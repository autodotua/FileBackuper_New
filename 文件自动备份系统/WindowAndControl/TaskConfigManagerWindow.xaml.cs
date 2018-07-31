using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfControls.Dialog;
using static FileBackuper.GlobalDatas;

namespace FileBackuper
{
    /// <summary>
    /// TaskConfigManagerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TaskConfigManagerWindow : Window
    {
        ObservableCollection<ConfigFileInfo> configFileInfos = new ObservableCollection<ConfigFileInfo>();

        public TaskConfigManagerWindow()
        {
            InitializeComponent();
            lvw.ItemsSource = configFileInfos;

            Reset();
        }

        private void Reset()
        {
            configFileInfos.Clear();
            foreach (var path in set.ConfigPaths)
            {
                configFileInfos.Add(new ConfigFileInfo(path));
            }
        }



        private void lvw_LvwItemPreviewKeyDownEvent(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Delete)
            {
                configFileInfos.Remove(lvw.SelectedItem as ConfigFileInfo);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch((sender as Button).Content as string)
            {
                case "导入":
                    var dialog = WpfControls.Dialog.CommonFileSystemDialog.OpenDialog;
                    dialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("JSON文件", "json"));
                    dialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("所有文件", "*"));
                    dialog.EnsureFileExists = true;
                  if(  dialog.ShowDialog()==Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                    {
                        try
                        {
                            TaskInfo task = Newtonsoft.Json.JsonConvert.DeserializeObject<TaskInfo>(File.ReadAllText(dialog.FileName));
                            set.ConfigPaths.Add(dialog.FileName);
                            DialogHelper.ShowPrompt("导入成功");
                            Reset();
                        }
                        catch(Exception ex)
                        {
                            DialogHelper.ShowException("导入失败", ex);
                        }
                    }
                    break;
                case "保存":
                    set.ConfigPaths.Clear();
                    foreach (var item in configFileInfos)
                    {
                        set.ConfigPaths.Add(item.Path);
                    }
                    Close();
                    break;
                case "重置":
                    Reset();
                    break;
                case "删除":
                    if(lvw.SelectedItem is ConfigFileInfo)
                    {
                        configFileInfos.Remove(lvw.SelectedItem as ConfigFileInfo);
                    }
                    break;
            }
        }
        
    }
}
