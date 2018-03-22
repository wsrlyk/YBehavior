using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    class ExternalActionMgr : Singleton<ExternalActionMgr>
    {
        Dictionary<string, ActionNode> m_ActionDic = new Dictionary<string, ActionNode>();
        public ExternalActionMgr()
        {
        }

        public ActionNode GetNode(string name)
        {
            if (m_ActionDic.TryGetValue(name, out ActionNode node))
            {
                return node.Clone();
            }
            return null;
        }
        public void Load(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            XmlElement root = xmlDoc.DocumentElement;

            foreach (XmlNode chi in root.ChildNodes)
            {
                ///> New Action
                if (chi.Name == "Action")
                {
                    ActionNode node = new ActionNode();
                    if (_LoadAction(node, chi))
                    {
                        m_ActionDic[node.ClassName] = node;
                        NodeMgr.Instance.NodeList.Add(node);
                    }
                }
            }
        }

        bool _LoadAction(ActionNode action, XmlNode xml)
        {
            var attr = xml.Attributes.GetNamedItem("Class");
            if (attr == null)
                return false;
            string classname = attr.Value;
            string noteformat = string.Empty;
            attr = xml.Attributes.GetNamedItem("Note");
            if (attr != null)
                noteformat = attr.Value;

            action.ClassName = classname;
            action.NoteFormat = noteformat;
            action.Type = NodeType.NT_Action;

            foreach (XmlNode chi in xml.ChildNodes)
            {
                ///> New Variable
                if (chi.Name == "Variable")
                {
                    _LoadVariable(action, chi);
                }
            }
            return true;
        }

        bool _LoadVariable(ActionNode action, XmlNode xml)
        {
            var attr = xml.Attributes.GetNamedItem("Name");
            if (attr == null)
                return false;
            string name = attr.Value;

            attr = xml.Attributes.GetNamedItem("ValueType");
            if (attr == null)
                return false;
            string valueTypes = attr.Value;
            Variable.ValueType[] valueType = new Variable.ValueType[valueTypes.Length];
            for (int i = 0; i < valueTypes.Length; ++i)
            {
                valueType[i] = Variable.ValueTypeDic.GetKey(valueTypes[i], Variable.ValueType.VT_NONE);
            }
            bool bAlwaysConst = (valueType.Length == 1 && valueType[0] == Variable.ValueType.VT_ENUM);

            bool isArray = false;
            attr = xml.Attributes.GetNamedItem("IsArray");
            if (attr != null)
            {
                isArray = attr.Value == "True";
            }
            Variable.CountType countType = isArray ? Variable.CountType.CT_LIST : Variable.CountType.CT_SINGLE;

            string value = string.Empty;
            attr = xml.Attributes.GetNamedItem("Value");
            if (attr != null)
            {
                value = attr.Value;
            }

            string param = null;
            attr = xml.Attributes.GetNamedItem("Param");
            if (attr != null)
            {
                param = attr.Value;
            }

            Variable v = Variable.CreateVariableInNode(
                name,
                value,
                valueType,
                countType,
                Variable.VariableType.VBT_Const,
                param
            );
            v.AlwaysConst = bAlwaysConst;
            action.Variables.AddVariable(v);
            return true;
        }
    }
}
