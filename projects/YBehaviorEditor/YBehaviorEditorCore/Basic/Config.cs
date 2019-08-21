using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class Config : Singleton<Config>
    {
        public string WorkingDir { get; set; }
        public string ExportingDir { get; set; }

        public bool PrintIntermediateInfo { get; set; }

        private string m_DebugIP;
        public string DebugIP
        {
            get { return m_DebugIP; }
            set
            {
                if (m_DebugIP != value)
                {
                    m_DebugIP = value;
                    IniFile configFile = new IniFile(Environment.CurrentDirectory + "\\user.ini");
                    configFile.WriteString("Debug", "IP", m_DebugIP);
                }
            }
        }

        private string m_DebugPort;
        public string DebugPort
        {
            get { return m_DebugPort; }
            set
            {
                if (m_DebugPort != value)
                {
                    m_DebugPort = value;
                    IniFile configFile = new IniFile(Environment.CurrentDirectory + "\\user.ini");
                    configFile.WriteString("Debug", "Port", m_DebugPort);
                }
            }
        }

        private string m_ExpandedFolders;
        public string ExpandedFolders
        {
            get { return m_ExpandedFolders; }
            set
            {
                if (m_ExpandedFolders != value)
                {
                    m_ExpandedFolders = value;
                    IniFile configFile = new IniFile(Environment.CurrentDirectory + "\\user.ini");
                    configFile.WriteString("Editor", "ExpandedFolders", m_ExpandedFolders);
                }
            }
        }
        public Config()
        {
            Load();
        }

        void Load()
        {
            IniFile configFile = new IniFile(Environment.CurrentDirectory + "\\config.ini");
            WorkingDir = configFile.ReadString("Config", "WorkingDir", "");
            ExportingDir = configFile.ReadString("Config", "ExportingDir", "");
            DescriptionMgr.Instance.Load(Environment.CurrentDirectory + "\\description.xml");

            New.ExternalActionMgr.Instance.Load(configFile.ReadString("Config", "ExternalAction", "actions.xml"));


            DirectoryInfo workingDir = new DirectoryInfo(WorkingDir);
            WorkingDir = workingDir.FullName;
            if (!WorkingDir.EndsWith("\\"))
                WorkingDir = WorkingDir + "\\";
            DirectoryInfo exportDir = new DirectoryInfo(ExportingDir);
            ExportingDir = exportDir.FullName;
            if (!ExportingDir.EndsWith("\\"))
                ExportingDir = ExportingDir + "\\";

            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            configFile = new IniFile(Environment.CurrentDirectory + "\\user.ini");
            m_DebugIP = configFile.ReadString("Debug", "IP", "127.0.0.1");
            m_DebugPort = configFile.ReadString("Debug", "Port", "444");

            PrintIntermediateInfo = configFile.ReadInt("Debug", "PrintIntermediateInfo", 0) != 0;

            m_ExpandedFolders = configFile.ReadString("Editor", "ExpandedFolders", "");
        }
    }
}
