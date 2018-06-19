﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class WorkBenchMgr : Singleton<WorkBenchMgr>
    {
        List<WorkBench> m_OpenedWorkBenchs = new List<WorkBench>();
        WorkBench m_ActiveWorkBench;
        public WorkBench ActiveWorkBench { get { return m_ActiveWorkBench; } }
        public string ActiveTreeName
        {
            get
            {
                if (m_ActiveWorkBench == null || m_ActiveWorkBench.FileInfo == null)
                    return string.Empty;
                return m_ActiveWorkBench.FileInfo.Name;
            }
        }

        private Node CopiedSubTree { get; set; }

        public WorkBenchMgr()
        {
            EventMgr.Instance.Register(EventType.NodeMoved, _NodeMoved);
        }

        public void ConnectNodes(ConnectionHolder holder0, ConnectionHolder holder1)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.ConnectNodes(holder0, holder1);
        }
        public void DisconnectNodes(ConnectionHolder childHolder)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.DisconnectNodes(childHolder);
        }
        public void RemoveNode(Node node)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveNode(node);
        }

        public void AddNode(Node node)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.AddNode(node);
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
                m_ActiveWorkBench.RefreshNodeUID();
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
            if (file == null)
                return null;

            foreach (var t in m_OpenedWorkBenchs)
            {
                if (t.FileInfo == file)
                {
                    m_ActiveWorkBench = t;
                    return t;
                }
            }

            WorkBench workBench = new WorkBench
            {
                FilePath = file.Path
            };

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

            m_OpenedWorkBenchs.Add(workBench);

            return workBench;
        }

        public int SaveAndExport(WorkBench bench = null)
        {
            int res = SaveWorkBench(bench);
            if (res >= 0)
            {
                ExportWorkBench(bench);

                if (bench == null)
                    bench = ActiveWorkBench;
                bench.FilePath = bench.FileInfo.Path;

                TreeFileMgr.Instance.Load();

                WorkBenchSavedArg arg = new WorkBenchSavedArg()
                {
                    bCreate = res == 1,
                    Bench = bench
                };
                EventMgr.Instance.Send(arg);
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
            if (bench == null)
                bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("AddNodeToBench Failed: bench == null");
                return -2;
            }

            if (!bench.CheckError())
            {
                LogMgr.Instance.Error("Something wrong in tree. Save Failed.");
                MessageBox.Show("Save Failed.");
                return -2;
            }

            bool bNewFile = false;

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

                    bNewFile = true;
                }
                else
                {
                    return -1;
                }
            }
            
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement(bench.FileInfo.Name);
            xmlDoc.AppendChild(el);

            bench.Save(el, xmlDoc);
            xmlDoc.Save(bench.FileInfo.Path);

            LogMgr.Instance.Log("Saved to " + bench.FileInfo.Path);
            return bNewFile ? 1 : 0;
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
            xmlDoc.Save(bench.FileInfo.ExportingPath);

            return true;
        }

        public Node CreateNodeToBench(Node template)
        {
            Node node;
            using (var locker = this.CommandLocker.StartLock())
            {
                WorkBench bench = ActiveWorkBench;
                if (bench == null)
                {
                    LogMgr.Instance.Error("AddNodeToBench Failed: bench == null");
                    return null;
                }

                node = NodeMgr.Instance.CreateNodeByName(template.Name);

                Utility.InitNode(node, true);
            }
            AddNode(node);

            NewNodeAddedArg arg = new NewNodeAddedArg();
            arg.Node = node;
            EventMgr.Instance.Send(arg);

            return node;
        }

        public Node CloneNodeToBench(Node template, bool bIncludeChildren)
        {
            WorkBench bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("CloneNodeToBench Failed: bench == null");
                return null;
            }

            Node node;
            using (var locker = this.CommandLocker.StartLock())
            {
                node = Utility.CloneNode(template, bIncludeChildren);
                Utility.InitNode(node, true);
            }
            AddNode(node);

            NewNodeAddedArg arg = new NewNodeAddedArg();
            arg.Node = node;
            EventMgr.Instance.Send(arg);

            return node;
        }

        public void CopyNode(Node template, bool bIncludeChildren)
        {
            CopiedSubTree = Utility.CloneNode(template, bIncludeChildren);
        }

        public void PasteCopiedToBench()
        {
            PasteNodeToBench(CopiedSubTree, true);
        }

        public Node PasteNodeToBench(Node template, bool bIncludeChildren)
        {
            if (CopiedSubTree == null)
                return null;

            WorkBench bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("CloneNodeToBench Failed: bench == null");
                return null;
            }

            Node node = Utility.CloneNode(template, bIncludeChildren);
            Utility.InitNode(node, true);

            AddNode(node);

            NewNodeAddedArg arg = new NewNodeAddedArg();
            arg.Node = node;
            EventMgr.Instance.Send(arg);

            return node;
        }

        public WorkBench CreateNewBench()
        {
            WorkBench workBench = new WorkBench
            {
                FilePath = string.Empty,
                FileInfo = new TreeFileMgr.TreeFileInfo()
                {
                    Name = TreeFileMgr.TreeFileInfo.UntitledName,
                    Path = string.Empty
                }
            };

            workBench.CreateEmptyRoot();

            m_OpenedWorkBenchs.Add(workBench);
            m_ActiveWorkBench = workBench;
            return workBench;
        }

        public Lock CommandLocker { get; } = new Lock();
        public void PushCommand(ICommand command)
        {
            if (ActiveWorkBench == null || CommandLocker.IsLocked)
                return;

            ActiveWorkBench.CommandMgr.PushDoneCommand(command);
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
    }
}
