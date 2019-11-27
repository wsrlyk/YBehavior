using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public enum FileType
    {
        TREE,
        FSM,
        FOLDER
    }

    public class FileMgr : Singleton<FileMgr>
    {
        public void Load()
        {
            if (m_FileInfos.Children != null)
                m_FileInfos.Children.Clear();
            m_FileDic.Clear();
            
            System.IO.DirectoryInfo TheFolder = new System.IO.DirectoryInfo(Config.Instance.WorkingDir);
            if (!TheFolder.Exists)
                return;

            using (var h = m_TreeList.Delay())
            {
                m_TreeList.Clear();
                m_TreeList.Add(string.Empty);
                _LoadDir(TheFolder, m_FileInfos);
            }
        }

        private void _LoadDir(System.IO.DirectoryInfo TheFolder, FileInfo thisFolder)
        {
            if (thisFolder.Children == null)
                thisFolder.Children = new List<FileInfo>();

            foreach (System.IO.DirectoryInfo nextDir in TheFolder.GetDirectories())
            {
                FileInfo childFolder = new FileInfo
                {
                    Name = nextDir.Name,
                    FileType = FileType.FOLDER,
                };
                thisFolder.Children.Add(childFolder);

                _LoadDir(nextDir, childFolder);
            }
            foreach (System.IO.FileInfo NextFile in TheFolder.GetFiles())
            {
                if (thisFolder.Children == null)
                    thisFolder.Children = new List<FileInfo>();

                if (NextFile.Extension != ".tree" && NextFile.Extension != ".fsm" && NextFile.Extension != ".xml")
                    continue;
                FileInfo thisFile = new FileInfo
                {
                    Name = NextFile.Name.Remove(NextFile.Name.LastIndexOf(NextFile.Extension)),
                    Extension = NextFile.Extension,
                    Path = NextFile.FullName,
                    FileType = NextFile.Extension == ".fsm" ? FileType.FSM : FileType.TREE,
                };
                thisFolder.Children.Add(thisFile);

                m_FileDic.Add(thisFile.Path.ToLower(), thisFile);
                if (thisFile.FileType == FileType.TREE)
                    m_TreeList.Add(thisFile.RelativeName);
            }
        }

        public class FileInfo
        {
            public string DisplayName { get { return Name; } }
            public string DisplayPath { get { return Path ?? "NULL"; } }

            public string Extension { get; set; }
            public string Name { get; set; }
            public string RelativeName { get; set; }
            public List<FileInfo> Children { get; set; }
            public FileType FileType = FileType.FOLDER;
            private string m_Path = null;
            public string Path
            {
                get { return m_Path; }
                set
                {
                    m_Path = value.Replace("\\", "/");
                    ExportingPath = m_Path.Replace(Config.Instance.WorkingDir, Config.Instance.ExportingDir);
                    RelativeName = m_Path.Replace(Config.Instance.WorkingDir, string.Empty);
                    RelativeName = RelativeName.Remove(RelativeName.LastIndexOf(Extension));
                }
            }
            public string ExportingPath { get; set; }
            private static int s_UntitledIndex = 0;
            public static string UntitledName { get { return "Untitled" + s_UntitledIndex++; } }
        }
        private FileInfo m_FileInfos = new FileInfo();
        private DelayableNotificationCollection<string> m_TreeList = new DelayableNotificationCollection<string>();
        private Dictionary<string, FileInfo> m_FileDic = new Dictionary<string, FileInfo>();

        public FileInfo ReloadAndGetAllFiles()
        {
            Load();
            return m_FileInfos;
        }
        public FileInfo AllFiles
        {
            get { return m_FileInfos; }
        }
        public DelayableNotificationCollection<string> TreeList
        {
            get { return m_TreeList; }
        }
        public FileInfo GetFileInfo(string path)
        {
            if (m_FileDic.TryGetValue(path.ToLower(), out FileInfo info))
                return info;
            return null;
        }
    }
}
