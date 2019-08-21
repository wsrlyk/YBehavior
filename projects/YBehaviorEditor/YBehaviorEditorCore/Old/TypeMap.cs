using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class TypeMap
    {
        public class Item
        {
            public string SrcVariable;
            public string SrcValue;
            public string DesVariable;
            public Variable.CountType DesCType;
            public Variable.ValueType DesVType;

            public void CloneFrom(Item other)
            {
                SrcVariable = other.SrcVariable;
                SrcValue = other.SrcValue;
                DesVariable = other.DesVariable;
                DesCType = other.DesCType;
                DesVType = other.DesVType;
            }
        }

        Dictionary<KeyValuePair<string, string>, Item> m_Dic = new Dictionary<KeyValuePair<string, string>, Item>();
        public Dictionary<KeyValuePair<string, string>, Item>.ValueCollection Items { get { return m_Dic.Values; } }

        public bool TryAdd(System.Xml.XmlNode data)
        {
            Item item = new Item();
            System.Xml.XmlAttribute attr;

            attr = data.Attributes["SrcVariable"];
            if (attr == null)
                return false;
            item.SrcVariable = attr.Value;

            attr = data.Attributes["SrcValue"];
            if (attr == null)
                return false;
            item.SrcValue = attr.Value;

            attr = data.Attributes["DesVariable"];
            if (attr == null)
                return false;
            item.DesVariable = attr.Value;

            attr = data.Attributes["DesType"];
            if (attr == null)
                return false;

            if (attr.Value.Length == 2)
            {
                char c = attr.Value[1];
                if (!Variable.ValueTypeDic.TryGetKey(c, out item.DesVType))
                    return false;

                item.DesCType = Variable.GetCountType(c, attr.Value[0]);
            }

            m_Dic.Add(new KeyValuePair<string, string>(item.SrcVariable, item.SrcValue), item);
            return true;
        }

        public bool TryGet(Variable v, out Item item)
        {
            item = null;
            if (v.vbType != Variable.VariableType.VBT_Const)
                return false;
            return m_Dic.TryGetValue(new KeyValuePair<string, string>(v.Name, v.Value), out item);
        }

        public void CloneFrom(TypeMap other)
        {
            foreach (var pair in other.m_Dic)
            {
                Item item = new Item();
                item.CloneFrom(pair.Value);
                m_Dic.Add(pair.Key, item);
            }
        }
    }
}
