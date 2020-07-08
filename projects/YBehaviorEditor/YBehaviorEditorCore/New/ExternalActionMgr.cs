using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    class ExternalActionMgr : Singleton<ExternalActionMgr>
    {
        Dictionary<string, ActionTreeNode> m_ActionDic = new Dictionary<string, ActionTreeNode>();

        public ExternalActionMgr()
        {
        }

        public New.ActionTreeNode GetTreeNode(string name)
        {
            if (m_ActionDic.TryGetValue(name, out New.ActionTreeNode node))
            {
                return node.Clone() as New.ActionTreeNode;
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
                    ActionTreeNode node = new ActionTreeNode();
                    node.CreateBase();
                    if (_LoadAction(node, chi))
                    {
                        node.LoadDescription();
                        m_ActionDic[node.ClassName] = node;
                        TreeNodeMgr.Instance.NodeList.Add(node);
                    }
                }
            }
        }

        bool _LoadAction(ActionTreeNode action, XmlNode xml)
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
            action.Type = TreeNodeType.TNT_External;

            attr = xml.Attributes["Hierachy"];
            if (attr != null && int.TryParse(attr.Value, out int hierachy))
            {
                action.Hierachy = hierachy;
            }

            foreach (XmlNode chi in xml.ChildNodes)
            {
                ///> New Variable
                switch (chi.Name)
                {
                    case "Variable":
                        {
                            _LoadVariable(action, chi);
                        }
                        break;
                    case "TypeMap":
                        {
                            _LoadTypeMap(action, chi);
                        }
                        break;
                    default:
                        break;
                }
            }

            return true;
        }

        bool _LoadVariable(ActionTreeNode action, XmlNode xml)
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

            bool bIsLocal = false;
            Variable.VariableType vbType = Variable.VariableType.VBT_NONE;
            ///> Is Enum, always const
            if (valueType.Length == 1 && valueType[0] == Variable.ValueType.VT_ENUM)
            {
                vbType = Variable.VariableType.VBT_Const;
            }
            else
            {
                attr = xml.Attributes["VariableType"];
                if (attr != null)
                {
                    string strVariableType = attr.Value;
                    if (strVariableType.Length == 1)
                    {
                        vbType = Variable.GetVariableType(strVariableType[0], Variable.VariableType.VBT_NONE);
                        if (vbType == Variable.VariableType.VBT_NONE)
                        {
                            LogMgr.Instance.Error("VariableType format error: " + strVariableType);
                            return false;
                        }

                        bIsLocal = Char.IsLower(strVariableType[0]);
                    }
                    else
                    {
                        LogMgr.Instance.Error("Too many VariableType: " + strVariableType);
                        return false;
                    }
                }
            }

            bool isArray = false;
            Variable.CountType countType = Variable.CountType.CT_NONE;
            attr = xml.Attributes["IsArray"];
            if (attr != null)
            {
                isArray = attr.Value == "True";
                countType = attr.Value == "True" ? Variable.CountType.CT_LIST : Variable.CountType.CT_SINGLE;
            }

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

            int vTypeGroup = 0;
            attr = xml.Attributes["vTypeGroup"];
            if (attr != null)
            {
                int.TryParse(attr.Value, out vTypeGroup);
            }
            int cTypeGroup = 0;
            attr = xml.Attributes["cTypeGroup"];
            if (attr != null)
            {
                int.TryParse(attr.Value, out cTypeGroup);
            }

            Variable v = action.NodeMemory.CreateVariable(
                name,
                value,
                valueType,
                countType,
                vbType,
                vTypeGroup,
                cTypeGroup,
                param
            );
            v.IsLocal = bIsLocal;
            return true;
        }

        bool _LoadTypeMap(ActionTreeNode action, XmlNode xml)
        {
            return action.TypeMap.TryAdd(xml);
        }
    }
}
