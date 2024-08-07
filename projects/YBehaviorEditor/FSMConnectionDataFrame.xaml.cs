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
    /// UI for editing fsm connection
    /// </summary>
    public partial class FSMConnectionDataFrame : UserControl
    {
        FSMConnection m_CurrentConnection;
        Transition m_SelectedTrans;

        public FSMConnectionDataFrame()
        {
            InitializeComponent();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FSMConnectionRenderer Renderer = DataContext as FSMConnectionRenderer;
            if (Renderer != null)
            {
                m_CurrentConnection = Renderer.FSMOwner;
                this.TransitionList.DataContext = m_CurrentConnection;

                this.TransContainer.ItemsSource = m_CurrentConnection.Trans;
                _SetSelectedTransition(null);

                AutoSelectTransition();
            }
            else
            {
                m_CurrentConnection = null;
                this.TransitionList.DataContext = null;
                this.TransContainer.ItemsSource = null;
                this.CondsContainer.ItemsSource = null;
            }
        }

        void _SetSelectedTransition(Transition res)
        {
            m_SelectedTrans = res;
            this.SelectedTrans.DataContext = res;
            if (res != null)
                this.CondsContainer.ItemsSource = res.Value;
            else
                this.CondsContainer.ItemsSource = null;
        }

        private void DeleteTrans_Click(object sender, RoutedEventArgs e)
        {
            FSMConnectionRenderer conn = DataContext as FSMConnectionRenderer;
            if (this.TransContainer.SelectedItem != null)
            {
                Transition trans = this.TransContainer.SelectedItem as Transition;
                if (trans.Type == TransitionType.Default)
                {
                    ShowSystemTipsArg arg = new ShowSystemTipsArg()
                    {
                        Content = "Default State Transition Cant Delete.",
                        TipType = ShowSystemTipsArg.TipsType.TT_Error,
                    };
                    EventMgr.Instance.Send(arg);
                }
                else if (WorkBenchMgr.Instance.ActiveWorkBench is FSMBench)
                {
                    (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).Disconnect(conn.Owner.Ctr, trans);
                }
            }
        }

        private void TransContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                _SetSelectedTransition((Transition)e.AddedItems[0]);
            }
            else if (e.RemovedItems.Count > 0)
            {
                foreach (var item in e.RemovedItems)
                {
                    if (item == this.CondsContainer.DataContext)
                    {
                        _SetSelectedTransition(null);

                        AutoSelectTransition();
                        break;
                    }
                }
            }
        }

        private void AutoSelectTransition()
        {
            if (m_CurrentConnection != null && m_CurrentConnection.Trans.Count > 0)
            {
                int selectIdx = Math.Min(this.TransContainer.SelectedIndex, m_CurrentConnection.Trans.Count);
                selectIdx = Math.Max(0, selectIdx);
                Dispatcher.BeginInvoke((Action)(() => this.TransContainer.SelectedIndex = selectIdx));
            }
        }

        private void AddCond_Click(object sender, RoutedEventArgs e)
        {
            if (m_SelectedTrans != null)
            {
                if (m_SelectedTrans.Type == TransitionType.Default)
                {
                    ShowSystemTipsArg arg = new ShowSystemTipsArg()
                    {
                        Content = "Default State Transition Cant Add Condition.",
                        TipType = ShowSystemTipsArg.TipsType.TT_Error,
                    };
                    EventMgr.Instance.Send(arg);
                }
                else
                {
                    var cond = new TransitionMapValue(string.Empty);
                    m_SelectedTrans.Value.Add(cond);
                    WorkBenchMgr.Instance.PushCommand(new AddCondCommand()
                    {
                        Cond = cond,
                        Trans = m_SelectedTrans,
                    });
                }
            }
        }

        private void DeleteCond_Click(object sender, RoutedEventArgs e)
        {
            if (m_SelectedTrans != null && this.CondsContainer.SelectedItem != null)
            {
                var cond = m_SelectedTrans.Value[this.CondsContainer.SelectedIndex];
                m_SelectedTrans.Value.RemoveAt(this.CondsContainer.SelectedIndex);
                WorkBenchMgr.Instance.PushCommand(new RemoveCondCommand()
                {
                    Cond = cond,
                    Trans = m_SelectedTrans,
                });
            }
        }
    }
}
