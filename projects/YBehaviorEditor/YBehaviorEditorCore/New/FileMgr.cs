using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public enum FileType
    {
        TREE,
        FSM,
    }

    public class FileMgr : Singleton<FileMgr>
    {
        public static readonly string TreeExtension = ".tree";

        private List<List<string>> m_Folders = new List<List<string>>();

        public void Load()
        {
            using (var h = m_TreeList.Delay())
            {
                m_TreeList.Clear();
                m_FileInfos.Clear();
                m_FileDic.Clear();
            
                System.IO.DirectoryInfo TheFolder = new System.IO.DirectoryInfo(Config.Instance.WorkingDirWin);
                if (!TheFolder.Exists)
                    return;

                List<string> folderStack = new List<string>();
                _LoadDir(TheFolder, folderStack);
            }

            _CheckSuo();
        }

        void _CheckSuo()
        {
            List<string> notexist = null;
            foreach (var file in Config.Instance.Suo.Files)
            {
                if (!m_FileDic.ContainsKey(file))
                {
                    if (notexist == null)
                        notexist = new List<string>();
                    notexist.Add(file);
                }
            }

            if (notexist != null)
            {
                foreach (var file in notexist)
                {
                    Config.Instance.Suo.ResetFile(file);
                }
            }
        }

        private void _LoadDir(System.IO.DirectoryInfo TheFolder, List<string> folderStack)
        {
            foreach (System.IO.DirectoryInfo nextDir in TheFolder.GetDirectories())
            {
                folderStack.Add(nextDir.Name);
                _LoadDir(nextDir, folderStack);
                folderStack.RemoveAt(folderStack.Count - 1);
            }
            bool bFolderStored = false;
            foreach (System.IO.FileInfo NextFile in TheFolder.GetFiles())
            {
                if (NextFile.Extension != ".tree" && NextFile.Extension != ".fsm" && NextFile.Extension != ".xml")
                    continue;
                if (!bFolderStored)
                {
                    _StoreFolderStack(folderStack);
                    bFolderStored = true;
                }
                FileInfo thisFile = new FileInfo
                {
                    Path = NextFile.FullName,
                    FileType = NextFile.Extension == ".fsm" ? FileType.FSM : FileType.TREE,
                };

                _GetFolderStack(ref thisFile.FolderStack, folderStack.Count);

                m_FileInfos.Add(thisFile);

                m_FileDic.Add(thisFile.RelativeName, thisFile);
                if (thisFile.FileType == FileType.TREE)
                    m_TreeList.Add(thisFile.RelativeName);
            }
        }

        private void _StoreFolderStack(List<string> folderStack)
        {
            for (int i = 0; i < folderStack.Count; ++i)
            {
                if (m_Folders.Count <= i)
                    m_Folders.Add(new List<string>());
                if (m_Folders[i].Count == 0 || m_Folders[i][m_Folders[i].Count - 1] != folderStack[i])
                    m_Folders[i].Add(folderStack[i]);
            }
        }

        private void _GetFolderStack(ref List<string> folderStack, int deep)
        {
            if (deep == 0)
                return;

            folderStack = new List<string>();

            for(int i = 0; i < deep; ++i)
            {
                if (m_Folders[i].Count == 0)
                    throw new Exception("There's no folder but try to get one.");
                folderStack.Add(m_Folders[i][m_Folders[i].Count - 1]);
            }
        }

        public class FileInfo
        {
            public string DisplayName { get { return Name; } }
            public string DisplayPath { get { return Path ?? "NULL"; } }

            public string Name { get; private set; } = string.Empty;
            public string RelativeName { get; private set; } = string.Empty;
            
            public FileType FileType = FileType.TREE;
            private string m_Path = null;
            public string Path
            {
                get { return m_Path; }
                set
                {
                    m_Path = value.Replace("\\", "/");
                    
                    ExportingPath = m_Path.Replace(Config.Instance.WorkingDir, Config.Instance.ExportingDir);
                    RelativeName = m_Path.Replace(Config.Instance.WorkingDir, string.Empty);

                    if (string.IsNullOrEmpty(m_Path))
                    {
                        Name = UntitledName;
                        RelativeName = Name;
                    }
                    else
                    {
                        int extIdx = RelativeName.LastIndexOf('.');
                        if (extIdx >= 0)
                        {
                            RelativeName = RelativeName.Remove(extIdx);
                        }

                        int slashIdx = RelativeName.LastIndexOf('/');
                        if (slashIdx >= 0)
                        {
                            Name = RelativeName.Substring(slashIdx + 1);
                        }
                        else
                        {
                            Name = RelativeName;
                        }
                    }
                }
            }
            public string ExportingPath { get; set; }
            private static int s_UntitledIndex = 0;
            public static string UntitledName { get { return "Untitled" + s_UntitledIndex++; } }

            public List<string> FolderStack;
            public int FolderDepth { get { return FolderStack == null ? 0 : FolderStack.Count; } }
        }
        private List<FileInfo> m_FileInfos = new List<FileInfo>();
        private DelayableNotificationCollection<string> m_TreeList = new DelayableNotificationCollection<string>();
        private Dictionary<string, FileInfo> m_FileDic = new Dictionary<string, FileInfo>();

        public List<FileInfo> ReloadAndGetAllFiles()
        {
            Load();
            return m_FileInfos;
        }
        public List<FileInfo> AllFiles
        {
            get { return m_FileInfos; }
        }
        public DelayableNotificationCollection<string> TreeList
        {
            get { return m_TreeList; }
        }
        public FileInfo GetFileInfo(string path)
        {
            if (m_FileDic.TryGetValue(path, out FileInfo info))
                return info;
            return null;
        }
    }
}
