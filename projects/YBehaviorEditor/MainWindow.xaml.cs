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
using YBehavior.Editor.Core;

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
                        Core.SelectionMgr.Instance.TryDeleteSelection(1);
                    }
                    else
                    {
                        ///> Duplicate only one
                        Core.SelectionMgr.Instance.TryDeleteSelection(0);
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
                            Core.SelectionMgr.Instance.TryDuplicateSelection(1);
                        }
                        else
                        {
                            ///> Duplicate only one
                            Core.SelectionMgr.Instance.TryDuplicateSelection(0);
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
                            Core.SelectionMgr.Instance.TryCopySelection(1);
                        }
                        else
                        {
                            ///> Duplicate only one
                            Core.SelectionMgr.Instance.TryCopySelection(0);
                        }
                    }
                    break;
                case Key.V:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    WorkBenchMgr.Instance.PasteCopiedToBench();
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
                case Key.F9:
                    Core.SelectionMgr.Instance.TryToggleBreakPoint();
                    break;
                case Key.F8:
                    Core.SelectionMgr.Instance.TryToggleLogPoint();
                    break;
                case Key.F12:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    Core.SelectionMgr.Instance.TryToggleDisable();
                    break;
                case Key.F11:
                    if (DebugMgr.Instance.IsDebugging())
                        break;
                    Core.SelectionMgr.Instance.TryToggleCondition();
                    break;
                case Key.T:
                    ///> Test....
                    WorkBenchMgr.Instance.OpenAllRelated();
                    break;
            }
        }
        private void _KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyDown(e.Key, Keyboard.Modifiers);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show("Really want to exit?", "Exit Or Not", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
