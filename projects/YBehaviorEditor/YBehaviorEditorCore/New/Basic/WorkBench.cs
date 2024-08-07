using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Base class for operating a tree/fsm
    /// </summary>
    public abstract class WorkBench : System.ComponentModel.INotifyPropertyChanged
    {
        protected Graph m_Graph;
        /// <summary>
        /// tree/fsm
        /// </summary>
        public Graph MainGraph { get { return m_Graph; } }
        public CommandMgr CommandMgr { get; } = new CommandMgr();

        /// <summary>
        /// Comment collection
        /// </summary>
        public DelayableNotificationCollection<Comment> Comments { get; } = new DelayableNotificationCollection<Comment>();
        /// <summary>
        /// ViewModels of nodes
        /// </summary>
        public CoroutineCollection<DelayableNotificationCollection<NodeBaseRenderer>, NodeBaseRenderer> NodeList { get; } = new CoroutineCollection<DelayableNotificationCollection<NodeBaseRenderer>, NodeBaseRenderer>();
        /// <summary>
        /// ViewModels of lines
        /// </summary>
        public CoroutineCollection<DelayableNotificationCollection<ConnectionRenderer>, ConnectionRenderer> ConnectionList { get; } = new CoroutineCollection<DelayableNotificationCollection<ConnectionRenderer>, ConnectionRenderer>();

        string filePath = string.Empty;
        /// <summary>
        /// Path of the tree/fsm
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                if (string.IsNullOrEmpty(filePath))
                {
                    m_UntitledFileInfo = new FileMgr.FileInfo() { FileType = this.FileType, Path = string.Empty };
                }
                else
                {
                    m_UntitledFileInfo = null;
                }
            }
        }
        /// <summary>
        /// Name for Tab ui
        /// </summary>
        public string DisplayName { get { return FileInfo.DisplayName + (CommandMgr.Dirty ? " *" : ""); } }
        /// <summary>
        /// Make the name shorter for better displaying
        /// </summary>
        public string ShortDisplayName
        {
            get 
            {
                string name = FileInfo.DisplayName;
                if (name.Length > 20)
                {
                    name = name.Substring(0, 10) + ".." + name.Substring(name.Length - 10);
                }
                return name + (CommandMgr.Dirty ? " *" : "");
            }
        }

        private FileMgr.FileInfo m_UntitledFileInfo = null;
        /// <summary>
        /// Reference to the FileInfo of this tree/fsm
        /// </summary>
        public FileMgr.FileInfo FileInfo
        {
            get
            {
                return string.IsNullOrEmpty(FilePath) ? m_UntitledFileInfo : FileMgr.Instance.GetFileInfo(FilePath);
            }
        }

        protected virtual FileType FileType => FileType.TREE;
        public WorkBench()
        {
            //NodeList.Step = 1;
            //ConnectionList.Step = 20;
        }
        /// <summary>
        /// Load from file
        /// </summary>
        /// <param name="data"></param>
        /// <param name="bRendering"></param>
        /// <returns></returns>
        public virtual bool Load(XmlElement data, bool bRendering) { return true; }
        /// <summary>
        /// Check error when saving
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckError() { return true; }
        /// <summary>
        /// Save to the file
        /// </summary>
        /// <param name="data"></param>
        /// <param name="xmlDoc"></param>
        public virtual void Save(XmlElement data, XmlDocument xmlDoc) { }
        /// <summary>
        /// Export the runtime version
        /// </summary>
        /// <param name="data"></param>
        /// <param name="xmlDoc"></param>
        public virtual void Export(XmlElement data, XmlDocument xmlDoc) { }
        /// <summary>
        /// Called when a node is moved
        /// </summary>
        /// <param name="arg"></param>
        public virtual void OnNodeMoved(EventArg arg) { }
        /// <summary>
        /// Connect two nodes
        /// </summary>
        /// <param name="ctr0"></param>
        /// <param name="ctr1"></param>
        public virtual void ConnectNodes(Connector ctr0, Connector ctr1) { }
        /// <summary>
        /// Disconnect two nodes
        /// </summary>
        /// <param name="connection"></param>
        public virtual void DisconnectNodes(Connection.FromTo connection) { }
        /// <summary>
        /// Remove a node
        /// </summary>
        /// <param name="node"></param>
        public virtual void RemoveNode(NodeBase node) { }
        /// <summary>
        /// Add a node
        /// </summary>
        /// <param name="node"></param>
        public virtual void AddNode(NodeBase node) { }
        /// <summary>
        /// Create a ViewModel for node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="batchAdd">When true, create viewmodels for children</param>
        /// <param name="excludeRoot">When true, exclude the root node</param>
        public virtual void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot = false) { }
        /// <summary>
        /// Remove the ViewModels for node and its children
        /// </summary>
        /// <param name="node"></param>
        /// <param name="excludeRoot">When true, exclude the root node</param>
        public virtual void RemoveRenderers(NodeBase node, bool excludeRoot = false) { }
        /// <summary>
        /// Init an empty one when newly creating a tree/fsm
        /// </summary>
        public virtual void InitEmpty() { }
        /// <summary>
        /// Save the current states to suo
        /// </summary>
        public virtual void SaveSuo() { }
        /// <summary>
        /// Push a command to Undo/Redo
        /// </summary>
        /// <param name="command"></param>
        public void PushDoneCommand(ICommand command)
        {
            bool bOldDirty = CommandMgr.Dirty;
            CommandMgr.PushDoneCommand(command);

            if (!bOldDirty)
                OnPropertyChanged("ShortDisplayName");
        }
        /// <summary>
        /// Remove a comment node
        /// </summary>
        /// <param name="comment"></param>
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
        /// <summary>
        /// Add a comment node
        /// </summary>
        /// <param name="comment"></param>
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
        /// <summary>
        /// Create a comment at a position
        /// </summary>
        /// <param name="viewPos"></param>
        public void CreateComment(System.Windows.Point viewPos)
        {
            Comment comment = new Comment();
            AddComment(comment);

            CommentCreatedArg cArg = new CommentCreatedArg()
            {
                Comment = comment,
                Pos = viewPos,
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
        /// <summary>
        /// Get the hash of runtime version
        /// </summary>
        public uint ExportFileHash
        {
            get
            {
                if (m_ExportFileHash == 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(FileInfo.ExportingPath);
                    return _GenerateHash(xmlDoc.DocumentElement.OuterXml.Replace(" ", string.Empty));
                }
                return m_ExportFileHash;
            }
        }
        protected uint _GenerateHash(string str)
        {
            m_ExportFileHash = Utility.Hash(str);

            return m_ExportFileHash;
        }
        /// <summary>
        /// Invoked when a tick result comes
        /// </summary>
        public event Action DebugEvent;
        /// <summary>
        /// Invoke the debugging events of the nodes and lines 
        /// </summary>
        public void RefreshContentDebug()
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
        /// <summary>
        /// Invoke the debugging events of this tree/fsm 
        /// </summary>
        public void RefreshBenchDebug()
        {
            DebugEvent?.Invoke();
        }
        /// <summary>
        /// Get the current final running state of this tree/fsm
        /// </summary>
        public NodeState RunState => DebugMgr.Instance.GetRunState(this);

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
