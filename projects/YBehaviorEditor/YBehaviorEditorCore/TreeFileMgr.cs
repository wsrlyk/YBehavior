using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class TreeFileMgr
    {
        public static TreeFileMgr Instance { get { return s_Instance; } }
        static TreeFileMgr s_Instance = new TreeFileMgr();

        public void Load()
        {
            if (m_TreeFileInfos.Children != null)
                m_TreeFileInfos.Children.Clear();
            LoadDir(Config.Instance.WorkingDir, m_TreeFileInfos);
        }
        public void LoadDir(string dir, TreeFileInfo parent)
        {
            DirectoryInfo TheFolder = new DirectoryInfo(dir);
            if (!TheFolder.Exists)
                return;

            TreeFileInfo thisFolder = new TreeFileInfo
            {
                Name = TheFolder.Name,
                bIsFolder = true
            };

            if (parent.Children == null)
                parent.Children = new List<TreeFileInfo>();
            parent.Children.Add(thisFolder);

            foreach (DirectoryInfo nextDir in TheFolder.GetDirectories())
            {
                LoadDir(nextDir.FullName, thisFolder);
            }
            foreach (FileInfo NextFile in TheFolder.GetFiles())
            {
                if (thisFolder.Children == null)
                    thisFolder.Children = new List<TreeFileInfo>();

                if (NextFile.Extension != ".xml")
                    continue;
                TreeFileInfo thisFile = new TreeFileInfo
                {
                    Name = NextFile.Name,
                    Path = NextFile.FullName
                };
                thisFolder.Children.Add(thisFile);
            }
        }

        public class TreeFileInfo
        {
            public string Name { get; set; }
            public List<TreeFileInfo> Children { get; set; }
            public bool bIsFolder = false;
            public string Path { get; set; }
        }
        public TreeFileInfo m_TreeFileInfos = new TreeFileInfo();

        public TreeFileInfo GetAllTrees()
        {
            Load();
            return m_TreeFileInfos;
        }

    }
}
