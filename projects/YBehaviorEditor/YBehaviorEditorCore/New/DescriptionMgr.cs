using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Description to the node and its pins
    /// </summary>
    public class NodeDescription
    {
        Dictionary<string, string> variables = new Dictionary<string, string>();
        public string node;
        public void SetVariable(string name, string content)
        {
            variables[name] = content;
        }
        public string GetVariable(string name)
        {
            variables.TryGetValue(name, out string content);
            return content;
        }
    }

    /// <summary>
    /// Tips to the toolbar command
    /// </summary>
    public struct CommandDescription
    {
        public string name;
        public string tips;
    }
    /// <summary>
    /// Description management
    /// </summary>
    public class DescriptionMgr : Singleton<DescriptionMgr>
    {
        Dictionary<string, string> m_LanguagesDic = new Dictionary<string, string>();
        Dictionary<string, NodeDescription> m_DescriptionDic = new Dictionary<string, NodeDescription>();
        Dictionary<int, string> m_HierachyDic = new Dictionary<int, string>();
        Dictionary<string, CommandDescription> m_CommandDic = new Dictionary<string, CommandDescription>();

        /// <summary>
        /// Get the description by node name
        /// </summary>
        /// <param name="name">Node name</param>
        /// <returns></returns>
        public NodeDescription GetNodeDescription(string name)
        {
            if (m_DescriptionDic.TryGetValue(name, out NodeDescription desc))
            {
                return desc;
            }
            return null;
        }
        /// <summary>
        /// Get the description by toolbar button name
        /// </summary>
        /// <param name="name">Node name</param>
        /// <returns></returns>
        public CommandDescription GetCommandDescription(string name)
        {
            if (m_CommandDic.TryGetValue(name, out var desc))
            {
                return desc;
            }
            return new CommandDescription();
        }
        /// <summary>
        /// Get the name of node hierachy
        /// </summary>
        /// <param name="hierachy"></param>
        /// <returns></returns>
        public string GetHierachyDescription(int hierachy)
        {
            if (m_HierachyDic.TryGetValue(hierachy, out string desc))
            {
                return desc;
            }
            return string.Empty;
        }
        /// <summary>
        /// Load from configuration
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            XmlElement root = xmlDoc.DocumentElement;

            StringBuilder sb = new StringBuilder();
            foreach (XmlNode rootchild in root.ChildNodes)
            {
                if (rootchild.Name == "Languages")
                {
                    foreach (XmlNode node in rootchild.ChildNodes)
                    {
                        m_LanguagesDic[node.Name] = node.InnerText;
                    }
                }
                if (rootchild.Name == "Nodes")
                {
                    foreach (XmlNode node in rootchild.ChildNodes)
                    {
                        sb.Length = 0;
                        NodeDescription desc = new NodeDescription();
                        var attr = node.Attributes["Content"];
                        if (attr != null)
                            sb.Append(attr.Value).Append("\n");

                        attr = node.Attributes["ReturnSuccess"];
                        if (attr != null)
                        {
                            sb.Append("\n");
                            if (m_LanguagesDic.TryGetValue("ReturnSuccess", out var lang))
                                sb.Append(lang).Append(": ");
                            else
                                sb.Append("Success: ");
                            sb.Append(attr.Value);
                        }
                        attr = node.Attributes["ReturnFailure"];
                        if (attr != null)
                        {
                            sb.Append("\n");
                            if (m_LanguagesDic.TryGetValue("ReturnFailure", out var lang))
                                sb.Append(lang).Append(": ");
                            else
                                sb.Append("Failure: ");
                            sb.Append(attr.Value);
                        }

                        desc.node = sb.ToString();
                        foreach (XmlNode chi in node.ChildNodes)
                        {
                            var chiattr = chi.Attributes["Content"];
                            if (chiattr != null)
                                desc.SetVariable(chi.Name, chiattr.Value);
                        }

                        m_DescriptionDic[node.Name] = desc;
                    }
                }
                else if (rootchild.Name == "Hierachies")
                {
                    m_HierachyDic.Clear();
                    foreach (XmlNode node in rootchild.ChildNodes)
                    {
                        var attr = node.Attributes["Value"];
                        if (attr != null && int.TryParse(attr.Value, out int hierachy))
                        {
                            m_HierachyDic.Add(hierachy, node.InnerText);
                        }
                    }
                }
                else if (rootchild.Name == "Commands")
                {
                    m_CommandDic.Clear();
                    foreach (XmlNode node in rootchild.ChildNodes)
                    {
                        var c = new CommandDescription();
                        var attr = node.Attributes["Name"];
                        if (attr != null)
                            c.name = attr.Value;
                        attr = node.Attributes["Content"];
                        if (attr != null)
                            c.tips = attr.Value;
                        m_CommandDic[node.Name] = c;
                    }
                }
            }
        }
    }
}
