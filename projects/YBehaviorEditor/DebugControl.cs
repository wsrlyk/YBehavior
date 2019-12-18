using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    public abstract class DebugControl<T> : YUserControl where T : DebugControl<T>
    {
        public abstract FrameworkElement DebugUI { get; }
        public abstract Brush DebugBrush { get; set; }

        public abstract Storyboard InstantAnim { get; }
        public abstract NodeState RunState { get; }

        public DebugControl()
        { }
        public DebugControl(bool bFindAncestor)
            : base (bFindAncestor)
        {

        }
        public void SetDebugInstant(NodeState state = NodeState.NS_INVALID)
        {
            this.DebugUI.Visibility = Visibility.Collapsed;
            if (state == NodeState.NS_INVALID)
            {
                InstantAnim.Remove(DebugUI);
            }
            else
            {
                Brush bgBrush;
                switch (state)
                {
                    case NodeState.NS_SUCCESS:
                        bgBrush = new SolidColorBrush(Colors.LightGreen);
                        break;
                    case NodeState.NS_FAILURE:
                        bgBrush = new SolidColorBrush(Colors.LightBlue);
                        break;
                    case NodeState.NS_RUNNING:
                        bgBrush = new SolidColorBrush(Colors.LightPink);
                        break;
                    case NodeState.NS_BREAK:
                        bgBrush = new SolidColorBrush(Colors.DarkRed);
                        break;
                    default:
                        bgBrush = new SolidColorBrush(Colors.Red);
                        break;
                }
                this.DebugBrush = bgBrush;

                InstantAnim.Begin(this.DebugUI, true);
            }
        }

        public void SetDebug(NodeState state = NodeState.NS_INVALID)
        {
            InstantAnim.Remove(DebugUI);
            if (state == NodeState.NS_INVALID)
            {
                this.DebugUI.Visibility = Visibility.Collapsed;
            }
            else
            {
                Brush bgBrush;
                switch (state)
                {
                    case NodeState.NS_SUCCESS:
                        bgBrush = new SolidColorBrush(Colors.LightGreen);
                        break;
                    case NodeState.NS_FAILURE:
                        bgBrush = new SolidColorBrush(Colors.LightBlue);
                        break;
                    case NodeState.NS_RUNNING:
                        bgBrush = new SolidColorBrush(Colors.LightPink);
                        break;
                    case NodeState.NS_BREAK:
                        bgBrush = new SolidColorBrush(Colors.DarkRed);
                        break;
                    default:
                        bgBrush = new SolidColorBrush(Colors.Red);
                        break;
                }
                this.DebugBrush = bgBrush;

                this.DebugUI.Visibility = Visibility.Visible;
            }
        }

        public static readonly DependencyProperty DebugTriggerProperty =
    DependencyProperty.Register("DebugTrigger",
    typeof(bool), typeof(T), new FrameworkPropertyMetadata(DebugTrigger_PropertyChanged));
        private static void DebugTrigger_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            T c = (T)d;
            if (DebugMgr.Instance.bBreaked)
                c.SetDebug(c.RunState);
            else
                c.SetDebugInstant(c.RunState);
        }
        public bool DebugTrigger
        {
            get { return (bool)GetValue(DebugTriggerProperty); }
            set { SetValue(DebugTriggerProperty, value); }
        }

    }
}
