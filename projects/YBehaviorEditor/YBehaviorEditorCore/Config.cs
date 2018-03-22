using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    class Config : Singleton<Config>
    {
        public string WorkingDir { get; set; }

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

            var attr = root.Attributes.GetNamedItem("ExternalAction");
            if (attr != null)
                ExternalActionMgr.Instance.Load(attr.Value);
        }
    }
}
