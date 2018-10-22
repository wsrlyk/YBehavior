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
                return node.Clone() as ActionNode;
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
                    node.CreateBase();
                    if (_LoadAction(node, chi))
                    {
                        node.LoadDescription();
                        m_ActionDic[node.ClassName] = node;
                        NodeMgr.Instance.NodeList.Add(node);
                    }
                }
            }
        }

        bool _LoadAction(ActionNode action, XmlNode xml)
        {
            var attr = xml.Attributes["Class"];
            if (attr == null)
                return false;
            string classname = attr.Value;
            string noteformat = string.Empty;
            attr = xml.Attributes["Note"];
            if (attr != null)
                noteformat = attr.Value;
            attr = xml.Attributes["Icon"];
            if (attr != null)
                action.SetIcon(attr.Value);

            action.ClassName = classname;
            action.NoteFormat = noteformat;
            action.Type = NodeType.NT_External;

            attr = xml.Attributes["Hierachy"];
            if (attr != null && int.TryParse(attr.Value, out int hierachy))
            {
                action.Hierachy = (NodeHierachy)hierachy;
            }

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
            var attr = xml.Attributes["Name"];
            if (attr == null)
                return false;
            string name = attr.Value;

            attr = xml.Attributes["ValueType"];
            if (attr == null)
                return false;
            string valueTypes = attr.Value;
            Variable.ValueType[] valueType = new Variable.ValueType[valueTypes.Length];
            for (int i = 0; i < valueTypes.Length; ++i)
            {
                valueType[i] = Variable.ValueTypeDic.GetKey(valueTypes[i], Variable.ValueType.VT_NONE);
            }


            Variable.VariableType vbType = Variable.VariableType.VBT_Const;
            bool bLockVBType = false;
            ///> Is Enum, always const
            if (valueType.Length == 1 && valueType[0] == Variable.ValueType.VT_ENUM)
            {
                bLockVBType = true;
            }
            else
            {
                attr = xml.Attributes["VariableType"];
                if (attr != null)
                {
                    string strVariableType = attr.Value;
                    if (strVariableType.Length == 1)
                    {
                        vbType = Variable.VariableTypeDic.GetKey(strVariableType[0], Variable.VariableType.VBT_NONE);
                        if (vbType == Variable.VariableType.VBT_NONE)
                        {
                            LogMgr.Instance.Error("VariableType format error: " + strVariableType);
                            return false;
                        }
                        else
                        {
                            bLockVBType = true;
                        }
                    }
                    else
                    {
                        LogMgr.Instance.Error("Too many VariableType: " + strVariableType);
                        return false;
                    }
                }
            }


            bool isArray = false;
            attr = xml.Attributes["IsArray"];
            if (attr != null)
            {
                isArray = attr.Value == "True";
            }
            Variable.CountType countType = isArray ? Variable.CountType.CT_LIST : Variable.CountType.CT_SINGLE;

            string value = string.Empty;
            attr = xml.Attributes["Value"];
            if (attr != null)
            {
                value = attr.Value;
            }

            string param = null;
            attr = xml.Attributes["Param"];
            if (attr != null)
            {
                param = attr.Value;
            }

            int typeGroup = 0;
            attr = xml.Attributes["TypeGroup"];
            if (attr != null)
            {
                int.TryParse(attr.Value, out typeGroup);
            }

            Variable v = action.Variables.CreateVariableInNode(
                name,
                value,
                valueType,
                countType,
                vbType,
                bLockVBType,
                typeGroup,
                param
            );
            return true;
        }
    }
}
