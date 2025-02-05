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

namespace YBehavior.Editor
{
    /// <summary>
    /// UI of a text box for filtering and searching
    /// </summary>
    public partial class FilterBox : UserControl
    {
        public TextChangedEventHandler TextChangedHandler { get; set; }
        public RoutedEventHandler TextClearedHandler { get; set; }

        public string Text { get { return this.SearchText.Text; } }

        public FilterBox()
        {
            InitializeComponent();
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextChangedHandler != null)
                TextChangedHandler(sender, e);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            this.SearchText.Text = string.Empty;
        }
        /// <summary>
        /// Set the TextBox focused
        /// </summary>
        public void SetFocus()
        {
            SearchText.Focus();
            SearchText.SelectAll();
        }
    }
}
