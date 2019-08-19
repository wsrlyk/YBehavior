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

        public static void ProcessKeyDown(Key key, ModifierKeys modifier)
        {
            switch (key)
            {
                case Key.Delete:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    if ((modifier & ModifierKeys.Shift) != ModifierKeys.None)
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
                case Key.D:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    if ((modifier & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        if ((modifier & ModifierKeys.Shift) != ModifierKeys.None)
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
                case Key.C:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    if ((modifier & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        if ((modifier & ModifierKeys.Shift) != ModifierKeys.None)
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
                case Key.V:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    if ((modifier & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        WorkBenchMgr.Instance.PasteCopiedToBench();
                    }
                    break;
                case Key.Z:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    if ((modifier & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        WorkBenchMgr.Instance.Undo();
                    }
                    break;
                case Key.Y:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    if ((modifier & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        WorkBenchMgr.Instance.Redo();
                    }
                    break;
                case Key.S:
                    if ((modifier & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        WorkBenchMgr.Instance.TrySaveAndExport();
                    }
                    break;
                case Key.F9:
                    SelectionMgr.Instance.TryToggleBreakPoint();
                    break;
                case Key.F8:
                    SelectionMgr.Instance.TryToggleLogPoint();
                    break;
                case Key.F12:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    SelectionMgr.Instance.TryToggleDisable();
                    break;
                case Key.F6:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    SelectionMgr.Instance.TryToggleCondition();
                    break;
                case Key.F7:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    SelectionMgr.Instance.TryToggleFold();
                    break;
                case Key.F1:
                    {
                        MakeCenterArg oArg = new MakeCenterArg();
                        EventMgr.Instance.Send(oArg);
                    }
                    break;
                case Key.F2:
                    ///> Test....
                    {
                        Console.Clear();
                    }
                    break;
                case Key.F5:
                    {
                        if (DebugMgr.Instance.IsDebugging())
                            DebugMgr.Instance.Continue();
                    }
                    break;
                case Key.F10:
                    {
                        if (DebugMgr.Instance.IsDebugging())
                            DebugMgr.Instance.StepOver();
                    }
                    break;
                case Key.F11:
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
            ProcessKeyDown(key, Keyboard.Modifiers);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show("Really want to exit?", "Exit Or Not", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
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
            }
            else
            {
                this.Title = "YBehaviorEditor";
            }
        }
    }
}
