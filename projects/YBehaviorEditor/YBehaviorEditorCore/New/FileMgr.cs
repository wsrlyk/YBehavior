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
    /// <summary>
    /// File list management
    /// </summary>
    public class FileMgr : Singleton<FileMgr>
    {
        public static readonly string TreeExtension = ".tree";

        ////private List<List<string>> m_Folders = new List<List<string>>();

        /// <summary>
        /// Import file information into FileMgr
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="relativeName"></param>
        public void Load(string relativePath, string relativeName)
        {
            if (m_FileDic.ContainsKey(relativeName))
            {
                return;
            }

            System.IO.DirectoryInfo TheFolder = new System.IO.DirectoryInfo(Config.Instance.WorkingDirWin);
            if (!TheFolder.Exists)
                return;

            FileInfo thisFile = new FileInfo();

            int slashIdx = relativePath.IndexOf('/');
            while (slashIdx >= 0)
            {
                string folderName = relativePath.Substring(0, slashIdx);

                var res = TheFolder.GetDirectories(folderName);
                if (res == null || res.Length == 0)
                {
                    LogMgr.Instance.Error("Folder not exist: " + folderName);
                    return;
                }
                TheFolder = res[0];

                relativePath = relativePath.Substring(slashIdx + 1);
                if (thisFile.FolderStack == null)
                    thisFile.FolderStack = new List<string>();
                thisFile.FolderStack.Add(folderName);

                slashIdx = relativePath.IndexOf('/');
            }

            var fileres = TheFolder.GetFiles(relativePath);
            if (fileres == null || fileres.Length == 0)
            {
                LogMgr.Instance.Error("File not exist: " + relativePath);
                return;
            }
            System.IO.FileInfo file = fileres[0];
            thisFile.Path = file.FullName;
            thisFile.FileType = file.Extension == ".fsm" ? FileType.FSM : FileType.TREE;

            m_FileInfos.Add(thisFile);
            m_FileDic.Add(thisFile.RelativeName, thisFile);
            if (thisFile.FileType == FileType.TREE)
                m_TreeList.Add(thisFile.RelativeName);
        }
        /// <summary>
        /// Import all files in WorkingDir into FileMgr 
        /// </summary>
        public void Load()
        {
            using (var h = m_TreeList.Delay())
            {
                m_TreeList.Clear();
                m_FileInfos.Clear();
                m_FileDic.Clear();
                m_TreeList.Add(string.Empty);

                System.IO.DirectoryInfo TheFolder = new System.IO.DirectoryInfo(Config.Instance.WorkingDirWin);
                if (!TheFolder.Exists)
                    return;

                List<string> folderStack = new List<string>();
                _LoadDir(TheFolder, folderStack);
            }

            _CheckSuo();
        }
        /// <summary>
        /// Remove invalid file in .suo
        /// </summary>
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
            ////bool bFolderStored = false;
            foreach (System.IO.FileInfo NextFile in TheFolder.GetFiles())
            {
                if (NextFile.Extension != ".tree" && NextFile.Extension != ".fsm" && NextFile.Extension != ".xml")
                    continue;
                ////if (!bFolderStored)
                ////{
                ////    _StoreFolderStack(folderStack);
                ////    bFolderStored = true;
                ////}
                FileInfo thisFile = new FileInfo
                {
                    Path = NextFile.FullName,
                    FileType = NextFile.Extension == ".fsm" ? FileType.FSM : FileType.TREE,
                };

                if (folderStack.Count > 0)
                {
                    thisFile.FolderStack = new List<string>(folderStack);
                }
                ////_GetFolderStack(ref thisFile.FolderStack, folderStack.Count);

                m_FileInfos.Add(thisFile);

                m_FileDic.Add(thisFile.RelativeName, thisFile);
                if (thisFile.FileType == FileType.TREE)
                    m_TreeList.Add(thisFile.RelativeName);
            }
        }

        ////private void _StoreFolderStack(List<string> folderStack)
        ////{
        ////    for (int i = 0; i < folderStack.Count; ++i)
        ////    {
        ////        if (m_Folders.Count <= i)
        ////            m_Folders.Add(new List<string>());
        ////        if (m_Folders[i].Count == 0 || m_Folders[i][m_Folders[i].Count - 1] != folderStack[i])
        ////            m_Folders[i].Add(folderStack[i]);
        ////    }
        ////}

        ////private void _GetFolderStack(ref List<string> folderStack, int deep)
        ////{
        ////    if (deep == 0)
        ////        return;

        ////    folderStack = new List<string>();

        ////    for(int i = 0; i < deep; ++i)
        ////    {
        ////        if (m_Folders[i].Count == 0)
        ////            throw new Exception("There's no folder but try to get one.");
        ////        folderStack.Add(m_Folders[i][m_Folders[i].Count - 1]);
        ////    }
        ////}

        /// <summary>
        /// File Information
        /// </summary>
        public class FileInfo
        {
            /// <summary>
            /// Used in Tab
            /// </summary>
            public string DisplayName { get { return Name; } }
            /// <summary>
            /// Used in Tab tips
            /// </summary>
            public string DisplayPath { get { return RelativeName ?? "NULL"; } }
            /// <summary>
            /// File name
            /// </summary>
            public string Name { get; private set; } = string.Empty;
            /// <summary>
            /// File name with relative path
            /// </summary>
            public string RelativeName { get; private set; } = string.Empty;
            /// <summary>
            /// File relative path
            /// </summary>
            public string RelativePath { get; private set; } = string.Empty;

            public FileType FileType = FileType.TREE;
            private string m_Path = string.Empty;
            /// <summary>
            /// Absolute path
            /// </summary>
            public string Path
            {
                get { return m_Path; }
                set
                {
                    m_Path = value.Replace("\\", "/");


                    if (string.IsNullOrEmpty(m_Path))
                    {
                        Name = UntitledName;
                        RelativeName = Name;
                        RelativePath = m_Path;
                    }
                    else
                    {
                        RelativePath = m_Path.Substring(Config.Instance.WorkingDir.Length);
                        ExportingPath = Config.Instance.ExportingDir + RelativePath;

                        int extIdx = RelativePath.LastIndexOf('.');
                        if (extIdx >= 0)
                        {
                            RelativeName = RelativePath.Remove(extIdx);
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
            /// <summary>
            /// Absolute output path
            /// </summary>
            public string ExportingPath { get; set; }
            private static int s_UntitledIndex = 0;
            /// <summary>
            /// A newly created file will have an untitled name
            /// </summary>
            public static string UntitledName { get { return "Untitled" + s_UntitledIndex++; } }

            public List<string> FolderStack;
            public int FolderDepth { get { return FolderStack == null ? 0 : FolderStack.Count; } }
        }
        private List<FileInfo> m_FileInfos = new List<FileInfo>();
        private DelayableNotificationCollection<string> m_TreeList = new DelayableNotificationCollection<string>();
        private Dictionary<string, FileInfo> m_FileDic = new Dictionary<string, FileInfo>();

        /// <summary>
        /// Reload and get all files
        /// </summary>
        /// <returns></returns>
        public List<FileInfo> ReloadAndGetAllFiles()
        {
            Load();
            return m_FileInfos;
        }
        /// <summary>
        /// Get all files
        /// </summary>
        public List<FileInfo> AllFiles
        {
            get { return m_FileInfos; }
        }
        /// <summary>
        /// Get all trees
        /// </summary>
        public DelayableNotificationCollection<string> TreeList
        {
            get { return m_TreeList; }
        }
        /// <summary>
        /// Get a file by relative name
        /// </summary>
        /// <param name="path">RelativeName</param>
        /// <returns></returns>
        public FileInfo GetFileInfo(string path)
        {
            if (m_FileDic.TryGetValue(path, out FileInfo info))
                return info;
            return null;
        }
    }
}
