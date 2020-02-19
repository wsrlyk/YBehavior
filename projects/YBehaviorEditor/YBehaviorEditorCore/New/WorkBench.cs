using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public abstract class WorkBench : System.ComponentModel.INotifyPropertyChanged
    {
        protected Graph m_Graph;
        public Graph MainGraph { get { return m_Graph; } }
        public CommandMgr CommandMgr { get; } = new CommandMgr();

        public DelayableNotificationCollection<Comment> Comments { get; } = new DelayableNotificationCollection<Comment>();
        public CoroutineCollection<DelayableNotificationCollection<NodeBaseRenderer>, NodeBaseRenderer> NodeList { get; } = new CoroutineCollection<DelayableNotificationCollection<NodeBaseRenderer>, NodeBaseRenderer>();
        public CoroutineCollection<DelayableNotificationCollection<ConnectionRenderer>, ConnectionRenderer> ConnectionList { get; } = new CoroutineCollection<DelayableNotificationCollection<ConnectionRenderer>, ConnectionRenderer>();

        public string FilePath { get; set; }

        public string DisplayName { get { return FileInfo.DisplayName + (CommandMgr.Dirty ? " *" : ""); } }

        private FileMgr.FileInfo m_UntitledFileInfo = null;
        public FileMgr.FileInfo FileInfo
        {
            get
            {
                return string.IsNullOrEmpty(FilePath) ? m_UntitledFileInfo : FileMgr.Instance.GetFileInfo(FilePath);
            }
            set
            {
                m_UntitledFileInfo = value;
            }
        }

        public WorkBench()
        {
            //NodeList.Step = 1;
            //ConnectionList.Step = 20;
        }

        public virtual bool Load(XmlElement data) { return true; }
        public virtual bool CheckError() { return true; }
        public virtual void Save(XmlElement data, XmlDocument xmlDoc) { }
        public virtual void Export(XmlElement data, XmlDocument xmlDoc) { }
        public virtual void OnNodeMoved(EventArg arg) { }
        public virtual void ConnectNodes(Connector ctr0, Connector ctr1) { }
        public virtual void DisconnectNodes(Connection.FromTo connection) { }
        public virtual void RemoveNode(NodeBase node) { }
        public virtual void AddNode(NodeBase node) { }
        public virtual void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot = false) { }
        public virtual void RemoveRenderers(NodeBase node, bool excludeRoot = false) { }
        public virtual void InitEmpty() { }
        public virtual void SaveSuo() { }
        public void PushDoneCommand(ICommand command)
        {
            bool bOldDirty = CommandMgr.Dirty;
            CommandMgr.PushDoneCommand(command);

            if (!bOldDirty)
                OnPropertyChanged("DisplayName");
        }

        public void RemoveComment(Comment comment)
        {
            if (comment != null)
                Comments.Remove(comment);

            RemoveCommentCommand removeCommentCommand = new RemoveCommentCommand()
            {
                Comment = comment
            };
            PushDoneCommand(removeCommentCommand);
        }

        public void AddComment(Comment comment)
        {
            if (comment != null)
                Comments.Add(comment);

            AddCommentCommand addCommentCommand = new AddCommentCommand()
            {
                Comment = comment
            };
            PushDoneCommand(addCommentCommand);
        }

        public void CreateComment()
        {
            Comment comment = new Comment();
            AddComment(comment);

            CommentCreatedArg cArg = new CommentCreatedArg()
            {
                Comment = comment
            };
            EventMgr.Instance.Send(cArg);
        }

        protected bool _LoadComments(XmlNode root)
        {
            Comments.Clear();
            foreach (XmlNode chi in root.ChildNodes)
            {
                if (chi.Name == "Comment")
                {
                    Comment comment = new Comment();

                    var attr = chi.Attributes["Content"];
                    if (attr != null)
                        comment.Content = attr.Value;
                    attr = chi.Attributes["Rect"];
                    if (attr != null)
                        comment.Geo.Rec = System.Windows.Rect.Parse(attr.Value);

                    Comments.Add(comment);
                }
            }
            return true;
        }

        protected void _SaveComments(XmlElement parent, XmlDocument xmlDoc)
        {
            if (Comments.Count > 0)
            {
                XmlElement root = xmlDoc.CreateElement("Comments");
                parent.AppendChild(root);

                foreach (Comment comment in Comments)
                {
                    XmlElement comEl = xmlDoc.CreateElement("Comment");
                    //comEl.SetAttribute("Title", comment.Name);
                    comEl.SetAttribute("Content", comment.Content);
                    comEl.SetAttribute("Rect", comment.Geo.Rec.ToString());
                    root.AppendChild(comEl);
                }
            }
        }

        protected uint m_ExportFileHash = 0;
        public uint ExportFileHash
        {
            get
            {
                if (m_ExportFileHash == 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(FileInfo.ExportingPath);
                    return GenerateHash(xmlDoc.DocumentElement.OuterXml.Replace(" ", string.Empty));
                }
                return m_ExportFileHash;
            }
        }
        public uint GenerateHash(string str)
        {
            int len = str.Length;
            uint hash = 0;
            for (int i = 0; i < len; ++i)
            {
                hash = (hash << 5) + hash + (uint)str[i];
            }

            m_ExportFileHash = hash;

            return m_ExportFileHash;
        }

        public void RefreshDebug()
        {
            foreach (var node in NodeList.Collection)
            {
                node.RefreshDebug();
            }
            foreach (var conn in ConnectionList.Collection)
            {
                conn.RefreshDebug();
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
