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
    /// ToolBarFrame.xaml 的交互逻辑
    /// </summary>
    public partial class ToolBarFrame : UserControl
    {
        public ToolBarFrame()
        {
            InitializeComponent();
            //EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
        }

        //private void _OnWorkBenchSelected(EventArg arg)
        //{
        //    WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;
        //    if (oArg.Bench != null)
        //    {
        //        this.btnUndo.DataContext = oArg.Bench.CommandMgr;
        //        this.btnRedo.DataContext = oArg.Bench.CommandMgr;

        //        Visibility treeVisibility = oArg.Bench is TreeBench ? Visibility.Visible : Visibility.Collapsed;
        //        Visibility fsmVisibility = oArg.Bench is FSMBench ? Visibility.Visible : Visibility.Collapsed;

        //        btnFold.Visibility = treeVisibility;
        //        btnCondition.Visibility = treeVisibility;
        //        btnMakeDefault.Visibility = fsmVisibility;
        //    }
        //}

        //private void btnUndo_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Undo);
        //}

        //private void btnRedo_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Redo);
        //}

        //private void btnPaste_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Paste);
        //}

        //private void btnCopy_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Copy, Keyboard.Modifiers);
        //}

        //private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Duplicate, Keyboard.Modifiers);
        //}

        //private void btnDelete_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Delete, Keyboard.Modifiers);
        //}

        //private void btnLog_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.LogPoint);
        //}

        //private void btnDebug_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.BreakPoint);
        //}

        //private void btnDisable_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Disable);
        //}

        //private void btnCondition_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Condition);
        //}

        //private void btnCenter_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Center);
        //}

        //private void btnFold_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Fold);
        //}

        //private void btnClear_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Clear);
        //}

        //private void btnMakeDefault_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Default);
        //}

        //private void btnSearch_Click(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.ProcessCommand(Command.Search);
        //}

        //private void btnFile_Click(object sender, RoutedEventArgs e)
        //{
        //    EventMgr.Instance.Send(new ShowWorkSpaceArg() { });
        //}
        //private void btnSaveFile_Click(object sender, RoutedEventArgs e)
        //{
        //    WorkBenchMgr.Instance.TrySaveAndExport();
        //}
    }
}
