using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class Comment: System.ComponentModel.INotifyPropertyChanged
    {
        string m_Name;
        public string Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                OnPropertyChanged("Name");
                OnPropertyChanged("UITitle");
            }
        }
        string m_Data;
        public string Data
        {
            get { return m_Data; }
            set
            {
                m_Data = value;
                OnPropertyChanged("Data");
            }
        }
        public string UITitle { get { return Name; } }
        public Geometry Geo { get; } = new Geometry();

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void OnGeometryChanged()
        {
            OnPropertyChanged("Geo");
        }
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
