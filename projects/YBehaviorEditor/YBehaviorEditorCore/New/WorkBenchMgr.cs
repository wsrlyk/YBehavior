using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Management of workbenches
    /// </summary>
    public class WorkBenchMgr : Singleton<WorkBenchMgr>
    {
        List<WorkBench> m_OpenedWorkBenchs = new List<WorkBench>();
        WorkBench m_ActiveWorkBench;
        /// <summary>
        /// Current active tree/fsm
        /// </summary>
        public WorkBench ActiveWorkBench { get { return m_ActiveWorkBench; } set { m_ActiveWorkBench = value; } }
        /// <summary>
        /// All opened files
        /// </summary>
        public List<WorkBench> OpenedBenches { get { return m_OpenedWorkBenchs; } }
        public string ActiveTreeName
        {
            get
            {
                if (m_ActiveWorkBench == null || m_ActiveWorkBench.FileInfo == null)
                    return string.Empty;
                return m_ActiveWorkBench.FileInfo.Name;
            }
        }

        private NodeBase CopiedSubTree { get; set; }

        public WorkBenchMgr()
        {
            EventMgr.Instance.Register(EventType.NodeMoved, _NodeMoved);
            EventMgr.Instance.Register(EventType.TickResult, _OnTickResult);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
        }
        /// <summary>
        /// Connect two connectors
        /// </summary>
        /// <param name="ctr0"></param>
        /// <param name="ctr1"></param>
        public void ConnectNodes(Connector ctr0, Connector ctr1)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.ConnectNodes(ctr0, ctr1);
        }
        /// <summary>
        /// Break a connection
        /// </summary>
        /// <param name="conn"></param>
        public void DisconnectNodes(Connection.FromTo conn)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.DisconnectNodes(conn);
        }
        /// <summary>
        /// Remove node from active bench
        /// </summary>
        /// <param name="node"></param>
        public void RemoveNode(NodeBase node)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveNode(node);
        }
        /// <summary>
        /// Remove ViewModel of the node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="excludeRoot"></param>
        public void RemoveRenderers(NodeBase node, bool excludeRoot)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveRenderers(node, excludeRoot);
        }
        /// <summary>
        /// Add node to active bench
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(NodeBase node)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddNode(node);
        }
        /// <summary>
        /// Add ViewModel of the node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="batchAdd"></param>
        /// <param name="excludeRoot"></param>
        public void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddRenderers(node, batchAdd, excludeRoot);
        }
        /// <summary>
        /// Remove comment
        /// </summary>
        /// <param name="comment"></param>
        public void RemoveComment(Comment comment)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveComment(comment);
        }
        /// <summary>
        /// Add comment
        /// </summary>
        /// <param name="comment"></param>
        public void AddComment(Comment comment)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddComment(comment);
        }
        /// <summary>
        /// Create comment at a position
        /// </summary>
        /// <param name="viewPos"></param>
        public void CreateComment(Point viewPos)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.CreateComment(viewPos);
        }

        //public void RefreshNodeUID()
        //{
        //    if (m_ActiveWorkBench != null)
        //        m_ActiveWorkBench.MainGraph.RefreshNodeUID(0);
        //}

        /// <summary>
        /// Close a bench
        /// </summary>
        /// <param name="target"></param>
        public void Remove(WorkBench target)
        {
            m_OpenedWorkBenchs.Remove(target);

            {
                WorkBenchClosedArg arg = new WorkBenchClosedArg()
                {
                    Bench = target
                };
                EventMgr.Instance.Send(arg);
            }

            if (m_ActiveWorkBench == target)
            {
                m_ActiveWorkBench = null;
                WorkBenchSelectedArg arg = new WorkBenchSelectedArg()
                {
                    Bench = null
                };
                EventMgr.Instance.Send(arg);
            }

        }

        private void _NodeMoved(EventArg arg)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.OnNodeMoved(arg);
        }
        private void _OnTickResult(EventArg arg)
        {
            if (DebugMgr.Instance.IsSomeBenchDebugging())
            {
                TickResultArg oArg = arg as TickResultArg;
                if (oArg.Token == NetworkMgr.Instance.MessageProcessor.TickResultToken)
                {
                    foreach (var bench in m_OpenedWorkBenchs)
                        bench.RefreshBenchDebug();
                    if (m_ActiveWorkBench != null)
                        m_ActiveWorkBench.RefreshContentDebug();
                }
            }
        }
        private void _OnDebugTargetChanged(EventArg arg)
        {
            foreach (var bench in m_OpenedWorkBenchs)
                bench.RefreshBenchDebug();
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RefreshContentDebug();
        }

        public bool Switch(WorkBench target)
        {
            if (m_ActiveWorkBench != target)
            {
                foreach (WorkBench bench in m_OpenedWorkBenchs)
                {
                    if (bench == target)
                    {
                        m_ActiveWorkBench = target;

                        break;
                    }
                }
            }

            if (m_ActiveWorkBench == target)
            {
                WorkBenchSelectedArg arg = new WorkBenchSelectedArg
                {
                    Bench = m_ActiveWorkBench
                };
                EventMgr.Instance.Send(arg);
                return true;
            }

            LogMgr.Instance.Error("Try to switch to a workbench that is not in the mgr: " + target.FileInfo.Name);
            return false;
        }
        /// <summary>
        /// Open a file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public WorkBench OpenWorkBench(FileMgr.FileInfo file)
        {
            WorkBench bench = _OpenWorkBenchInBackGround(file);
            if (bench != null)
                m_ActiveWorkBench = bench;

            return bench;
        }
        WorkBench _OpenWorkBenchInBackGround(FileMgr.FileInfo file)
        {
            if (file == null)
                return null;

            foreach (var t in m_OpenedWorkBenchs)
            {
                var info = t.FileInfo;
                if (info == file)
                {
                    return t;
                }
            }

            WorkBench workBench;
            if (file.FileType == FileType.TREE)
            {
                workBench = new TreeBench
                {
                    FilePath = file.RelativeName
                };
            }
            else
            {
                workBench = new FSMBench
                {
                    FilePath = file.RelativeName
                };
            }

            WorkBench oldBench = m_ActiveWorkBench;
            m_ActiveWorkBench = workBench;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file.Path);

            XmlElement root = xmlDoc.DocumentElement;
            if (!workBench.Load(root, true))
            {
                m_ActiveWorkBench = oldBench;
                return null;
            }

            m_ActiveWorkBench = oldBench;
            m_OpenedWorkBenchs.Add(workBench);

            return workBench;
        }

        /// <summary>
        /// WorkBench must be managed outside
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public WorkBench OpenWorkBenchTemp(FileMgr.FileInfo file)
        {
            if (file == null)
                return null;

            WorkBench workBench;
            if (file.FileType == FileType.TREE)
            {
                workBench = new TreeBench
                {
                    FilePath = file.RelativeName
                };
            }
            else
            {
                workBench = new FSMBench
                {
                    FilePath = file.RelativeName
                };
            }

            WorkBench oldBench = m_ActiveWorkBench;
            m_ActiveWorkBench = workBench;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file.Path);

            XmlElement root = xmlDoc.DocumentElement;
            if (!workBench.Load(root, false))
            {
                m_ActiveWorkBench = oldBench;
                return null;
            }

            //m_ActiveWorkBench = oldBench;

            return workBench;
        }
        /// <summary>
        /// Save and Export the tree/fsm when not debugging
        /// </summary>
        /// <param name="bench"></param>
        /// <returns></returns>
        public int TrySaveAndExport(WorkBench bench = null)
        {
            if (NetworkMgr.Instance.IsConnected)
            {
                ShowSystemTipsArg arg = new ShowSystemTipsArg
                {
                    Content = "Still Connecting..",
                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                };
                EventMgr.Instance.Send(arg);
                return 0;
            }
            else
                return SaveAndExport(bench);

        }

        public static readonly int SaveResultFlag_None = 0;
        public static readonly int SaveResultFlag_WithError = 1;
        public static readonly int SaveResultFlag_NewFile = 1 << 1;
        public static readonly int SaveResultFlag_Saved = 1 << 2;

        /// <summary>
        /// Save and Export the tree/fsm
        /// </summary>
        /// <param name="bench"></param>
        /// <returns>SaveResultFlag</returns>
        public int SaveAndExport(WorkBench bench)
        {
            int res = SaveWorkBench(bench);
            if ((res & SaveResultFlag_Saved) != 0)
            {
                ExportWorkBench(bench);

                if (bench == null)
                    bench = ActiveWorkBench;

                WorkBenchSavedArg arg = new WorkBenchSavedArg()
                {
                    bCreate = (res & SaveResultFlag_NewFile) != 0,
                    Bench = bench
                };
                EventMgr.Instance.Send(arg);

                ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg();
                if ((res & SaveResultFlag_WithError) != 0)
                {
                    showSystemTipsArg.Content = "Saved with some errors. \nCheck logs for details.";
                    showSystemTipsArg.TipType = ShowSystemTipsArg.TipsType.TT_Error;
                }
                else
                {
                    showSystemTipsArg.Content = "Saved successfully.";
                    showSystemTipsArg.TipType = ShowSystemTipsArg.TipsType.TT_Success;
                }
                EventMgr.Instance.Send(showSystemTipsArg);

            }

            return res;
        }

        /// <summary>
        /// Only save the tree/fsm
        /// </summary>
        /// <param name="bench"></param>
        /// <returns></returns>
        public int SaveWorkBench(WorkBench bench)
        {
            int res = SaveResultFlag_None;
            if (bench == null)
                bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("Save Failed: bench == null");
                return res;
            }

            if (!bench.CheckError())
            {
                //LogMgr.Instance.Error("Something wrong in file.");
                //ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                //{
                //    Content = "Saved With Errors.",
                //    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                //};
                //EventMgr.Instance.Send(showSystemTipsArg);
                res |= SaveResultFlag_WithError;
            }

            if (string.IsNullOrEmpty(bench.FilePath))
            {
                ///> Pop the save dialog
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                var index = bench.FileInfo.RelativeName.LastIndexOf(bench.FileInfo.Name);
                string subdir = bench.FileInfo.RelativeName.Substring(0, index).Replace("/", "\\");
                sfd.InitialDirectory = Config.Instance.WorkingDirWin + subdir;
                sfd.FileName = bench.FileInfo.RelativeName.Substring(index);
                sfd.Filter = bench is TreeBench ? "TREE|*" + FileMgr.TreeExtension : "FSM|*.fsm";
                if (sfd.ShowDialog() == true)
                {
                    bench.FileInfo.Path = sfd.FileName;

                    res |= SaveResultFlag_NewFile;
                }
                else
                {
                    return res;
                }
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement(bench.FileInfo.Name);
            el.SetAttribute("IsEditor", "");
            xmlDoc.AppendChild(el);

            bench.Save(el, xmlDoc);
            xmlDoc.Save(bench.FileInfo.Path);

            LogMgr.Instance.Log("Saved to " + bench.FileInfo.Path);
            res |= SaveResultFlag_Saved;

            if (string.IsNullOrEmpty(bench.FilePath))
            {
                FileMgr.Instance.Load(bench.FileInfo.RelativePath, bench.FileInfo.RelativeName);
                bench.FilePath = bench.FileInfo.RelativeName;
            }

            return res;
        }
        /// <summary>
        /// Only export the tree/fsm
        /// </summary>
        /// <param name="bench"></param>
        /// <returns></returns>
        public bool ExportWorkBench(WorkBench bench = null)
        {
            if (bench == null)
                bench = ActiveWorkBench;
            if (string.IsNullOrEmpty(bench.FileInfo.Path))
            {
                MessageBoxResult dr = MessageBox.Show("This file must be saved first.", "Go to save", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement(bench.FileInfo.Name);
            xmlDoc.AppendChild(el);

            bench.Export(el, xmlDoc);
            new System.IO.FileInfo(bench.FileInfo.ExportingPath).Directory.Create();
            xmlDoc.Save(bench.FileInfo.ExportingPath);

            return true;
        }
        /// <summary>
        /// Create a node at a position
        /// </summary>
        /// <param name="template">Node template, we use its type and name</param>
        /// <param name="viewPos"></param>
        /// <returns></returns>
        public NodeBase CreateNodeToBench(NodeBase template, Point viewPos)
        {
            NodeBase node = null;
            using (var locker = this.CommandLocker.StartLock())
            {
                WorkBench bench = ActiveWorkBench;
                if (bench == null)
                {
                    LogMgr.Instance.Error("AddNodeToBench Failed: bench == null");
                    return null;
                }

                if (template is TreeNode)
                {
                    node = TreeNodeMgr.Instance.CreateNodeByName(template.Name);
                }
                else if (template is FSMNode)
                {
                    node = FSMNodeMgr.Instance.CreateStateByName(template.Name);
                }
                ///> Init Variables
                ////Utility.InitNode(node, true);
            }
            AddNode(node);

            NewNodeAddedArg arg = new NewNodeAddedArg();
            arg.Node = node;
            arg.From = NewNodeAddedArg.AddMethod.New;
            arg.Pos = viewPos;
            arg.PosType = NewNodeAddedArg.PositionType.Final;
            EventMgr.Instance.Send(arg);

            return node;
        }
        /// <summary>
        /// Clone a node
        /// </summary>
        /// <param name="template"></param>
        /// <param name="bIncludeChildren">All children or just single node</param>
        /// <returns></returns>
        public NodeBase CloneTreeNodeToBench(NodeBase template, bool bIncludeChildren)
        {
            WorkBench bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("CloneNodeToBench Failed: bench == null");
                return null;
            }

            NodeBase node;
            using (var locker = this.CommandLocker.StartLock())
            {
                node = Utility.CloneNode(template, bIncludeChildren);
                ////Utility.InitNode(node, true);
            }
            AddNode(node);

            NewNodeAddedArg arg = new NewNodeAddedArg();
            arg.Node = node;
            arg.From = NewNodeAddedArg.AddMethod.Duplicate;
            arg.Pos = template.Geo.Pos + new System.Windows.Vector(15, 15);
            arg.PosType = NewNodeAddedArg.PositionType.Origin;
            EventMgr.Instance.Send(arg);

            return node;
        }
        /// <summary>
        /// Copy the node
        /// </summary>
        /// <param name="template"></param>
        /// <param name="bIncludeChildren">All children or just single node</param>
        public void CopyNode(NodeBase template, bool bIncludeChildren)
        {
            CopiedSubTree = Utility.CloneNode(template, bIncludeChildren);
        }
        /// <summary>
        /// Paste the copied nodes to bench
        /// </summary>
        public void PasteCopiedToBench()
        {
            _PasteNodeToBench(CopiedSubTree, true);
        }

        NodeBase _PasteNodeToBench(NodeBase template, bool bIncludeChildren)
        {
            if (CopiedSubTree == null)
                return null;

            WorkBench bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("CloneNodeToBench Failed: bench == null");
                return null;
            }

            NodeBase node = Utility.CloneNode(template, bIncludeChildren);
            ////Utility.InitNode(node, true);

            AddNode(node);

            NewNodeAddedArg arg = new NewNodeAddedArg();
            arg.Node = node;
            arg.From = NewNodeAddedArg.AddMethod.Paste;
            arg.Pos = new Point(60, 60);
            arg.PosType = NewNodeAddedArg.PositionType.Final;
            EventMgr.Instance.Send(arg);

            return node;
        }
        /// <summary>
        /// Create a tree/fsm
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public WorkBench CreateNewBench(FileType type)
        {
            WorkBench workBench = null;
            if (type == FileType.TREE)
            {
                workBench = new TreeBench()
                {
                    FilePath = string.Empty
                };
            }
            else if (type == FileType.FSM)
            {
                workBench = new FSMBench()
                {
                    FilePath = string.Empty
                };
            }

            if (workBench == null)
                return null;

            workBench.CommandMgr.Dirty = true;
            m_OpenedWorkBenchs.Add(workBench);
            m_ActiveWorkBench = workBench;

            workBench.InitEmpty();
            return workBench;
        }
        /// <summary>
        /// Stop pushing command when last pushing is not finished
        /// </summary>
        public Lock CommandLocker { get; } = new Lock();
        /// <summary>
        /// Push a command for undo/redo
        /// </summary>
        /// <param name="command"></param>
        public void PushCommand(ICommand command)
        {
            if (ActiveWorkBench == null || CommandLocker.IsLocked)
                return;

            ActiveWorkBench.PushDoneCommand(command);
        }

        public void Undo()
        {
            if (ActiveWorkBench == null)
                return;
            ActiveWorkBench.CommandMgr.Undo();
        }

        public void Redo()
        {
            if (ActiveWorkBench == null)
                return;
            ActiveWorkBench.CommandMgr.Redo();
        }
        /// <summary>
        /// Open files with a list of names
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public List<WorkBench> OpenAList(IEnumerable<string> list)
        {
            List<WorkBench> res = new List<WorkBench>();

            foreach (var f in list)
            {
                //System.IO.FileInfo file = null;
                //if (info.Type == GraphType.TREE)
                //    file = new System.IO.FileInfo(Config.Instance.WorkingDirWin + info.Name + FileMgr.TreeExtension);
                //else
                //    file = new System.IO.FileInfo(Config.Instance.WorkingDirWin + info.Name + ".fsm");
                //if (!file.Exists)
                //{
                //    LogMgr.Instance.Error("File not exists: " + file.FullName);
                //    continue;
                //}

                FileMgr.FileInfo fileInfo = FileMgr.Instance.GetFileInfo(f);
                if (fileInfo == null)
                {
                    LogMgr.Instance.Error("File not exists: " + f);
                    continue;
                }
                WorkBench newBench = _OpenWorkBenchInBackGround(fileInfo);

                WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                arg.Bench = newBench;
                EventMgr.Instance.Send(arg);

                res.Add(newBench);
            }

            return res;
        }
        /// <summary>
        /// Save the suo of all opened files
        /// </summary>
        public void SaveAllSuos()
        {
            foreach (var bench in m_OpenedWorkBenchs)
            {
                if (!bench.CommandMgr.Dirty)
                    bench.SaveSuo();
            }
        }
    }

    //public struct BenchInfo
    //{
    //    public string Name;
    //    public GraphType Type;
    //}
}
