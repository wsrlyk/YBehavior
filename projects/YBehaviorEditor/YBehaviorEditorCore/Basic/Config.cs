using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class Config : Singleton<Config>
    {
        public string WorkingDirWin { get; set; }
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

        public Suo Suo { get { return m_Suo; } }
        private Suo m_Suo;

        public Config()
        {
            Load();
        }

        void Load()
        {
            IniFile configFile = new IniFile(Environment.CurrentDirectory + "\\config.ini");
            WorkingDirWin = configFile.ReadString("Config", "WorkingDir", "");
            ExportingDir = configFile.ReadString("Config", "ExportingDir", "");
            DescriptionMgr.Instance.Load(Environment.CurrentDirectory + "\\description.xml");

            New.ExternalActionMgr.Instance.Load(configFile.ReadString("Config", "ExternalAction", "actions.xml"));


            DirectoryInfo workingDir = new DirectoryInfo(WorkingDirWin);
            WorkingDirWin = workingDir.FullName;
            if (!WorkingDirWin.EndsWith("\\"))
                WorkingDirWin = WorkingDirWin + "\\";
            DirectoryInfo exportDir = new DirectoryInfo(ExportingDir);
            ExportingDir = exportDir.FullName;
            if (!ExportingDir.EndsWith("\\"))
                ExportingDir = ExportingDir + "\\";

            WorkingDir = WorkingDirWin.Replace("\\", "/");
            ExportingDir = ExportingDir.Replace("\\", "/");

            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            configFile = new IniFile(Environment.CurrentDirectory + "\\user.ini");
            m_DebugIP = configFile.ReadString("Debug", "IP", "127.0.0.1");
            m_DebugPort = configFile.ReadString("Debug", "Port", "444");

            PrintIntermediateInfo = configFile.ReadInt("Debug", "PrintIntermediateInfo", 0) != 0;

            m_ExpandedFolders = configFile.ReadString("Editor", "ExpandedFolders", "");

            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            try
            {
                IFormatter formatter = new BinaryFormatter();
                using (Stream stream = new FileStream(Environment.CurrentDirectory + "\\.suo", FileMode.Open, FileAccess.Read))
                {
                    m_Suo = formatter.Deserialize(stream) as Suo;
                }
            }
            catch (Exception)
            {
                m_Suo = new Suo();
            }
        }

        public void Save()
        {
            WorkBenchMgr.Instance.SaveAllSuos();
            IFormatter formatter = new BinaryFormatter();

            {
                using (Stream stream = new FileStream(Environment.CurrentDirectory + "\\.suo", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    formatter.Serialize(stream, m_Suo);
                }
            }
        }
    }
}
