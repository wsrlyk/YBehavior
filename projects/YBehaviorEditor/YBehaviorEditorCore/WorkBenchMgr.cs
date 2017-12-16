using System;
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
    }
}
