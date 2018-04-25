using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    class Config : Singleton<Config>
    {
        public string WorkingDir { get; set; }
        public string ExportingDir { get; set; }

        public Config()
        {
            Load();
        }

        void Load()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("config.xml");
            XmlElement root = xmlDoc.DocumentElement;
            WorkingDir = root.GetAttribute("WorkingDir");
            ExportingDir = root.GetAttribute("ExportingDir");

            DirectoryInfo workingDir = new DirectoryInfo(WorkingDir);
            WorkingDir = workingDir.FullName;

            DirectoryInfo exportDir = new DirectoryInfo(ExportingDir);
            ExportingDir = exportDir.FullName;

            var attr = root.Attributes.GetNamedItem("ExternalAction");
            if (attr != null)
                ExternalActionMgr.Instance.Load(attr.Value);
        }
    }
}
