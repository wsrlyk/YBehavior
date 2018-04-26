using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class Comment: System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public string UITitle { get { return Name; } }
        public Geometry Geo { get; } = new Geometry();

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void SendProperty(string name)
        {
            OnPropertyChanged(name);
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
