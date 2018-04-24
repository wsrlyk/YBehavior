using System;
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
        public WorkBenchMgr()
        {
            EventMgr.Instance.Register(EventType.NodesConnected, _OnNodesConnected);
            EventMgr.Instance.Register(EventType.NodesDisconnected, _OnNodesDisconnected);
            EventMgr.Instance.Register(EventType.RemoveNode, _RemoveNode);
            EventMgr.Instance.Register(EventType.NodeMoved, _NodeMoved);
        }

        private void _OnNodesConnected(EventArg arg)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.OnNodesConnected(arg);
        }
        private void _OnNodesDisconnected(EventArg arg)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.OnNodesDisconnected(arg);
        }
        private void _RemoveNode(EventArg arg)
        {
            if (m_ActiveWorkBench != null)
                m_ActiveWorkBench.RemoveNode(arg);
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
                FileInfo = file
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
            if (bench.FileInfo.Path == null)
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
            if (bench.FileInfo.Path == null)
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

        public Node AddNodeToBench(Node template, WorkBench bench = null)
        {
            if (bench == null)
                bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("AddNodeToBench Failed: bench == null");
                return null;
            }

            Node node = NodeMgr.Instance.CreateNodeByName(template.Name);
            node.Init();
            bench.AddSubTree(node);

            return node;
        }

        public Node CloneNodeToBench(Node template, bool bIncludeChildren, WorkBench bench = null)
        {
            if (bench == null)
                bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("CloneNodeToBench Failed: bench == null");
                return null;
            }

            Node node = Utility.CloneNode(template, bIncludeChildren);
            bench.AddSubTree(node);

            return node;
        }

        public WorkBench CreateNewBench()
        {
            WorkBench workBench = new WorkBench
            {
                FileInfo = new TreeFileMgr.TreeFileInfo()
            };

            workBench.CreateEmptyRoot();

            m_OpenedWorkBenchs.Add(workBench);
            m_ActiveWorkBench = workBench;
            return workBench;
        }
    }
}
