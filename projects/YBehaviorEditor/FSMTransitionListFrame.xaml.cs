using System;
using System.Collections.Generic;
//using System.Collections.Specialized;
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
    /// FSMTransitionListFrame.xaml 的交互逻辑
    /// </summary>
    public partial class FSMTransitionListFrame : UserControl
    {
        FSMConnection Conn;

        public FSMTransitionListFrame()
        {
            InitializeComponent();
            //((INotifyCollectionChanged)this.TransContainer.Items).CollectionChanged += TransContainer_CollectionChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Conn = DataContext as FSMConnection;

            this.TransContainer.ItemsSource = Conn.Trans;
            this.DataFrame.DataContext = null;

            AutoSelect();
        }

        private void DeleteTrans_Click(object sender, RoutedEventArgs e)
        {
            FSMConnection conn = DataContext as FSMConnection;
            if (this.TransContainer.SelectedItem != null)
            {
                TransitionResult trans = this.TransContainer.SelectedItem as TransitionResult;

                if (WorkBenchMgr.Instance.ActiveWorkBench is FSMBench)
                {
                    (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).Disconnect(conn.Ctr, trans);
                }
            }
        }

        private void TransContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.DataFrame.DataContext = e.AddedItems[0];
            }
            else if (e.RemovedItems.Count > 0)
            {
                foreach (var item in e.RemovedItems)
                {
                    if (item == this.DataFrame.DataContext)
                    {
                        this.DataFrame.DataContext = null;

                        AutoSelect();
                        break;
                    }
                }
            }
        }

        private void AutoSelect()
        {
            if (Conn != null && Conn.Trans.Count > 0)
            {
                int selectIdx = Math.Min(this.TransContainer.SelectedIndex, Conn.Trans.Count);
                selectIdx = Math.Max(0, selectIdx);
                Dispatcher.BeginInvoke((Action)(() => this.TransContainer.SelectedIndex = selectIdx));
            }
        }
        //private void TransContainer_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    if (e.Action == NotifyCollectionChangedAction.Reset)
        //        this.DataFrame.DataContext = null;
        //    else if (e.Action == NotifyCollectionChangedAction.Remove)
        //    {
        //    }
        //}
    }
}
