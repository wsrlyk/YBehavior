﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace YBehavior.Editor
{
    abstract public class YUserControl : UserControl
    {
        public static readonly DependencyProperty AncestorProperty =
            DependencyProperty.Register("Ancestor",
            typeof(FrameworkElement), typeof(YUserControl), new FrameworkPropertyMetadata(Ancestor_PropertyChanged));

        private static void Ancestor_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            YUserControl c = (YUserControl)d;
            c._OnAncestorPropertyChanged();
        }

        public FrameworkElement Ancestor
        {
            get
            {
                return (FrameworkElement)GetValue(AncestorProperty);
            }
            set
            {
                SetValue(AncestorProperty, value);
            }
        }

        public YUserControl()
        {
            this.SetBinding(AncestorProperty, new Binding()
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorLevel = 1,
                    AncestorType = typeof(ItemsControl)
                }
            });
        }

        abstract protected void _OnAncestorPropertyChanged();
    }
}
