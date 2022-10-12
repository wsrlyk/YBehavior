using System;
using System.Collections;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls;
using System.ComponentModel;

namespace YBehavior.Editor
{
    /// <summary>
    /// https://stackoverflow.com/questions/2001842/dynamic-filter-of-wpf-combobox-based-on-text-input
    /// </summary>
    public class FilteredComboBox : ComboBox
    {
        private string oldFilter = string.Empty;

        private string currentFilter = string.Empty;

        protected TextBox EditableTextBox => GetTemplateChild("PART_EditableTextBox") as TextBox;

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (oldValue != null)
            {
                var view = oldValue as ICollectionView;
                if (view != null) view.Filter -= FilterItem;
            }

            if (newValue != null)
            {
                var view = newValue as ICollectionView;
                if (view != null) view.Filter += FilterItem;
            }

            base.OnItemsSourceChanged(oldValue, newValue);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Enter:
                    IsDropDownOpen = false;
                    break;
                case Key.Escape:
                    IsDropDownOpen = false;
                    SelectedIndex = -1;
                    Text = currentFilter;
                    break;
                default:
                    if (e.Key == Key.Down) IsDropDownOpen = true;

                    base.OnPreviewKeyDown(e);
                    break;
            }

            // Cache text
            oldFilter = Text;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    break;
                case Key.Tab:
                case Key.Enter:

                    ClearFilter();
                    break;
                default:
                    if (Text != oldFilter)
                    {
                        var temp = Text;
                        currentFilter = temp;
                        RefreshFilter(); //RefreshFilter will change Text property
                        Text = temp;

                        if (SelectedIndex != -1 && Text != Items[SelectedIndex].ToString())
                        {
                            SelectedIndex = -1; //Clear selection. This line will also clear Text property
                            Text = temp;
                        }


                        IsDropDownOpen = true;

                        EditableTextBox.SelectionStart = int.MaxValue;
                    }

                    //automatically select the item when the input text matches it
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (Text == Items[i].ToString())
                            SelectedIndex = i;
                    }

                    base.OnKeyUp(e);
                    currentFilter = Text;
                    break;
            }
        }

        protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ClearFilter();
            var temp = SelectedIndex;
            SelectedIndex = -1;
            Text = string.Empty;
            SelectedIndex = temp;
            base.OnPreviewLostKeyboardFocus(e);
        }

        private void RefreshFilter()
        {
            var view = ItemsSource as ICollectionView;
            if (view == null) return;

            view.Refresh();
        }

        private void ClearFilter()
        {
            currentFilter = string.Empty;
            RefreshFilter();
        }

        private bool FilterItem(object value)
        {
            if (value == null) return false;
            if (currentFilter.Length == 0) return true;

            return value.ToString().ToLower().Contains(currentFilter.ToLower());
        }
    }
}
