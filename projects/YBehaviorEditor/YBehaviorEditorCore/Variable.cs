using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class Variable
    {
        public const char ListSpliter = '|';
        public const char SpaceSpliter = ' ';

        public const char NONE = (char)0;
        public const char INT = 'I';
        public const char FLOAT = 'F';
        public const char BOOL = 'B';
        public const char VECTOR3 = 'V';
        public const char STRING = 'S';
        public const char ENUM = 'E';

        public const char POINTER = 'S';
        public const char CONST = 'C';

        public static readonly ValueType[] CreateParams_AllNumbers = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT };
        public static readonly ValueType[] CreateParams_Int = new ValueType[] { ValueType.VT_INT };
        public static readonly ValueType[] CreateParams_Float = new ValueType[] { ValueType.VT_FLOAT };
        public static readonly ValueType[] CreateParams_String = new ValueType[] { ValueType.VT_STRING };
        public static readonly ValueType[] CreateParams_Bool = new ValueType[] { ValueType.VT_BOOL };
        public static readonly ValueType[] CreateParams_Enum = new ValueType[] { ValueType.VT_ENUM };
        public static readonly ValueType[] CreateParams_Vector3 = new ValueType[] { ValueType.VT_VECTOR3 };

        public static readonly CountType[] CreateParams_Single = new CountType[] { CountType.CT_SINGLE };
        public static readonly CountType[] CreateParams_List = new CountType[] { CountType.CT_LIST };
        public static readonly CountType[] CreateParams_AllCounts = new CountType[] { CountType.CT_SINGLE, CountType.CT_LIST };

        public enum ValueType
        {
            VT_NONE,
            VT_INT,
            VT_FLOAT,
            VT_BOOL,
            VT_VECTOR3,
            VT_STRING,
            VT_ENUM
        }
        public static ValueType GetValueType(char type)
        {
            switch(type)
            {
                case INT:
                    return ValueType.VT_INT;
                case FLOAT:
                    return ValueType.VT_FLOAT;
                case BOOL:
                    return ValueType.VT_BOOL;
                case VECTOR3:
                    return ValueType.VT_VECTOR3;
                case STRING:
                    return ValueType.VT_STRING;
                case ENUM:
                    return ValueType.VT_ENUM;
                default:
                    return ValueType.VT_NONE;
            }
        }
        public enum CountType
        {
            CT_NONE,
            CT_SINGLE,
            CT_LIST,
        }

        public static CountType GetCountType(char valueType, char countType)
        {
            if (valueType != countType)
                return CountType.CT_SINGLE;
            return CountType.CT_LIST;
        }

        public enum VariableType
        {
            VBT_NONE,
            VBT_Const,
            VBT_Pointer,
        }

        public static VariableType GetVariableType(char variableType)
        {
            switch (variableType)
            {
                case POINTER:
                    return VariableType.VBT_Pointer;
                case CONST:
                    return VariableType.VBT_Const;
                default:
                    return VariableType.VBT_NONE;
            }
        }

        ValueType m_vType = ValueType.VT_NONE;
        CountType m_cType = CountType.CT_NONE;
        VariableType m_vbType = VariableType.VBT_Const;
        public ValueType vType
        {
            get { return m_vType; }
            set
            {
                if (m_vTypeSet.Count == 0)
                {
                    m_vTypeSet.Add(value);
                }
                if (m_vTypeSet.Contains(value))
                    m_vType = value;
                else
                    m_vType = ValueType.VT_NONE;
            }
        }
        public CountType cType
        {
            get { return m_cType; }
            set
            {
                if (m_cTypeSet.Count == 0)
                {
                    m_cTypeSet.Add(value);
                }
                if (m_cTypeSet.Contains(value))
                    m_cType = value;
                else
                    m_cType = CountType.CT_NONE;
            }
        }
        public VariableType vbType
        {
            get { return m_vbType; }
            set
            {
                m_vbType = value;
            }
        }

        List<ValueType> m_vTypeSet = new List<ValueType>();
        List<CountType> m_cTypeSet = new List<CountType>();
        public List<ValueType> vTypeSet { get { return m_vTypeSet; } }
        public List<CountType> cTypeSet { get { return m_cTypeSet; } }

        string m_Value;
        string m_Name;
        string m_Params = null;
        
        public string Name { get { return m_Name; } }
        public string Value { get { return m_Value; } }
        public bool CheckValid(SharedData data)
        {
            if (vbType == VariableType.VBT_Pointer)
            {
                Variable other = data.GetVariable(Value);
                if (other == null)
                {
                    LogMgr.Instance.Error(string.Format("Pointer doesnt exist for variable {0}, while the pointer is {1} ", Name, Value));
                    return false;
                }

                if (other.vType != vType)
                {
                    LogMgr.Instance.Error(string.Format("Types dont match: {0}.{1} != {2}.{3}", Name, vType, other.Name, other.vType));
                    return false;
                }

                if (other.cType != cType)
                {
                    LogMgr.Instance.Error(string.Format("Types dont match: {0}.{1} != {2}.{3}", Name, cType, other.Name, other.cType));
                    return false;
                }
                return true;
            }

            if (cType == CountType.CT_LIST)
            {
                string[] ss = Value.Split(ListSpliter);
                foreach(var s in ss)
                {
                    if (!CheckValidSingle(s))
                        return false;
                }
                return true;
            }

            return CheckValidSingle(Value);
        }

        private bool CheckValidSingle(string v)
        {
            switch(vType)
            {
                case ValueType.VT_INT:
                    {
                        if (int.TryParse(v, out int a))
                            return true;
                        LogMgr.Instance.Error(string.Format("Variable parse error: int {0} == {1}", Name, v));
                    }
                    break;
                case ValueType.VT_FLOAT:
                    {
                        if (float.TryParse(v, out float a))
                            return true;
                        LogMgr.Instance.Error(string.Format("Variable parse error: float {0} == {1}", Name, v));
                    }
                    break;
                case ValueType.VT_BOOL:
                    {
                        if (bool.TryParse(v, out bool a))
                            return true;
                        LogMgr.Instance.Error(string.Format("Variable parse error: bool {0} == {1}", Name, v));
                    }
                    break;
                case ValueType.VT_STRING:
                    {
                        return true;
                    }
                case ValueType.VT_VECTOR3:
                    {
                        /// TODO
                        return true;
                    }
                case ValueType.VT_ENUM:
                    {
                        string[] ss = m_Params.Split(ListSpliter);
                        foreach (string s in ss)
                        {
                            if (v == s)
                                return true;
                        }
                    }
                    break;
                default:
                    return false;
            }

            return false;
        }

        public bool SetVariableInNode(string s, string newName = null)
        {
            string[] ss = s.Split(SpaceSpliter);
            if (ss.Length > 2 || ss.Length == 0 || ss[0].Length != 3)
            {
                LogMgr.Instance.Error("Format error when set variable from node: " + s);
                return false;
            }
            if (newName != null)
                m_Name = newName;

            return SetVariable(ss[0][1], ss[0][0], ss[0][2], ss.Length == 2 ? ss[1] : string.Empty);
        }

        public bool SetVariable(char valueType, char countType, char variableType, string value, string param = null)
        {
            vType = GetValueType(valueType);
            if (vType == ValueType.VT_NONE)
                return false;

            cType = GetCountType(valueType, countType);

            vbType = GetVariableType(variableType);
            if (vbType == VariableType.VBT_NONE)
                return false;

            m_Value = value;
            m_Params = param;

            return true;
        }

        public static Variable CreateVariableInNode(string name, string defaultValue, ValueType[] valueType, CountType[] countType, VariableType vbType, string param = null)
        {
            Variable v = new Variable();
            v.vTypeSet.AddRange(valueType);
            v.cTypeSet.AddRange(countType);
            v.vbType = vbType;
            v.m_Name = name;
            v.m_Value = defaultValue;
            return v;
        }

        public static Variable CreateVariable(char valueType, char countType, char variableType, string name, string value, string param = null)
        {
            Variable v = new Variable();

            if (!v.SetVariable(valueType, countType, variableType, value, param))
                return null;

            v.m_Name = name;

            return v;
        }
    }

    public class SharedData
    {
        Dictionary<string, Variable> m_Variables = new Dictionary<string, Variable>();

        /// <summary>
        /// This function is only for the variables of the whole tree, not for a node
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAddData(string name, string value)
        {
            Variable v = new Variable();
            if (!v.SetVariableInNode(value, name))
                return false;

            if (!v.CheckValid(this))
                return false;

            AddVariable(v);

            return true;
        }

        public void AddVariable(Variable v)
        {
            if (v == null)
                return;
            m_Variables[v.Name] = v;
        }

        public Variable GetVariable(string name)
        {
            Variable v;
            m_Variables.TryGetValue(name, out v);
            return v;
        }

        public System.Collections.IEnumerable Datas { get { return m_Variables.Values; } }
    }
}
