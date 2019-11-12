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
    /// FSMStateFrame.xaml 的交互逻辑
    /// </summary>
    public partial class FSMStateFrame : UserControl
    {
        public FSMStateFrame()
        {
            InitializeComponent();
        }

        FSMStateRenderer Renderer;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Renderer = DataContext as FSMStateRenderer;

            if (Renderer != null)
            {
                this.NamePanel.Visibility = (Renderer.FSMStateOwner.Type == FSMStateType.User) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
