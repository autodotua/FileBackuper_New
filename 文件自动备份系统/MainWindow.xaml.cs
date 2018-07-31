//using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using WpfControls.Dialog;
using static FileBackuper.GlobalDatas;
using static WpfControls.Dialog.DialogHelper;

namespace FileBackuper
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 字段属性声明
        public MainWindow()
        {
            DefautDialogOwner = this;
            InitializeComponent();
            lvwTasks.ItemsSource = taskInfos;//绑定任务列表数据
            chkStartup.IsChecked = WpfCodes.Program.Startup.WillRunWhenStartup("FileBackuper");
            CurrentInstance = this;
        }

        #endregion 字段属性声明

        public static MainWindow CurrentInstance { get; private set; }

        private void MainWindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {

        }


        private void NotifyIconClickEventHandler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (WindowState != WindowState.Minimized)
                {
                    //ShowInTaskbar = false;
                    WindowState = WindowState.Minimized;
                    Hide();
                }
                else
                {
                    //ShowInTaskbar = true;

                    //WindowState = WindowState.Maximized;
                    WindowState = WindowState.Normal;

                    //Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                    Show();
                    //Arrange(new Rect(DesiredSize));
                    Activate();
                    //不知道为什么，试了一个小时还是没法把窗口调出来，然后发现回车键可以，就模拟一下了
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                }
            }
            // this.Margin = new Thickness(300);

            // }
            //if (this.Visibility == Visibility.Visible)
            //{
            //    this.WindowState = WindowState.Minimized;
            //    this.Visibility = Visibility.Hidden;
            //}
            //else
            //{
            //    this.WindowState = WindowState.Normal;
            //    this.Visibility = Visibility.Visible;
            //    this.Activate();
            //}
            //  }
        }


        /// <summary>
        /// 刷新列表
        /// </summary>
        private void RefreshListView()
        {
            SaveTasks();
            background.LoadTasks();
        }





        #region 任务相关按钮等控件事件
        /// <summary>
        /// 单击新建按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            background.timer.Stop();
            new TaskEditWindow() { Owner = this }.ShowDialog();
            RefreshListView();
            background.timer.Start();
        }

        /// <summary>
        /// 单击暂停时间按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseTimerButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (background.timer.Enabled)
            {
                background.TryToStopTimer();
            }
            else
            {
                background.timer.Start();
            }
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtLogPaneMouseLeaveEventHandler(object sender, MouseEventArgs e)
        {
            //txtLogPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
        /// <summary>
        /// 单击强制执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ForceToExecuteButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            (lvwTasks.SelectedItem as TaskInfo).Status = TaskInfo.Statuses.InTheLine;
            // itemsLastTime[lvwTasks.SelectedIndex] = 0;
        }
        /// <summary>
        /// 单击删除按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            if(SelectedTask==null)
            {
                return;
            }
            if (SelectedTask.Status == TaskInfo.Statuses.BackingUp)
            {
                ShowError("无法删除正在备份的任务");
                return;
            }
            background.timer.Stop();
            switch (ShowMessage("是否要删除任务？", "请选择删除类型", DialogType.Warn, new string[] { "仅任务配置", "配置和备份的文件", "取消" }, this))
            {
                case 0:
                    set.ConfigPaths.Remove(SelectedTask.ConfigJsonPath);
                    RefreshListView();
                    break;
                case 1:
                    WpfCodes.Basic.IO.DeleteFileOrFolder(SelectedTask.TargetRootDirectory,true,true);
                    set.ConfigPaths.Remove(SelectedTask.ConfigJsonPath);
                    RefreshListView();
                    break;
            }
           
            background.timer.Start();

        }
        /// <summary>
        /// 单击完全退出按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).TryExit();
            Button btn = sender as Button;
            btn.IsEnabled = false;
            btn.Content = "正在退出";
        }
        /// <summary>
        /// 单击编辑按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditTaskButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            background.timer.Stop();
            new TaskEditWindow(lvwTasks.SelectedItem as TaskInfo) { Owner = this }.ShowDialog();
            RefreshListView();
            background.timer.Start();


        }
        /// <summary>
        /// 双击列表项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvwTasksItemPreviewMouseDoubleClickEventHandler(object sender, MouseButtonEventArgs e)
        {
            btnForceToExecute.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        /// 在列表项上按下按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvwTaskItemPreviewKeyDownEventHandler(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.Enter:
                    btnEditTask.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Delete:
                    btnDeleteTask.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Space:
                    btnForceToExecute.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
            }

        }

        private void LvwTaskPreviewMouseLeftButtonUpEventHandler(object sender, MouseButtonEventArgs e)
        {
            if (lvwTasks.SelectedIndex != -1)
            {
                btnDeleteTask.IsEnabled = true;
                btnEditTask.IsEnabled = true;
                btnForceToExecute.IsEnabled = true;
                btnPauseCurrent.IsEnabled = true;
                btnOpenTargetDirectory.IsEnabled = true;
            }
        }
        #endregion 任务相关按钮等控件事件

        

        #region 程序相关事件
        private void MainWindowClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            set.Save();
            CurrentInstance = null;
        }

        #endregion 程序相关事件

        private void CbxStartupClickEventHandler(object sender, RoutedEventArgs e)
        {
            bool ok = false;
            if (chkStartup.IsChecked.Value && (!WpfCodes.Program.Startup.WillRunWhenStartup("FileBackuper")))
            {
                ok = WpfCodes.Program.Startup.SetRunWhenStartup("FileBackuper", "noWindow");
            }
            else if ((!chkStartup.IsChecked.Value) && WpfCodes.Program.Startup.WillRunWhenStartup("FileBackuper"))
            {
                ok = WpfCodes.Program.Startup.CancelRunWhenStartup("FileBackuper");
            }

            if (!ok)
            {
                ShowError("设置开机自启失败");
            }
        }

        private void cbxMinimumClickEventHandler(object sender, RoutedEventArgs e)
        {
        }

        private void PauseCurrentTimerButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            foreach (TaskInfo info in lvwTasks.SelectedItems)
            {
                if (info.Status == TaskInfo.Statuses.Pause)
                {
                    info.Status = TaskInfo.Statuses.Waiting;

                }
                else if (info.Status == TaskInfo.Statuses.BackingUp)
                {
                    background.StopCurrentTask();
                }
                else
                {
                    info.Status = TaskInfo.Statuses.Pause;
                    info.DisplayStatus = "暂停中";
                }
            }
        }

        private void btnOpenTargetDirectoryClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (SelectedTask != null)
            {
                Process.Start("explorer.exe",SelectedTask.TargetRootDirectory);
            }
        }

        private void ListViewSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetButtons();

        }

        public void ResetButtons()
        {
            btnDeleteTask.IsEnabled = false;
            btnEditTask.IsEnabled = false;
            btnForceToExecute.IsEnabled = false;
            btnPauseCurrent.IsEnabled = false;
            btnOpenTargetDirectory.IsEnabled = false;
            btnRestore.IsEnabled = false;
            if (!(lvwTasks.SelectedItem is TaskInfo task))
            {
                return;
            }
            btnOpenTargetDirectory.IsEnabled = true;
            btnPauseCurrent.IsEnabled = true;
            if (task.Status != TaskInfo.Statuses.BackingUp)
            {
                btnDeleteTask.IsEnabled = true;
                btnEditTask.IsEnabled = true;
                btnForceToExecute.IsEnabled = true;
                btnRestore.IsEnabled = true;
            }
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            TaskInfo task = lvwTasks.SelectedItem as TaskInfo;
            if (task.Status == TaskInfo.Statuses.BackingUp)
            {
                ShowError("无法在任务进行备份时打开还原窗口");
                return;
            }
            background.timer.Stop();
            new RestoreWindow(task) { Owner = this }.ShowDialog();
            background.timer.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (background.IsRunning)
            {
                ShowError("无法在任务进行备份时打开配置管理窗口");
                return;
            }

            background.timer.Stop();
            new TaskConfigManagerWindow() { Owner = this }.Show();
            background.timer.Start();
        }

        public TaskInfo SelectedTask => lvwTasks.SelectedItem as TaskInfo;
    }
}


