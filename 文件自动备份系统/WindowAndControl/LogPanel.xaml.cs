using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static WpfControls.Dialog.DialogHelper;

namespace FileBackuper
{
    /// <summary>
    /// SimpleLog.xaml 的交互逻辑
    /// </summary>
    public partial class LogPanel : UserControl
    {
        public LogPanel()
        {
            LogHelper.panel = this;
            InitializeComponent();
            lvwSimpleLog.ItemsSource = logInfos;
            lvwHistoryLog.ItemsSource = historyLogs;

        }

        ObservableCollection<LogInfo> logInfos = new ObservableCollection<LogInfo>();

        public void Add(LogInfo info)
        {
            Dispatcher.Invoke(() =>
            {
                if (logInfos.Count > GlobalDatas.set.MaxLogLines)
                {
                    logInfos.RemoveAt(0);
                }
                logInfos.Add(info);

                if (!lvwSimpleLog.IsFocused)
                {
                    lvwSimpleLog.ScrollIntoView(info);
                }
            });
        }



        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbxHistory.Items.Clear();
            TaskInfo info = MainWindow.CurrentInstance.SelectedTask;
            if (info == null)
            {
                return;
            }
            foreach (var item in LogHelper.GetLogs(info))
            {
                lbxHistory.Items.Add(new ListBoxItem() { Content = item.Key, Tag = item.Value });
            }
        }

        ObservableCollection<LogInfo> historyLogs = new ObservableCollection<LogInfo>();

        private async void lbxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            historyLogs.Clear();

            ListBoxItem item = lbxHistory.SelectedItem as ListBoxItem;

            TaskInfo task = MainWindow.CurrentInstance.SelectedTask;
            if (item == null)
            {
                return;
            }

            FileInfo file = item.Tag as FileInfo;

            List<LogInfo> logList = new List<LogInfo>();
            Exception ex = null;
            await Task.Run(() =>
            {
                try
                {
                    logList = LogHelper.GetLogContent(task, file);
                }
                catch (Exception ex2)
                {
                    ex = ex2;
                }
            });

            if (ex != null)
            {
                ShowException("读取历史日志错误", ex);
                return;
            }
            if (logList != null)
            {
                foreach (var log in logList)
                {
                    historyLogs.Add(log);
                }
            }

        }
    }
}
