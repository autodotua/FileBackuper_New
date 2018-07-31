using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static FileBackuper.GlobalDatas;

namespace FileBackuper
{
    public class BackgroundWork:IDisposable
    {
        Task existTask = null;
        BackupCore currentCore;

        public Timer timer;

        private bool needToStop = false;

        public delegate void TaskCompleteEventHandler(object sender, TaskCompleteEventArgs e);
        public event TaskCompleteEventHandler TaskComplete;

        public void TryToStopTimer()
        {
            if (existTask == null)
            {
                timer.Stop();
            }
            else
            {
                needToStop = true;
            }
        }

        public BackgroundWork()
        {
            LoadTasks();
            timer = new Timer(1000);
            timer.Elapsed += MainTimerTickEventHandler;


        }

        public void LoadTasks()
        {
            taskInfos.Clear();
            foreach (var jsonPath in set.ConfigPaths)
            {
                if (!File.Exists(jsonPath))
                {
                    continue;
                }
                try
                {
                    TaskInfo task = Newtonsoft.Json.JsonConvert.DeserializeObject<TaskInfo>(File.ReadAllText(jsonPath));
                    LogHelper.AppendLog(task, "加载成功");
                    if (task.Status == TaskInfo.Statuses.BackingUp || task.Status == TaskInfo.Statuses.InTheLine)
                    {
                        task.Status = TaskInfo.Statuses.Waiting;
                    }
                    taskInfos.Add(task);
                }
                catch(Exception ex)
                {
                    WpfControls.Dialog.DialogHelper.ShowException($"无法加载位于{jsonPath}的配置文件", ex);
                }
            }
        }

        private void MainTimerTickEventHandler(object sender, ElapsedEventArgs e)
        {
            foreach (var task in taskInfos)
            {
                DateTime now = DateTime.Now;
                switch (task.Status)
                {
                    case TaskInfo.Statuses.BackingUp:
                        task.DisplayStatus = currentCore.status;
                        break;
                    case TaskInfo.Statuses.InTheLine:
                        TryBackup(task);
                        break;
                    case TaskInfo.Statuses.Waiting:
                        double seconds = (now - task.LastBackupTime).TotalSeconds;
                        if (seconds >= task.Interval)
                        {
                            TryBackup(task);
                        }
                        else
                        {
                            task.DisplayStatus = "剩余" + WpfCodes.Basic.Number.SecondToFitString(task.Interval - (int)seconds, false);
                        }
                        break;
                    case TaskInfo.Statuses.Error:
                        task.DisplayStatus = "错误";
                        break;
                }

            }

        }

        private void TryBackup(TaskInfo task)
        {
            DateTime now = DateTime.Now;
            if (existTask == null)//如果没有正在备份
            {
                //if (!Directory.Exists(task.TargetDirectory))//如果不存在目标目录
                //{
                //    try
                //    {
                //        //尝试去创建
                //        Directory.CreateDirectory(task.TargetDirectory);
                //    }
                //    catch
                //    {
                //        task.LastBackupTime = now;
                //        //  AppendLog(itemsName[i], "目标目录不存在且无法创建，将在下一个周期重试。");
                //        //  txtLogPanel.Text = log.ToString();
                //        //  lvwTasks.Items.Refresh();
                //        return;
                //    }

                //}
                //else//如果目录存在
                //{
                    //如果校验失败

                    task.Status = TaskInfo.Statuses.BackingUp;

                    currentCore = new BackupCore(task);
                    Task t = new Task(currentCore.Backup);
                    existTask = t;
                    t.ContinueWith(p =>
                        {
                            existTask = null;
                            task.Status = TaskInfo.Statuses.Waiting;
                            task.LastBackupTime = DateTime.Now;
                            if (needToStop)
                            {
                                timer.Stop();
                                needToStop = false;
                            }
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                if (App.Current.MainWindow as MainWindow != null && App.Current.MainWindow.Visibility == System.Windows.Visibility.Visible)
                                {
                                    (App.Current.MainWindow as MainWindow).ResetButtons();
                                }

                            });
                            TaskComplete?.Invoke(this, new TaskCompleteEventArgs(task));
                        });
                    t.Start();

               // }

            }
            else//如果正在备份其他的东西
            {
                task.DisplayStatus = "排队中";

                LogHelper.AppendLog(task, "因已有工作中的备份任务，该任务正在排队等待");
                task.Status = TaskInfo.Statuses.InTheLine;
            }
        }

        public bool IsRunning => existTask != null;

        public void StopCurrentTask()
        {
            if (existTask == null)
            {
                return;
            }
            currentCore.TryToStop();
        }

        public void Dispose()
        {
            timer.Stop();
        }

        public class TaskCompleteEventArgs : EventArgs
        {
            TaskInfo task;

            public TaskCompleteEventArgs(TaskInfo task) => this.task = task;
        }
    }

}

