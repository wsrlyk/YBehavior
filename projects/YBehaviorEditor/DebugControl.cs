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
    public interface IDebugControl
    {
        FrameworkElement DebugUI { get; }
        Brush DebugBrush { get; set; }

        Storyboard InstantAnim { get; }
        NodeState RunState { get; }
    }
    public class DebugControl
    {
        IDebugControl m_Target;
        public DebugControl(IDebugControl target)
        {
            m_Target = target;
        }

        public void SetDebugInstant(NodeState state = NodeState.NS_INVALID)
        {
            if (m_Target.DebugUI == null)
                return;
            m_Target.DebugUI.Visibility = Visibility.Collapsed;
            if (state == NodeState.NS_INVALID)
            {
                m_Target.InstantAnim.Remove(m_Target.DebugUI);
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
                m_Target.DebugBrush = bgBrush;

                m_Target.InstantAnim.Begin(m_Target.DebugUI, true);
            }
        }

        public void SetDebug(NodeState state = NodeState.NS_INVALID)
        {
            if (m_Target.DebugUI == null)
                return;
            m_Target.InstantAnim.Remove(m_Target.DebugUI);
            if (state == NodeState.NS_INVALID)
            {
                m_Target.DebugUI.Visibility = Visibility.Collapsed;
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
                m_Target.DebugBrush = bgBrush;

                m_Target.DebugUI.Visibility = Visibility.Visible;
            }
        }

        public void Renderer_DebugEvent()
        {
            if (DebugMgr.Instance.bBreaked)
                SetDebug(m_Target.RunState);
            else
                SetDebugInstant(m_Target.RunState);
        }
    }
}
