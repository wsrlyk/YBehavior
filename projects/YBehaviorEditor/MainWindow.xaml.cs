using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);

            UnityCoroutines.CoroutineManager.Instance.Run();
        }

        static void _ProcessKeyDown(Key key, ModifierKeys modifier)
        {
            var cmd = Config.Instance.KeyBindings.GetCommand(key, modifier);
            ProcessCommand(cmd, modifier);
        }

        public static void ProcessCommand(Command cmd, ModifierKeys modifier = ModifierKeys.None)
        {
            switch (cmd)
            {
                case Command.Delete:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    if (Config.Instance.KeyBindings.IsMulti(modifier))
                    {
                        ///> Duplicate all children
                        SelectionMgr.Instance.TryDeleteSelection(1);
                    }
                    else
                    {
                        ///> Duplicate only one
                        SelectionMgr.Instance.TryDeleteSelection(0);
                    }
                    break;
                case Command.Duplicate:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    {
                        if (Config.Instance.KeyBindings.IsMulti(modifier))
                        {
                            ///> Duplicate all children
                            SelectionMgr.Instance.TryDuplicateSelection(1);
                        }
                        else
                        {
                            ///> Duplicate only one
                            SelectionMgr.Instance.TryDuplicateSelection(0);
                        }
                    }
                    break;
                case Command.Copy:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    {
                        if (Config.Instance.KeyBindings.IsMulti(modifier))
                        {
                            ///> Duplicate all children
                            SelectionMgr.Instance.TryCopySelection(1);
                        }
                        else
                        {
                            ///> Duplicate only one
                            SelectionMgr.Instance.TryCopySelection(0);
                        }
                    }
                    break;
                case Command.Paste:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    {
                        WorkBenchMgr.Instance.PasteCopiedToBench();
                    }
                    break;
                case Command.Undo:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    {
                        WorkBenchMgr.Instance.Undo();
                    }
                    break;
                case Command.Redo:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    {
                        WorkBenchMgr.Instance.Redo();
                    }
                    break;
                case Command.Save:
                    {
                        WorkBenchMgr.Instance.TrySaveAndExport();
                    }
                    break;
                case Command.Open:
                    EventMgr.Instance.Send(new ShowWorkSpaceArg() { });
                    break;
                case Command.Search:
                    {
                        SearchFrame searchFrame = (App.Current.MainWindow as MainWindow).SearchFrame;
                        searchFrame.Visibility = searchFrame.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    }
                    break;
                case Command.BreakPoint:
                    SelectionMgr.Instance.TryToggleBreakPoint();
                    break;
                case Command.LogPoint:
                    SelectionMgr.Instance.TryToggleLogPoint();
                    break;
                case Command.Disable:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    SelectionMgr.Instance.TryToggleDisable();
                    break;
                case Command.Condition:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    SelectionMgr.Instance.TryToggleCondition();
                    break;
                case Command.Fold:
                    //if (NetworkMgr.Instance.IsConnected)
                    //    break;
                    SelectionMgr.Instance.TryToggleFold();
                    break;
                case Command.Default:
                    if (NetworkMgr.Instance.IsConnected)
                        break;
                    SelectionMgr.Instance.TryMakeDefault();
                    break;
                case Command.Center:
                    {
                        MakeCenterArg oArg = new MakeCenterArg();
                        EventMgr.Instance.Send(oArg);
                    }
                    break;
                case Command.Clear:
                    ///> Test....
                    {
                        Console.Clear();
                    }
                    break;
                case Command.DebugContinue:
                    {
                        if (DebugMgr.Instance.IsDebugging())
                            DebugMgr.Instance.Continue();
                    }
                    break;
                case Command.DebugStepOver:
                    {
                        if (DebugMgr.Instance.IsDebugging())
                            DebugMgr.Instance.StepOver();
                    }
                    break;
                case Command.DebugStepIn:
                    {
                        if (DebugMgr.Instance.IsDebugging())
                            DebugMgr.Instance.StepInto();
                    }
                    break;
            }
        }
        private void _KeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key;
            if (e.Key == Key.System && e.SystemKey == Key.F10)
            {
                e.Handled = true;
                key = Key.F10;
            }
            _ProcessKeyDown(key, Keyboard.Modifiers);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show("Really want to exit?", "Exit Or Not", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Core.New.Config.Instance.Save();
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;

            if (oArg.Bench != null)
            {
                //if (DebugMgr.Instance.IsDebugging(oArg.Bench.FileInfo.Name) && DebugMgr.Instance.bBreaked)
                //{
                //    _RefreshMainTreeDebug(false, NetworkMgr.Instance.MessageProcessor.TickResultToken);
                //}
                this.Title = oArg.Bench.DisplayName + " - YBehaviorEditor";

                Visibility treeVisibility = oArg.Bench is TreeBench ? Visibility.Visible : Visibility.Collapsed;
                this.TreeRightPanel.Visibility = treeVisibility;
                //this.NodeListPanel.Visibility = treeVisibility;
                this.FSMRightPanel.Visibility = oArg.Bench is FSMBench ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                this.Title = "YBehaviorEditor";
            }

        }
    }
}
