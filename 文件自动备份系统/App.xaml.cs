using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows;
using static FileBackuper.GlobalDatas;

namespace FileBackuper
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        protected async override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            //base.OnStartup(e);
            new WpfCodes.Program.Exception().UnhandledException += (p1, p2) =>
              {
                  try
                  {
                      Dispatcher.Invoke(() => WpfControls.Dialog.DialogHelper.ShowException("程序发生了未捕获的错误，类型" + p2.Source.ToString(), p2.Exception));

                      File.AppendAllText("Exception.log", Environment.NewLine + Environment.NewLine + DateTime.Now.ToString() + Environment.NewLine + p2.Exception.ToString());
                  }
                  catch (Exception ex)
                  {
                      Dispatcher.Invoke(() => WpfControls.Dialog.DialogHelper.ShowException("错误信息无法写入", ex));
                  }
                  finally
                  {
                      App.Current.Shutdown();
                  }
              };


          if(await  WpfCodes.Program.Startup.CheckAnotherInstanceAndOpenWindow<MainWindow>("FileBackuper", this))
            {
                return;
            }

#endif

            if (!(e.Args.Length == 1 && e.Args[0] == "noWindow"))
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
                MainWindow.Closed += (p1, p2) =>
                  {
                      MainWindow = null;
                  };
            }

            Dictionary<string, Action> trayRightButtonActions = new Dictionary<string, Action>()
            {
                {"退出",TryExit }
            };


            tray = new WpfCodes.Program.TrayIcon(FileBackuper.Properties.Resources.Icon, "文件自动备份系统", TrayOpenWindow, trayRightButtonActions);
            tray.Show();
            background.timer.Start();


            void TrayOpenWindow()
            {
                if (MainWindow == null)
                {
                    MainWindow = new MainWindow();
                    MainWindow.Show();
                }
            }
        }

        public void TryExit()
        {
            if(!background.IsRunning)
            {
                Shutdown();
            }
            background.TaskComplete += (p1, p2) =>
            {
                Dispatcher.Invoke(() => Application.Current.Shutdown());
            };
            background.StopCurrentTask();
        }



        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Exception ex = SaveTasks();
            if (ex!=null)
            {
                WpfControls.Dialog.DialogHelper.ShowException("部分配置文件无法写入！", ex);
            }
        
            tray?.Dispose();
        }
    }
}
