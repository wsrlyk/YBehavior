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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UITabItem.xaml 的交互逻辑
    /// </summary>
    public partial class UITabItem : TabItem, IDebugControl
    {
        Border m_DebugUI;
        public UITabItem()
        {
            InitializeComponent();
            m_InstantAnim = Application.Current.Resources["InstantShowAnim"] as Storyboard;
            m_DebugControl = new DebugControl(this);

            this.DataContextChanged += _DataContextChangedEventHandler;
            this.Loaded += UITabItem_Loaded;
        }

        private void UITabItem_Loaded(object sender, RoutedEventArgs e)
        {
            m_DebugUI = (Border)this.Template.FindName("debugUI", this);
        }

        private TabControl _FindParentTabControl(DependencyObject reference)
        {
            DependencyObject dObj = VisualTreeHelper.GetParent(reference);
            if (dObj == null)
                return null;
            if (dObj.GetType() == typeof(TabControl))
                return dObj as TabControl;
            else
                return _FindParentTabControl(dObj);
        }

        public FrameworkElement DebugUI => m_DebugUI;

        public Brush DebugBrush
        {
            get { return m_DebugUI.Background; }
            set
            {
                m_DebugUI.Background = value;
            }
        }

        Storyboard m_InstantAnim;
        public Storyboard InstantAnim { get { return m_InstantAnim; } }

        public NodeState RunState => m_WorkBench.RunState;

        WorkBench m_WorkBench;
        DebugControl m_DebugControl;

        public delegate bool CloseCallback(UITabItem tab);
        public event CloseCallback CloseHandler;

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            m_WorkBench = this.DataContext as WorkBench;

            if (m_DebugControl != null)
                m_WorkBench.DebugEvent += m_DebugControl.Renderer_DebugEvent;
            //SetCanvas((renderer.ChildConn.Owner as Node).Renderer.RenderCanvas);
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
            CloseHandler?.Invoke(this);

            var parent = _FindParentTabControl(this);
            if (parent != null)
                parent.Items.Remove(this);
        }
    }
}
