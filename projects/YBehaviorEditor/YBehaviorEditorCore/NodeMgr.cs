using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class NodeMgr
    {
        public static TreeMgr Instance { get { return s_Instance; } }
        static TreeMgr s_Instance = new TreeMgr();

        public void Load()
        {
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

        public List<TreeFileInfo> GetAllTrees()
        {
            Load();
            return m_TreeFileInfos.children;
        }

    }

    public class DepartmentModel
    {
        public List<DepartmentModel> Nodes { get; set; }
        public DepartmentModel()
        {
            this.Nodes = new List<DepartmentModel>();
            this.ParentId = 0;//主节点的父id默认为0
        }
        public int id { get; set; }//id
        public string deptName { get; set; }//部门名称
        public int ParentId { get; set; }//父类id
    }

}
