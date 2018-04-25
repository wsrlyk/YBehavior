using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class TreeFileMgr : Singleton<TreeFileMgr>
    {
        public void Load()
        {
            if (m_TreeFileInfos.Children != null)
                m_TreeFileInfos.Children.Clear();
            m_FileDic.Clear();

            _LoadDir(Config.Instance.WorkingDir, m_TreeFileInfos);
        }
        private void _LoadDir(string dir, TreeFileInfo parent)
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
                _LoadDir(nextDir.FullName, thisFolder);
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

                m_FileDic.Add(thisFile.Path, thisFile);
            }
        }

        public class TreeFileInfo
        {
            public string DisplayName { get { return Name; } }
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
                    ExportingPath = m_Path.Replace(Config.Instance.WorkingDir, Config.Instance.ExportingDir);
                }
            }
            public string ExportingPath { get; set; }

            private static int s_UntitledIndex = 0;
            public static string UntitledName { get { return "Untitled" + s_UntitledIndex++; } }
        }
        private TreeFileInfo m_TreeFileInfos = new TreeFileInfo();
        private Dictionary<string, TreeFileInfo> m_FileDic = new Dictionary<string, TreeFileInfo>();

        public TreeFileInfo ReloadAndGetAllTrees()
        {
            Load();
            return m_TreeFileInfos;
        }
        public TreeFileInfo AllTrees
        {
            get { return m_TreeFileInfos; }
        }

        public TreeFileInfo GetFileInfo(string path)
        {
            if (m_FileDic.TryGetValue(path, out TreeFileInfo info))
                return info;
            return null;
        }
    }
}
