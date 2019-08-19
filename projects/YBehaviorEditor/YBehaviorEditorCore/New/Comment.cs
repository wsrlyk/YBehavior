using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class Comment: System.ComponentModel.INotifyPropertyChanged
    {
        //string m_Name;
        //public string Name
        //{
        //    get { return m_Name; }
        //    set
        //    {
        //        m_Name = value;
        //        OnPropertyChanged("Name");
        //        OnPropertyChanged("UITitle");
        //    }
        //}
        string m_Content;
        public string Content
        {
            get { return m_Content; }
            set
            {
                ChangeCommentCommand command = new ChangeCommentCommand()
                {
                    Comment = this,
                    OriginContent = m_Content,
                    FinalContent = value,
                };

                m_Content = value;
                OnPropertyChanged("Content");

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }
        //public string UITitle { get { return Name; } }
        public Geometry Geo { get; } = new Geometry();

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void OnGeometryChanged()
        {
            OnPropertyChanged("Geo");

            if (m_DefaultGeo == m_PreservedGeo)
            {
                m_PreservedGeo = Geo.Rec;
            }
        }

        public void OnGeometryChangedWithoutCommand()
        {
            OnPropertyChanged("Geo");
        }

        System.Windows.Rect m_PreservedGeo = new System.Windows.Rect(0, 0, 0, 0);
        System.Windows.Rect m_DefaultGeo = new System.Windows.Rect(0, 0, 0, 0);

        public void OnFinishGeometryChanged()
        {
            MoveCommentCommand command = new MoveCommentCommand()
            {
                Comment = this,
                OriginRec = m_PreservedGeo,
                FinalRec = Geo.Rec,
            };
            WorkBenchMgr.Instance.PushCommand(command);
            m_PreservedGeo = m_DefaultGeo;
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
