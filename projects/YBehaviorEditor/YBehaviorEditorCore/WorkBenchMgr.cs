﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class WorkBenchMgr : Singleton<WorkBenchMgr>
    {
        List<WorkBench> m_OpenedWorkBenchs = new List<WorkBench>();
        WorkBench m_ActiveWorkBench;
        public WorkBench ActiveWorkBench { get { return m_ActiveWorkBench; } }

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

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file.Path);

            XmlElement root = xmlDoc.DocumentElement;
            if (!workBench.Load(root))
                return null;

            m_OpenedWorkBenchs.Add(workBench);
            m_ActiveWorkBench = workBench;

            return workBench;
        }

        public void SaveWorkBench(WorkBench bench = null)
        {
            if (bench == null)
                bench = ActiveWorkBench;
            if (bench == null)
            {
                LogMgr.Instance.Error("AddNodeToBench Failed: bench == null");
                return;
            }

            string path = bench.FileInfo.Path;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var el = xmlDoc.CreateElement(bench.FileInfo.Name);
            xmlDoc.AppendChild(el);

            bench.Save(el, xmlDoc);
            xmlDoc.Save(path);
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
            bench.AddSubTree(node);

            return node;
        }
    }
}
