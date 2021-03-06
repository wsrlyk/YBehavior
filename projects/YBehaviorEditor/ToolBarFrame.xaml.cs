﻿using System;
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
    /// ToolBarFrame.xaml 的交互逻辑
    /// </summary>
    public partial class ToolBarFrame : UserControl
    {
        public ToolBarFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;
            if (oArg.Bench != null)
            {
                this.btnUndo.DataContext = oArg.Bench.CommandMgr;
                this.btnRedo.DataContext = oArg.Bench.CommandMgr;

                Visibility treeVisibility = oArg.Bench is TreeBench ? Visibility.Visible : Visibility.Collapsed;
                Visibility fsmVisibility = oArg.Bench is FSMBench ? Visibility.Visible : Visibility.Collapsed;

                btnFold.Visibility = treeVisibility;
                btnCondition.Visibility = treeVisibility;
                btnMakeDefault.Visibility = fsmVisibility;
            }
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.Z, ModifierKeys.Control);
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.Y, ModifierKeys.Control);
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.V, ModifierKeys.Control);
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.C, ModifierKeys.Control | (Keyboard.Modifiers & ModifierKeys.Shift));
        }

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.D, ModifierKeys.Control | (Keyboard.Modifiers & ModifierKeys.Shift));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.Delete,  (Keyboard.Modifiers & ModifierKeys.Shift));
        }

        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F8, ModifierKeys.None);
        }

        private void btnDebug_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F9, ModifierKeys.None);
        }

        private void btnDisable_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F12, ModifierKeys.None);
        }

        private void btnCondition_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F6, ModifierKeys.None);
        }

        private void btnCenter_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F1, ModifierKeys.None);
        }

        private void btnFold_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F7, ModifierKeys.None);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F2, ModifierKeys.None);
        }

        private void btnMakeDefault_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F7, ModifierKeys.None);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F, ModifierKeys.Control);
        }
    }
}
