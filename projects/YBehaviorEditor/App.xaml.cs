using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    /// 

    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(OnUnhandledException);

            CompositionTarget.Rendering += _ProcessConsoleKey;

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            FileMgr.Instance.Load();

            if (e.Args != null && e.Args.Length > 0)
            {
                int i = 0;
                string s = e.Args[i];
                do
                {
                    switch (s)
                    {
                        case "-f":
                            {
                                List<string> list = new List<string>();
                                while(++i < e.Args.Length)
                                {
                                    s = e.Args[i];
                                    if (s.StartsWith("-"))
                                        break;
                                    list.Add(s);
                                }
                                if (list.Count > 0)
                                    WorkBenchMgr.Instance.OpenAList(list);
                            }
                            break;
                        default:
                            LogMgr.Instance.Error("Invalid arg: " + s);
                            ++i;
                            if (i < e.Args.Length)
                                s = e.Args[i];
                            break;
                    }
                }
                while (i < e.Args.Length);
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string exceptionStr = e.ExceptionObject.ToString();
            MessageBox.Show(exceptionStr);
        }

        StringBuilder sb = new StringBuilder();
        void _ProcessConsoleKey(object sender, EventArgs e)
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        if (sb.Length > 0)
                        {
                            _ProcessCommand(sb.ToString());
                            LogMgr.Instance.LogEnd();
                            sb.Clear();
                        }
                        break;
                    case ConsoleKey.Backspace:
                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);
                        }
                        break;
                    default:
                        if ((short)key.KeyChar != 0)
                        {
                            sb.Append(key.KeyChar);
                        }
                        break;
                }

            }
        }

        void _ProcessCommand(string s)
        {
            switch (s)
            {
                case "loadandsavealltrees":
                    _LoadAndSaveAll();
                    break;
                case "checklocal":
                    _CheckLocal();
                    break;
                case "clear":
                    Console.Clear();
                    break;
            }
        }

        void _LoadAndSaveAll()
        {
            WorkBench oldActiveBench = WorkBenchMgr.Instance.ActiveWorkBench;
            foreach (var treename in FileMgr.Instance.TreeList)
            {
                if (string.IsNullOrEmpty(treename))
                    continue;
                var fileinfo = FileMgr.Instance.GetFileInfo(treename);
                if (fileinfo == null)
                {
                    LogMgr.Instance.Error("Cant find " + treename);
                    continue;
                }

                var workbench = WorkBenchMgr.Instance.OpenWorkBenchTemp(fileinfo);
                int res = WorkBenchMgr.Instance.SaveWorkBench(workbench);
                if ((res & WorkBenchMgr.SaveResultFlag_Saved) != 0)
                {
                    WorkBenchMgr.Instance.ExportWorkBench(workbench);

                    LogMgr.Instance.Log(treename + " saved and exported.");
                }
            }
            WorkBenchMgr.Instance.ActiveWorkBench = oldActiveBench;
        }
        void _CheckLocal()
        {
            WorkBench oldActiveBench = WorkBenchMgr.Instance.ActiveWorkBench;
            foreach (var treename in FileMgr.Instance.TreeList)
            {
                if (string.IsNullOrEmpty(treename))
                    continue;
                var fileinfo = FileMgr.Instance.GetFileInfo(treename);
                if (fileinfo == null)
                {
                    LogMgr.Instance.Error("Cant find " + treename);
                    continue;
                }

                var workbench = WorkBenchMgr.Instance.OpenWorkBenchTemp(fileinfo) as TreeBench;
                workbench.CheckLocal();

                int res = WorkBenchMgr.Instance.SaveWorkBench(workbench);
                if ((res & WorkBenchMgr.SaveResultFlag_Saved) != 0)
                {
                    WorkBenchMgr.Instance.ExportWorkBench(workbench);

                    LogMgr.Instance.Log(treename + " saved and exported.");
                }
            }
            WorkBenchMgr.Instance.ActiveWorkBench = oldActiveBench;
        }
    }
}
