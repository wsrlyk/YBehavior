using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class WorkBenchMgr : Singleton<WorkBenchMgr>
    {
        List<WorkBench> m_OpenedWorkBenchs = new List<WorkBench>();
        WorkBench m_ActiveWorkBench;
        public WorkBench ActiveWorkBench { get { return m_ActiveWorkBench; } }
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
        }

        public void ConnectNodes(Connector ctr0, Connector ctr1)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.ConnectNodes(ctr0, ctr1);
        }

        public void DisconnectNodes(Connection.FromTo conn)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.DisconnectNodes(conn);
        }

        public void RemoveNode(NodeBase node)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveNode(node);
        }

        public void RemoveRenderers(NodeBase node, bool excludeRoot)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveRenderers(node, excludeRoot);
        }

        public void AddNode(NodeBase node)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddNode(node);
        }

        public void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddRenderers(node, batchAdd, excludeRoot);
        }

        public void RemoveComment(Comment comment)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveComment(comment);
        }

        public void AddComment(Comment comment)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddComment(comment);
        }

        public void CreateComment()
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.CreateComment();
        }

        public void RefreshNodeUID()
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.MainGraph.RefreshNodeUID();
        }
        public void Remove(WorkBench target)
        {
            m_OpenedWorkBenchs.Remove(target);
            if (m_ActiveWorkBench == target)
                m_ActiveWorkBench = null;
        }

        private void _NodeMoved(EventArg arg)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.OnNodeMoved(arg);
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

        public WorkBench OpenWorkBench(TreeFileMgr.TreeFileInfo file)
        {
            WorkBench bench = OpenWorkBenchInBackGround(file);
            if (bench != null)
                m_ActiveWorkBench = bench;

            return bench;
        }
        public WorkBench OpenWorkBenchInBackGround(TreeFileMgr.TreeFileInfo file)
        {
            if (file == null)
                return null;

            foreach (var t in m_OpenedWorkBenchs)
            {
                if (t.FileInfo == file)
                {
                    return t;
                }
            }

            WorkBench workBench;
            if (file.FileType == FileType.TREE)
            {
                workBench = new TreeBench
                {
                    FilePath = file.Path
                };
            }
            else
            {
                workBench = new FSMBench
                {
                    FilePath = file.Path
                };
            }

            WorkBench oldBench = m_ActiveWorkBench;
            m_ActiveWorkBench = workBench;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file.Path);

            XmlElement root = xmlDoc.DocumentElement;
            if (!workBench.Load(root))
            {
                m_ActiveWorkBench = oldBench;
                return null;
            }

            m_ActiveWorkBench = oldBench;
            m_OpenedWorkBenchs.Add(workBench);

            return workBench;
        }

        public void TrySaveAndExport(WorkBench bench = null)
        {
            if (NetworkMgr.Instance.IsConnected)
            {
                ShowSystemTipsArg arg = new ShowSystemTipsArg
                {
                    Content = "Still Connecting..",
                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                };
                EventMgr.Instance.Send(arg);
            }
            else
                SaveAndExport(bench);

        }

        public static readonly int SaveResultFlag_None = 0;
        public static readonly int SaveResultFlag_WithError = 1;
        public static readonly int SaveResultFlag_NewFile = 1 << 1;
        public static readonly int SaveResultFlag_Saved = 1 << 2;

        public int SaveAndExport(WorkBench bench = null)
        {
            int res = SaveWorkBench(bench);
            if ((res & SaveResultFlag_Saved) != 0)
            {
                ExportWorkBench(bench);

                if (bench == null)
                    bench = ActiveWorkBench;
                bench.FilePath = bench.FileInfo.Path;

                TreeFileMgr.Instance.Load();

                WorkBenchSavedArg arg = new WorkBenchSavedArg()
                {
                    bCreate = (res & SaveResultFlag_NewFile) != 0,
                    Bench = bench
                };
                EventMgr.Instance.Send(arg);

                ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg();
                if ((res & SaveResultFlag_WithError) != 0)
                {
                    showSystemTipsArg.Content = "Saved with some errors.";
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
        /// 
        /// </summary>
        /// <param name="bench"></param>
        /// <returns> 0: normal; 1: new file created; -1: cancel; -2: error</returns>
        public int SaveWorkBench(WorkBench bench = null)
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
                LogMgr.Instance.Error("Something wrong in file.");
                ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                {
                    Content = "Saved With Errors.",
                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                };
                EventMgr.Instance.Send(showSystemTipsArg);
                res |= SaveResultFlag_WithError;
            }

            if (string.IsNullOrEmpty(bench.FilePath))
            {
                ///> Pop the save dialog
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.InitialDirectory = Config.Instance.WorkingDir;
                sfd.Filter = "XML|*.xml";
                if (sfd.ShowDialog() == true)
                {
                    bench.FileInfo.Path = sfd.FileName;
                    bench.FileInfo.Name = sfd.SafeFileName.Remove(sfd.SafeFileName.LastIndexOf(".xml"));

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
            xmlDoc.AppendChild(el);

            bench.Save(el, xmlDoc);
            xmlDoc.Save(bench.FileInfo.Path);

            LogMgr.Instance.Log("Saved to " + bench.FileInfo.Path);
            res |= SaveResultFlag_Saved;
            return res;
        }

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

        public NodeBase CreateNodeToBench(NodeBase template)
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
            EventMgr.Instance.Send(arg);

            return node;
        }

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
            EventMgr.Instance.Send(arg);

            return node;
        }

        public void CopyNode(NodeBase template, bool bIncludeChildren)
        {
            CopiedSubTree = Utility.CloneNode(template, bIncludeChildren);
        }

        public void PasteCopiedToBench()
        {
            PasteNodeToBench(CopiedSubTree, true);
        }

        public NodeBase PasteNodeToBench(NodeBase template, bool bIncludeChildren)
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
            EventMgr.Instance.Send(arg);

            return node;
        }

        public WorkBench CreateNewBench(FileType type)
        {
            WorkBench workBench = null;
            if (type == FileType.TREE)
            {
                workBench = new TreeBench
                {
                    FilePath = string.Empty,
                    FileInfo = new TreeFileMgr.TreeFileInfo()
                    {
                        Name = TreeFileMgr.TreeFileInfo.UntitledName,
                        Path = string.Empty,
                        FileType = type,
                    }
                };
            }
            else if (type == FileType.FSM)
            {
                workBench = new FSMBench
                {
                    FilePath = string.Empty,
                    FileInfo = new TreeFileMgr.TreeFileInfo()
                    {
                        Name = TreeFileMgr.TreeFileInfo.UntitledName,
                        Path = string.Empty,
                        FileType = type,
                    }
                };
            }

            if (workBench == null)
                return null;

            m_OpenedWorkBenchs.Add(workBench);
            m_ActiveWorkBench = workBench;

            workBench.InitEmpty();
            return workBench;
        }

        public Lock CommandLocker { get; } = new Lock();
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

        public List<WorkBench> OpenAllRelated()
        {
            if (ActiveWorkBench == null)
                return null;

            HashSet<string> loaded = new HashSet<string>();
            Queue<WorkBench> loading = new Queue<WorkBench>();
            HashSet<string> toload = new HashSet<string>();
            List<WorkBench> res = new List<WorkBench>();
            Action<NodeBase> action = new Action<NodeBase>
                (
                    (NodeBase node) =>
                    {
                        SubTreeNode subTreeNode = node as SubTreeNode;
                        if (subTreeNode != null)
                            toload.Add(subTreeNode.Variables.GetVariable("Tree").Value);
                    }
                );

            loading.Enqueue(ActiveWorkBench);
            loaded.Add(ActiveWorkBench.FilePath);
            res.Add(ActiveWorkBench);
            while (loading.Count > 0)
            {
                TreeBench bench = loading.Dequeue() as TreeBench;
                Utility.OperateNode(bench.Tree.Root, true, action);

                foreach (string toloadtree in toload)
                {
                    System.IO.FileInfo file = new System.IO.FileInfo(Config.Instance.WorkingDir + toloadtree + ".xml");
                    if (!file.Exists || loaded.Contains(file.FullName))
                        continue;

                    TreeFileMgr.TreeFileInfo fileInfo = TreeFileMgr.Instance.GetFileInfo(file.FullName);
                    WorkBench newBench = OpenWorkBenchInBackGround(fileInfo);

                    WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                    arg.Bench = newBench;
                    EventMgr.Instance.Send(arg);

                    res.Add(newBench);
                    loaded.Add(newBench.FilePath);
                    loading.Enqueue(newBench);
                }

                toload.Clear();
            }

            return res;
        }
        public List<WorkBench> OpenAList(IEnumerable<string> list)
        {
            List<WorkBench> res = new List<WorkBench>();

            foreach (string treename in list)
            {
                System.IO.FileInfo file = new System.IO.FileInfo(Config.Instance.WorkingDir + treename + ".xml");
                if (!file.Exists)
                    continue;

                TreeFileMgr.TreeFileInfo fileInfo = TreeFileMgr.Instance.GetFileInfo(file.FullName);
                WorkBench newBench = OpenWorkBenchInBackGround(fileInfo);

                WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                arg.Bench = newBench;
                EventMgr.Instance.Send(arg);

                res.Add(newBench);
            }

            return res;
        }

    }
}
