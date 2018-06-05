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
using System.Windows.Input;
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

        private void _KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
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
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
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
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
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
                    WorkBenchMgr.Instance.PasteCopiedToBench();
                    break;
                case Key.Z:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        WorkBenchMgr.Instance.Undo();
                    }
                    break;
                case Key.Y:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
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
                    Core.SelectionMgr.Instance.TryToggleDisable();
                    break;
            }
        }
    }
}
