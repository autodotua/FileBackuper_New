using System;
using System.Windows;
using System.IO;
using System.Configuration;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using static WpfControls.Dialog.DialogHelper;
using System.Linq;
using static FileBackuper.GlobalDatas;

namespace FileBackuper
{
    public partial class TaskEditWindow : Window
    {
        TaskInfo info;
        string lastConfigPath = null;
        public TaskEditWindow(TaskInfo info = null)
        {
            InitializeComponent();
            if (info == null)
            {
                this.info = new TaskInfo();

            }
            else
            {
                this.info = info;

                lastConfigPath = info.ConfigJsonPath;
            }
        }
        private void OKButtonClickEventHandler(object sender, RoutedEventArgs e)
        {
            //检查目录
            if (!Directory.Exists(txtTargetDirectory.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtTargetDirectory.Text);
                }
                catch (Exception ex)
                {
                    ShowException("目标目录输入不存在且无法创建！", ex);
                    return;
                }
            }



            TimeSpan? interval = timeInterval.TimeSpan;
            if (interval.HasValue)
            {
                info.Interval = (int)interval.Value.TotalSeconds;
            }
            else
            {
                ShowError("输入的时间有误");
                return;
            }



            info.WhiteList = new List<string>(lbxWhite.Items.Cast<string>());
            info.BlackList = new List<string>(lbxBlack.Items.Cast<string>());
            info.TargetRootDirectory = txtTargetDirectory.Text;
            info.RealTime = cbbCheckMode.SelectedIndex ==1;
            info.Name = txtName.Text;

            set.ConfigPaths.Remove(lastConfigPath);
            set.ConfigPaths.Add(info.ConfigJsonPath);
            set.Save();

            try
            {
                File.WriteAllText(info.ConfigJsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(info));
                Close();
            }
            catch (Exception ex)
            {
                ShowException("无法写入配置文件！", ex);
                return;
            }


        }


        private void WindowLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            DefautDialogOwner = this;

            foreach (var path in info.WhiteList)
            {
                lbxWhite.Items.Add(path);
            }

            foreach (var path in info.BlackList)
            {
                lbxBlack.Items.Add(path);
            }

            txtName.Text = info.Name;
            txtTargetDirectory.Text = info.TargetRootDirectory;
            timeInterval.TimeSpan = TimeSpan.FromSeconds(info.Interval);
            cbbCheckMode.SelectedIndex = info.RealTime ?1 : 0;

        }

        private void OpenFile(object sender, WpfControls.Common.StorageOperationEventArgs e)
        {
            string[] files = e.Names;
            switch ((sender as Button).Tag as string)
            {
                case "1":
                    foreach (var name in files)
                    {
                        lbxWhite.Items.Add(name);
                    }
                    break;
                case "2":
                    foreach (var name in files)
                    {
                        lbxBlack.Items.Add(name);
                    }
                    break;
                case "3":
                    txtTargetDirectory.Text = e.Name;
                    break;
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DefautDialogOwner = App.Current.MainWindow;
        }

        private void BtnDeleteClick(object sender, RoutedEventArgs e)
        {
            object seletedItem = null;
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "1":
                    seletedItem = lbxWhite.SelectedItem;

                    if (seletedItem != null)
                    {
                        lbxWhite.Items.Remove(seletedItem);
                    }
                    break;
                case "2":
                    seletedItem = lbxBlack.SelectedItem;
                    if (seletedItem != null)
                    {
                        lbxBlack.Items.Remove(seletedItem);
                    }
                    break;
            }
        }

        private void cbbCheckMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(tbkIntervalInfo==null)
            {
                return;
            }
           if ( cbbCheckMode.SelectedIndex==0)
            {
                tbkIntervalInfo.Text = "每隔：";
                //timeInterval.TimeSpan = TimeSpan.FromMinutes(10);
            }
            else
            {
                tbkIntervalInfo.Text = "缓冲时间：";
                //timeInterval.TimeSpan = TimeSpan.FromSeconds(10);
            }
        }
    }
}
