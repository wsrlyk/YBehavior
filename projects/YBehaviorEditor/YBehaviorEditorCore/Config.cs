using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    class Config
    {
        public static Config Instance { get { return s_Instance; } }
        static Config s_Instance = new Config();

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
        }
    }
}
