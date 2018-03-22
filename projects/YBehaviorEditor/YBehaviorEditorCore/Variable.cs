﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class Variable : System.ComponentModel.INotifyPropertyChanged
    {
        public static readonly char ListSpliter = '|';
        public static readonly char SpaceSpliter = ' ';

        public static readonly char NONE = (char)0;
        public static readonly char INT = 'I';
        public static readonly char FLOAT = 'F';
        public static readonly char BOOL = 'B';
        public static readonly char VECTOR3 = 'V';
        public static readonly char STRING = 'S';
        public static readonly char ENUM = 'E';
        public static readonly char AGENT = 'A';

        public static readonly char POINTER = 'S';
        public static readonly char CONST = 'C';

        public static readonly char SINGLE = '_';

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
            VT_ENUM,
            VT_AGENT,
        }

        public static Bimap<ValueType, char> ValueTypeDic = new Bimap<ValueType, char>
        {
            {ValueType.VT_INT, INT },
            {ValueType.VT_FLOAT, FLOAT },
            {ValueType.VT_BOOL, BOOL },
            {ValueType.VT_VECTOR3, VECTOR3 },
            {ValueType.VT_STRING, STRING },
            {ValueType.VT_ENUM, ENUM },
            {ValueType.VT_AGENT, AGENT }
        };

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
        public static Bimap<VariableType, char> VariableTypeDic = new Bimap<VariableType, char>
        {
            {VariableType.VBT_Const, CONST },
            {VariableType.VBT_Pointer, POINTER },
        };

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
                RefreshCandidates();
                _OnConditionChanged();
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
                RefreshCandidates();
                _OnConditionChanged();
            }
        }
        public VariableType vbType
        {
            get { return m_vbType; }
            set
            {
                m_vbType = value;
                RefreshCandidates();
                _OnConditionChanged();
            }
        }

        List<ValueType> m_vTypeSet = new List<ValueType>();
        List<CountType> m_cTypeSet = new List<CountType>();
        public List<ValueType> vTypeSet { get { return m_vTypeSet; } }
        public List<CountType> cTypeSet { get { return m_cTypeSet; } }

        string m_Value;
        string m_Name;
        string m_Params = null;
        bool m_bAlwaysConst = false;
        bool m_bCanbeRemoved = false;
        public SharedData SharedData { get; set; } = null;
        public SharedData Container { get; set; } = null;

        public Variable(SharedData sharedData) => SharedData = sharedData;

        static List<string> candidatesBuffer = new List<string>();
        public void RefreshCandidates(bool bForce = false)
        {
            if (!m_bInited && !bForce)
                return;
            m_bInited = true;
            if (SharedData == null || vbType != VariableType.VBT_Pointer)
            {
                Candidates.Clear();
            }
            //else if (m_bCandidatesDirty)
            else
            {
                //m_bCandidatesDirty = false;
                using (var handler = Candidates.Delay())
                {
                    Candidates.Clear();
                    foreach (var v in SharedData.Datas.Values)
                    {
                        if (v.vType == vType && v.cType == cType)
                            Candidates.Add(v.Name);
                    }
                }
            }
        }
        public DelayableNotificationCollection<string> Candidates { get; } = new DelayableNotificationCollection<string>();

        //bool m_bCandidatesDirty = true;
        bool m_bInited = false;

        public string[] Enums
        {
            get
            {
                if (m_Params == null || vType != ValueType.VT_ENUM)
                    return null;
                return m_Params.Split(ListSpliter);
            }
        }
        public string Name { get { return m_Name; } }
        public string Value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                _OnValueChanged();
                _OnConditionChanged();
            }
        }

        private void _OnValueChanged()
        {
            OnPropertyChanged("Value");
            Container.OnVariableValueChanged(this);
        }
        private void _OnConditionChanged()
        {
            OnPropertyChanged("IsValid");
        }

        public bool AlwaysConst { get { return m_bAlwaysConst; } set { m_bAlwaysConst = value; } }
        public bool CanBeRemoved { get { return m_bCanbeRemoved; } set { m_bCanbeRemoved = value; } }
        public bool CanSwitchConst { get { return !AlwaysConst; } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        public string ValueInXml {
            get
            {
                char _vtype = ValueTypeDic.GetValue(m_vType, INT);
                char _ctype = m_cType == CountType.CT_LIST ? _vtype : SINGLE;
                char _vbtype = VariableTypeDic.GetValue(m_vbType, CONST);
                StringBuilder sb = new StringBuilder();
                sb.Append(_ctype).Append(_vtype).Append(_vbtype).Append(' ').Append(Value);
                return sb.ToString();
            }
        }

        public bool IsValid
        {
            get { return CheckValid(); }
        }

        public bool CheckValid()
        {
            if (SharedData == null)
                return false;
            if (vbType == VariableType.VBT_Pointer)
            {
                if (AlwaysConst)
                {
                    LogMgr.Instance.Error(string.Format("This variable cant be pointer: {0}.", Name));
                    return false;
                }
                if (string.IsNullOrEmpty(Value))
                    return false;
                Variable other = SharedData.GetVariable(Value);
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
                case ValueType.VT_AGENT:
                    {
                        /// TODO
                        return true;
                    }
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
            m_bInited = false;
            vType = ValueTypeDic.GetKey(valueType, ValueType.VT_NONE);
            if (vType == ValueType.VT_NONE)
                return false;

            cType = GetCountType(valueType, countType);

            vbType = VariableTypeDic.GetKey(variableType, VariableType.VBT_NONE);
            if (vbType == VariableType.VBT_NONE)
                return false;

            m_Value = value;
            if (param != null)
                m_Params = param;

            RefreshCandidates(true);
            return true;
        }
        public bool SetVariable(ValueType vtype, CountType ctype, VariableType vbtype, string value, string param = null, string newName = null)
        {
            m_bInited = false;
            if (newName != null)
                m_Name = newName;

            vType = vtype;
            if (vType == ValueType.VT_NONE)
                return false;

            cType = ctype;

            vbType = vbType;
            if (vbType == VariableType.VBT_NONE)
                return false;

            m_Value = value;
            if (param != null)
                m_Params = param;

            RefreshCandidates(true);

            return true;
        }

        public static Variable CreateVariableInNode(string name, string defaultValue, ValueType[] valueType, CountType[] countType, VariableType vbType, string param = null)
        {
            Variable v = new Variable(null);
            v.vTypeSet.AddRange(valueType);
            v.cTypeSet.AddRange(countType);
            v.vbType = vbType;

            v.SetVariable(valueType[0], countType[0], vbType, defaultValue, param, name);
            return v;
        }

        //public static Variable CreateVariable(SharedData sharedData, char valueType, char countType, char variableType, string name, string value, string param = null)
        //{
        //    Variable v = new Variable(sharedData);

        //    if (!v.SetVariable(valueType, countType, variableType, value, param))
        //        return null;

        //    v.m_Name = name;

        //    return v;
        //}
    }

}
