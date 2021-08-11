using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace YBehavior.Editor
{
    public class YUserControl : UserControl, Core.New.IGetCanvas
    {
        public static readonly DependencyProperty CanvasProperty =
            DependencyProperty.Register("Canvas",
            typeof(FrameworkElement), typeof(YUserControl), new FrameworkPropertyMetadata(Ancestor_PropertyChanged));

        private static void Ancestor_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            YUserControl c = (YUserControl)d;
            c._OnAncestorPropertyChanged();
        }

        public FrameworkElement Canvas
        {
            get
            {
                return (FrameworkElement)GetValue(CanvasProperty);
            }
            set
            {
                SetValue(CanvasProperty, value);
            }
        }

        public YUserControl()
        {
            this.SetBinding(CanvasProperty, new Binding()
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorLevel = 1,
                    AncestorType = typeof(ItemsControl)
                }
            });
        }

        public YUserControl(bool bFindAncestor)
        {
            if (bFindAncestor)
            {
                this.SetBinding(CanvasProperty, new Binding()
                {
                    RelativeSource = new RelativeSource()
                    {
                        Mode = RelativeSourceMode.FindAncestor,
                        AncestorLevel = 1,
                        AncestorType = typeof(ItemsControl)
                    }
                });
            }
        }

        protected void _OnAncestorPropertyChanged() { }
    }
}
