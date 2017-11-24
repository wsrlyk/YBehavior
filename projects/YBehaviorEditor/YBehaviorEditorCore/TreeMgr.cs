using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class TreeMgr
    {
        public static TreeMgr Instance { get { return s_Instance; } }
        static TreeMgr s_Instance = new TreeMgr();

        public void Load()
        {
            if (m_TreeFileInfos.children != null)
                m_TreeFileInfos.children.Clear();
            LoadDir(Config.Instance.WorkingDir, m_TreeFileInfos);
        }
        public void LoadDir(string dir, TreeFileInfo parent)
        {
            DirectoryInfo TheFolder = new DirectoryInfo(dir);
            if (!TheFolder.Exists)
                return;

            TreeFileInfo thisFolder = new TreeFileInfo();
            thisFolder.name = TheFolder.Name;
            thisFolder.bIsFolder = true;

            if (parent.children == null)
                parent.children = new List<TreeFileInfo>();
            parent.children.Add(thisFolder);

            foreach (DirectoryInfo nextDir in TheFolder.GetDirectories())
            {
                LoadDir(nextDir.FullName, thisFolder);
            }
            foreach (FileInfo NextFile in TheFolder.GetFiles())
            {
                if (thisFolder.children == null)
                    thisFolder.children = new List<TreeFileInfo>();

                TreeFileInfo thisFile = new TreeFileInfo();
                thisFile.name = NextFile.Name;
                thisFolder.children.Add(thisFile);
            }
        }

        public class TreeFileInfo
        {
            public string name { get; set; }
            public List<TreeFileInfo> children { get; set; }
            public bool bIsFolder = false;
        }
        public TreeFileInfo m_TreeFileInfos = new TreeFileInfo();

        public TreeFileInfo GetAllTrees()
        {
            Load();
            return m_TreeFileInfos;
        }

    }
}
