﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class InOutMemoryMgr : Singleton<InOutMemoryMgr>
    {
        Dictionary<string, InOutMemory> m_Dic = new Dictionary<string, InOutMemory>();

        public InOutMemory Get(string name)
        {
            if (m_Dic.TryGetValue(name, out InOutMemory inOutMemory))
                return inOutMemory;

            inOutMemory = new InOutMemory(null, false);
            _Load(name, inOutMemory);
            m_Dic[name] = inOutMemory;
            return inOutMemory;
        }

        public InOutMemory Reload(string name)
        {
            InOutMemory inOutMemory = new InOutMemory(null, false);
            _Load(name, inOutMemory);
            m_Dic[name] = inOutMemory;
            return inOutMemory;
        }

        private void _Load(string name, InOutMemory inOutMemory)
        {
            string path = Config.Instance.WorkingDir + name + ".xml";
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
                            if (Node.ReservedAttributes.Contains(attr2.Name))
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
    }
}