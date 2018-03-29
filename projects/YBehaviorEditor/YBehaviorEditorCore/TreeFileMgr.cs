using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class TreeFileMgr : Singleton<TreeFileMgr>
    {
        public string WorkingPath { get; set; }
        public string ExportingPath { get; set; }
        public void Load()
        {
            if (m_TreeFileInfos.Children != null)
                m_TreeFileInfos.Children.Clear();

            WorkingPath = null;
            DirectoryInfo exportDir = new DirectoryInfo(Config.Instance.ExportingDir);
            if (!exportDir.Exists)
            {
                LogMgr.Instance.Error("ExportingPath not exists: " + Config.Instance.ExportingDir);
                return;
            }
            ExportingPath = exportDir.FullName;

            LoadDir(Config.Instance.WorkingDir, m_TreeFileInfos);
        }
        public void LoadDir(string dir, TreeFileInfo parent)
        {
            DirectoryInfo TheFolder = new DirectoryInfo(dir);
            if (!TheFolder.Exists)
                return;
            if (WorkingPath == null)
                WorkingPath = TheFolder.FullName;

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
                    Name = NextFile.Name.Remove(NextFile.Name.LastIndexOf(NextFile.Extension)),
                    Path = NextFile.FullName,
                };
                thisFolder.Children.Add(thisFile);
            }
        }

        public class TreeFileInfo
        {
            public string DisplayName { get { return Name ?? "Untitled"; } }
            public string DisplayPath { get { return Path ?? "NULL"; } }


            public string Name { get; set; }
            public List<TreeFileInfo> Children { get; set; }
            public bool bIsFolder = false;
            private string m_Path = null;
            public string Path
            {
                get { return m_Path; }
                set
                {
                    m_Path = value;
                    ExportingPath = m_Path.Replace(TreeFileMgr.Instance.WorkingPath, TreeFileMgr.Instance.ExportingPath);
                }
            }
            public string ExportingPath { get; set; }
        }
        public TreeFileInfo m_TreeFileInfos = new TreeFileInfo();

        public TreeFileInfo GetAllTrees()
        {
            Load();
            return m_TreeFileInfos;
        }

    }
}
