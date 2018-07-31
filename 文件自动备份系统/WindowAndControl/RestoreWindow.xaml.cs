using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Shapes;
using static WpfCodes.Basic.String;

namespace FileBackuper
{
    /// <summary>
    /// RestoreWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RestoreWindow : Window, INotifyPropertyChanged
    {
        public class FullBackupInfo
        {
            public FullBackupInfo(DirectoryInfo directory, DateTime dateTime)
            {
                Directory = directory;
                DateTime = dateTime;
            }

            public DirectoryInfo Directory { get; set; }
            public DateTime DateTime { get; set; }

            public override string ToString() => DateTime.ToString();
        }
        public RestoreWindow(TaskInfo task)
        {
            this.task = task;
            InitializeComponent();
            DataContext = this;
        }

        TaskInfo task;
        private FullBackupInfo currentBackup;
        public FullBackupInfo CurrentBackup
        {
            get => currentBackup;
            set
            {
                currentBackup = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentBackup"));
                lbxBackupedTime.Items.Clear();
                if (value == null || !Directory.Exists(currentBackup.Directory.FullName + "\\FileList"))
                {
                    return;
                }
                foreach (var path in Directory.EnumerateFiles(currentBackup.Directory.FullName + "\\FileList"))
                {
                    FileInfo file = new FileInfo(path);
                    ListBoxItem item = new ListBoxItem();
                    try
                    {
                        string dateString = file.Name.Replace(file.Extension, "");
                        DateTime time = DateTime.ParseExact(dateString, Properties.Resources.DateTimeFormat, CultureInfo.CurrentCulture);
                        item.Content = time;
                        item.Tag = path;
                        lbxBackupedTime.Items.Add(item);
                    }
                    catch
                    {

                    }
                }

            }
        }


        public ObservableCollection<FullBackupInfo> FullBackupDatetimeList { get; set; } = new ObservableCollection<FullBackupInfo>();

        public ObservableCollection<FileTree> FileInfos { get; set; } = new ObservableCollection<FileTree>();

        private void BackupedTimeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            treeFiles.Items.Clear();
            ListBoxItem selectedItem = lbxBackupedTime.SelectedItem as ListBoxItem;
            if (selectedItem == null)
            {
                return;
            }
            RestoreCore core = new RestoreCore(task, selectedItem.Tag as string, (DateTime)selectedItem.Content);
            FileTree tree = core.GetFileTree();
            TreeViewItem item = new TreeViewItem();
            treeFiles.Items.Add(item);
            item.Header = selectedItem.Content.ToString();
            ShowFileTreeOnTreeView(tree, item);
            ItemCollection items;
            do
            {
                items = item.Items;
                if (items.Count == 1)
                {
                    item.IsExpanded = true;
                    item = items[0] as TreeViewItem;
                }
            } while (items.Count == 1);
            item.IsExpanded = true;
            item.IsSelected = true;
        }

        private void ShowFileTreeOnTreeView(FileTree tree, TreeViewItem item)
        {
            TreeViewItem childItem = new TreeViewItem();
            childItem.Header = tree.Name;
            childItem.Tag = tree;
            childItem.ContextMenu = FindResource("treeItemMenu") as ContextMenu;
            childItem.PreviewMouseRightButtonDown += (p1, p2) => childItem.IsSelected = true;
            item.Items.Add(childItem);
            foreach (var childTree in tree.SubFileTrees)
            {
                if (!childTree.IsFile)
                {
                    ShowFileTreeOnTreeView(childTree, childItem);
                }
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            FullBackupDatetimeList.Clear();
            foreach (var item in task.GetFullBackupDirectories())
            {
                FullBackupDatetimeList.Add(new FullBackupInfo(item.Key, item.Value));
            }
            if (FullBackupDatetimeList.Count > 0)
            {
                CurrentBackup = FullBackupDatetimeList.Last();
            }

            //FullBackupDatetimeList = new ObservableCollection<KeyValuePair<DirectoryInfo, DateTime>>();

        }

        private void TreeFilesSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is TreeViewItem item))
            {
                return;
            }
            FileInfos.Clear();
            if (!(item.Tag is FileTree files))
            {
                return;
            }
            foreach (var file in files.SubFileTrees)
            {
                FileInfos.Add(file);
            }
        }

        private void FileListMouseLeftDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileTree tree = lvwFiles.SelectedItem as FileTree;
            if (tree.IsFile)
            {
                Process.Start(tree.File.FullName);
            }
            else
            {
                if (!(treeFiles.SelectedItem is TreeViewItem item))
                {
                    return;
                }
                TreeViewItem childItem = item.Items.Cast<TreeViewItem>().First(p => (p.Tag as FileTree) == tree);
                if (childItem != null)
                {
                    item.IsExpanded = true;
                    childItem.IsSelected = true;
                }
            }
        }
        List<FileInfo> exportingFiles;

        ObservableCollection<TextBlock> logs = new ObservableCollection<TextBlock>();

        string sumLengthString = "";

        public event PropertyChangedEventHandler PropertyChanged;

        private async void MenuTreeViewExportClick(object sender, RoutedEventArgs e)
        {
            Grid.SetColumn(grdExport, 4);
            treeFiles.IsEnabled = false;
            await ExportFolderAsync((treeFiles.SelectedItem as TreeViewItem).Tag as FileTree);
            treeFiles.IsEnabled = true;
        }

        public async Task ExportFolderAsync(FileTree fileTree)
        {
            if (fileTree.IsFile)
            {
                throw new FormatException();
            }
            CommonOpenFileDialog dialog = WpfControls.Dialog.CommonFileSystemDialog.OpenDialog;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                logs.Clear();
                lbxLog.ItemsSource = logs;
                exportingFiles = new List<FileInfo>();
                GetAllFiles(fileTree);
                grdExport.Visibility = Visibility.Visible;
                Dictionary<FileInfo, long> fileLengths = new Dictionary<FileInfo, long>();
                long lengthSum = 0;
                await Task.Run(() =>
                {
                    foreach (var file in exportingFiles)
                    {
                        try
                        {
                            long length = file.Length;
                            fileLengths.Add(file, length);
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => logs.Add(new TextBlock() { Text = $"获取文件{file.FullName}长度失败：{ex.Message}", Foreground = Brushes.Red }));
                        }
                    }
                    lengthSum = fileLengths.Sum(p => p.Value);
                    sumLengthString = WpfCodes.Basic.Number.ByteToFitString(lengthSum);
                });
                int count = fileLengths.Count;
                int index = 0;
                long currentSumLength = 0;
                pgbFileCopy.Maximum = lengthSum;
                pgbFileCopy.Value = 0;

                string directory = dialog.FileName;
                await Task.Run(() =>
                {
                    foreach (var file in fileLengths)
                    {
                        index++;
                        currentSumLength += file.Value;
                        try
                        {
                            string name = file.Key.Name;
                            if (name.StartsWith("#OldBackupedFile#") && name.Length > 33)
                            {
                                name = name.Substring(33);
                            }
                            string newDirectory = directory + file.Key.DirectoryName.RemoveStart(CurrentBackup.Directory.FullName);
                            if (!Directory.Exists(newDirectory))
                            {
                                Directory.CreateDirectory(newDirectory);
                            }
                            CopyFile(file.Key.FullName, newDirectory + "\\" + name);

                            Dispatcher.Invoke(() => logs.Add(new TextBlock() { Text = $"复制{name}成功" }));
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                pgbFileCopy.Foreground = Brushes.Red;
                                logs.Add(new TextBlock() { Text = $"复制{file.Key.FullName}失败：{ex.Message}", Foreground = Brushes.Red });
                            });
                        }
                        finally
                        {
                            Dispatcher.Invoke(() =>
                            {
                                tbkLeft.Text = index + "/" + count;
                                pgbFileCopy.Value = currentSumLength;
                                lbxLog.SelectedIndex = logs.Count - 1;
                            });

                        }
                    }
                });
            }

        }

        public async Task ExportFileAsync(FileTree fileTree)
        {
            if (!fileTree.IsFile)
            {
                throw new FormatException();
            }
            CommonSaveFileDialog dialog = WpfControls.Dialog.CommonFileSystemDialog.SaveDialog;
            string name = fileTree.File.Name;
            if (name.StartsWith("#OldBackupedFile#") && name.Length > 33)
            {
                name = name.Substring(33);
            }
            dialog.DefaultFileName = name;
            dialog.Filters.Add(new CommonFileDialogFilter("*", "所有文件"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                logs.Clear();
                lbxLog.ItemsSource = logs;
                grdExport.Visibility = Visibility.Visible;
                long length = 0;

                try
                {
                    length = fileTree.File.Length;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => logs.Add(new TextBlock() { Text = $"获取文件{fileTree.File.Name}长度失败：{ex.Message}", Foreground = Brushes.Red }));
                    return;
                }
                pgbFileCopy.Maximum = length;
                pgbFileCopy.Value = 0;

                string directory = dialog.FileName;
                try
                {
                    await Task.Run(() =>
                    {
                        CopyFile(fileTree.File.FullName, dialog.FileName);
                    });
                    logs.Add(new TextBlock() { Text = $"复制{name}成功" });
                }
                catch (Exception ex)
                {
                    pgbFileCopy.Foreground = Brushes.Red;
                    logs.Add(new TextBlock() { Text = $"复制{fileTree.File.FullName}失败：{ex.Message}", Foreground = Brushes.Red });
                }
            }

        }


        private void CopyFile(string source, string target)
        {
            using (FileStream fsRead = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fsWrite = new FileStream(target, FileMode.Create, FileAccess.Write))
                {
                    long value = 0;
                    Dispatcher.Invoke(() => value = (long)pgbFileCopy.Value);
                    long bufferLength = 1024 * 1024;
                    byte[] bytes = new byte[bufferLength];
                    int receivedLength = fsRead.Read(bytes, 0, bytes.Length);

                    while (receivedLength > 0)
                    {
                        fsWrite.Write(bytes, 0, receivedLength);
                        value += receivedLength;
                        Dispatcher.Invoke(() =>
                        {
                            pgbFileCopy.Value = value;
                            tbkRight.Text = WpfCodes.Basic.Number.ByteToFitString(value) + "/" + sumLengthString;
                        });
                        receivedLength = fsRead.Read(bytes, 0, bytes.Length);

                    }
                }

            }
        }

        private void GetAllFiles(FileTree tree)
        {
            if (tree.IsFile)
            {
                exportingFiles.Add(tree.File);
            }
            else
            {
                foreach (var child in tree.SubFileTrees)
                {
                    GetAllFiles(child);
                }
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            grdExport.Visibility = Visibility.Hidden;
        }

        private void lvwFiles_ListViewItemMouseRightButtonUpEvent(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = FindResource("listViewItemMenu") as ContextMenu;
            menu.IsOpen = true;
        }

        private void MenuOpenFolderOpen(object sender, RoutedEventArgs e)
        {
            Process.Start(((treeFiles.SelectedItem as TreeViewItem).Tag as FileTree).Directory.FullName);
        }

        private async void MenuListViewExportClick(object sender, RoutedEventArgs e)
        {
            FileTree tree = lvwFiles.SelectedItem as FileTree;
            Grid.SetColumn(grdExport, 2);
            lvwFiles.IsEnabled = false;
            if (tree.IsFile)
            {
                await ExportFileAsync(tree);
            }
            else
            {
                await ExportFolderAsync(tree);
            }

            lvwFiles.IsEnabled = true;
        }

        private async void FullBackupOperationButtonClick(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag as string)
            {
                case "1":
                    Directory.CreateDirectory(task.TargetRootDirectory + "\\" + DateTime.Now.ToString(Properties.Resources.DateTimeFormat));
                    task.InitializeTargetBackupDirectory();
                    WindowLoaded(null, null);
                    GlobalDatas.background.Dispose();
                    GlobalDatas.background = new BackgroundWork();
                    break;
                case "2":
                    loading.Show();

                    long sumLength = 0;
                    int sumCount = 0;
                    await Task.Run(() =>
                     {
                         foreach (var path in Directory.EnumerateFiles(CurrentBackup.Directory.FullName, "*", SearchOption.AllDirectories))
                         {
                             sumCount++;
                             sumLength += new FileInfo(path).Length;
                         }
                     });

                    loading.Hide();
                    if (WpfControls.Dialog.DialogHelper.ShowMessage("是否删除当前备份？", $"共{sumCount}个，{WpfCodes.Basic.Number.ByteToFitString(sumLength)}{Environment.NewLine}备份将移至回收站", WpfControls.Dialog.DialogType.Warn, MessageBoxButton.YesNo, this) == 0)
                    {
                        WpfCodes.Basic.IO.DeleteFileOrFolder(CurrentBackup.Directory.FullName, true, true);
                        task.InitializeTargetBackupDirectory();
                        WindowLoaded(null, null);
                        GlobalDatas.background.Dispose();
                        GlobalDatas.background = new BackgroundWork();
                    }
                    break;
            }
        }
    }
}
