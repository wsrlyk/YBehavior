using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// InOut interface management
    /// </summary>
    public class InOutMemoryMgr : Singleton<InOutMemoryMgr>
    {
        Dictionary<string, InOutMemory> m_Dic = new Dictionary<string, InOutMemory>();
        /// <summary>
        /// Get an interface by tree name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public InOutMemory Get(string name)
        {
            if (m_Dic.TryGetValue(name, out InOutMemory inOutMemory))
                return inOutMemory;

            inOutMemory = new InOutMemory(null, false);
            if (!_Load(name, inOutMemory))
                return null;
            m_Dic[name] = inOutMemory;
            return inOutMemory;
        }
        /// <summary>
        /// Reload a tree and get its interface
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public InOutMemory Reload(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            InOutMemory inOutMemory = new InOutMemory(null, false);
            if (!_Load(name, inOutMemory))
                return null;
            m_Dic[name] = inOutMemory;
            return inOutMemory;
        }

        private bool _Load(string name, InOutMemory inOutMemory)
        {
            string path = Config.Instance.WorkingDir + name + FileMgr.TreeExtension;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(path);

                XmlElement root = xmlDoc.DocumentElement;
                foreach (XmlNode chi in root.ChildNodes)
                {
                    if (chi.Name != "Node")
                        continue;
                    XmlAttribute attr = chi.Attributes["Class"];
                    if (attr == null)
                        continue;
                    if (attr.Value != "Root")
                        continue;

                    foreach (XmlNode chi2 in chi.ChildNodes)
                    {
                        if (chi2.Name == "Input" || chi2.Name == "Output")
                        {
                            foreach (System.Xml.XmlAttribute attr2 in chi2.Attributes)
                            {
                                if (Utility.ReservedAttributes.Contains(attr2.Name))
                                    continue;

                                if (!inOutMemory.TryAddData(attr2.Name, attr2.Value, chi2.Name == "Input"))
                                {
                                    LogMgr.Instance.Error("Error when add Input/Output: " + attr2.Name + " " + attr2.Value);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                LogMgr.Instance.Error(e.ToString());
                return false;
            }

            return true;
        }
    }
}
