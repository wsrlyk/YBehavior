using System;
using System.Collections.Generic;
using System.IO;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Configuration Management
    /// </summary>
    public class Config : Singleton<Config>
    {
        /// <summary>
        /// Working Directory with backslash
        /// </summary>
        public string WorkingDirWin { get; set; }
        /// <summary>
        /// Working Directory
        /// </summary>
        public string WorkingDir { get; set; }
        /// <summary>
        /// Output Directory
        /// </summary>
        public string ExportingDir { get; set; }

        //public bool PrintIntermediateInfo { get; set; }

        /// <summary>
        /// Mouse hover time to show tooltips
        /// </summary>
        public int NodeTooltipDelayTime { get; set; }

        private string m_DebugIP;
        /// <summary>
        /// IP address that will be connected to for debugging
        /// </summary>
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
        /// <summary>
        /// Port that will be connected to for debugging
        /// </summary>
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

        public HashSet<string> ExpandedFolders
        {
            get { return m_Suo.ExpandedFolders; }
        }

        /// <summary>
        /// Preserve and recover some status of the Editor
        /// </summary>
        public Suo Suo { get { return m_Suo; } }
        private Suo m_Suo;

        /// <summary>
        /// Key bindings of some frequently used commands
        /// </summary>
        public KeyBindings KeyBindings { get { return m_KeyBindings; } }
        private KeyBindings m_KeyBindings;

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

            //PrintIntermediateInfo = configFile.ReadInt("Debug", "PrintIntermediateInfo", 0) != 0;

            NodeTooltipDelayTime = configFile.ReadInt("Editor", "NodeTooltipDelayTime", -1);
            if (NodeTooltipDelayTime < 0)
            {
                NodeTooltipDelayTime = 800;
                configFile.WriteInt("Editor", "NodeTooltipDelayTime", 800);
            }
            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            try
            {
                using (StreamReader stream = new StreamReader(Environment.CurrentDirectory + "\\.suo"))
                {
                    m_Suo = Newtonsoft.Json.JsonConvert.DeserializeObject<Suo>(stream.ReadToEnd());
                }
            }
            catch (Exception)
            {
                //LogMgr.Instance.Log(e.ToString());
                m_Suo = new Suo();
            }

            try
            {
                using (StreamReader stream = new StreamReader(Environment.CurrentDirectory + "\\keybindings.json"))
                {
                    m_KeyBindings = Newtonsoft.Json.JsonConvert.DeserializeObject<KeyBindings>(stream.ReadToEnd());
                }
            }
            catch (System.Exception)
            {
                m_KeyBindings = new KeyBindings();
            }
            m_KeyBindings.Init();
        }

        /// <summary>
        /// Save the .suo and keybindings to the file system
        /// </summary>
        public void Save()
        {
            WorkBenchMgr.Instance.SaveAllSuos();
            Newtonsoft.Json.JsonSerializerSettings jsetting = new Newtonsoft.Json.JsonSerializerSettings();
            jsetting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string json2 = Newtonsoft.Json.JsonConvert.SerializeObject(m_Suo, Newtonsoft.Json.Formatting.Indented, jsetting);
            {
                using (StreamWriter stream = new StreamWriter(Environment.CurrentDirectory + "\\.suo"))
                {
                    stream.Write(json2);
                }
            }
            if (m_KeyBindings.IsDirty)
            {
                json2 = Newtonsoft.Json.JsonConvert.SerializeObject(m_KeyBindings, Newtonsoft.Json.Formatting.Indented, jsetting);
                {
                    using (StreamWriter stream = new StreamWriter(Environment.CurrentDirectory + "\\keybindings.json"))
                    {
                        stream.Write(json2);
                    }
                }
            }
        }
    }
}
