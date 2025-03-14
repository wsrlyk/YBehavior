﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Interface of getting collection of data
    /// </summary>
    public interface IVariableDataSource
    {
        TreeMemory SharedData { get; }
        InOutMemory InOutData { get; }
    }
    /// <summary>
    /// A shared or local variable of the tree,
    /// an input or output pin of the tree used in subtree,
    /// or the pin of a tree node.
    /// We would separate them into different classes in the future
    /// </summary>
    public class Variable : System.ComponentModel.INotifyPropertyChanged
    {
        public static readonly char[] ListSpliter = new char[] { '|' };
        public static readonly char[] SequenceSpliter = new char[] { '=' };
        public static readonly char[] Vector3Spliter = new char[] { ',' };
        public static readonly char SpaceSpliter = ' ';

        public static readonly char NONE = (char)0;
        public static readonly char INT = 'I';
        public static readonly char FLOAT = 'F';
        public static readonly char BOOL = 'B';
        public static readonly char VECTOR3 = 'V';
        public static readonly char STRING = 'S';
        public static readonly char ENUM = 'E';
        public static readonly char ENTITY = 'A';
        public static readonly char ULONG = 'U';

        public static readonly char POINTER = 'P';
        public static readonly char CONST = 'C';

        public static readonly char SINGLE = '_';
        public static readonly char ANY = '*';

        public static readonly char ENABLE = 'E';
        public static readonly char DISABLE = 'D';

        public static readonly ValueType[] CreateParams_AllNumbers = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT };
        public static readonly ValueType[] CreateParams_Int = new ValueType[] { ValueType.VT_INT };
        public static readonly ValueType[] CreateParams_Ulong = new ValueType[] { ValueType.VT_ULONG };
        public static readonly ValueType[] CreateParams_Float = new ValueType[] { ValueType.VT_FLOAT };
        public static readonly ValueType[] CreateParams_String = new ValueType[] { ValueType.VT_STRING };
        public static readonly ValueType[] CreateParams_Bool = new ValueType[] { ValueType.VT_BOOL };
        public static readonly ValueType[] CreateParams_Enum = new ValueType[] { ValueType.VT_ENUM };
        public static readonly ValueType[] CreateParams_Vector3 = new ValueType[] { ValueType.VT_VECTOR3 };
        public static readonly ValueType[] CreateParams_Entity = new ValueType[] { ValueType.VT_ENTITY };
        public static readonly ValueType[] CreateParams_AllTypes = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT, ValueType.VT_VECTOR3, ValueType.VT_ULONG, ValueType.VT_STRING, ValueType.VT_BOOL, ValueType.VT_ENTITY };
        public static readonly ValueType[] CreateParams_RandomTypes = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT, ValueType.VT_BOOL };
        public static readonly ValueType[] CreateParams_SwitchTypes = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT, ValueType.VT_BOOL, ValueType.VT_ULONG, ValueType.VT_STRING };
        public static readonly ValueType[] CreateParams_CalculatorTypes = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT, ValueType.VT_STRING, ValueType.VT_VECTOR3 };
        public static readonly ValueType[] CreateParams_ConvertTypes = new ValueType[] { ValueType.VT_INT, ValueType.VT_FLOAT, ValueType.VT_BOOL, ValueType.VT_STRING };

        /// <summary>
        /// Type of value
        /// </summary>
        public enum ValueType
        {
            /// <summary>
            /// Invalid
            /// </summary>
            VT_NONE,
            /// <summary>
            /// Integer
            /// </summary>
            VT_INT,
            /// <summary>
            /// Float
            /// </summary>
            VT_FLOAT,
            /// <summary>
            /// Boolean
            /// </summary>
            VT_BOOL,
            /// <summary>
            /// (x, y, z)
            /// </summary>
            VT_VECTOR3,
            /// <summary>
            /// String
            /// </summary>
            VT_STRING,
            /// <summary>
            /// Enum
            /// </summary>
            VT_ENUM,
            /// <summary>
            /// References to an entity in game
            /// </summary>
            VT_ENTITY,
            /// <summary>
            /// UINT64
            /// </summary>
            VT_ULONG,
        }

        public static Bimap<ValueType, char> ValueTypeDic = new Bimap<ValueType, char>
        {
            {ValueType.VT_INT, INT },
            {ValueType.VT_FLOAT, FLOAT },
            {ValueType.VT_BOOL, BOOL },
            {ValueType.VT_VECTOR3, VECTOR3 },
            {ValueType.VT_STRING, STRING },
            {ValueType.VT_ENUM, ENUM },
            {ValueType.VT_ENTITY, ENTITY },
            {ValueType.VT_ULONG, ULONG }
        };

        public static Bimap<Variable.ValueType, string> ValueTypeDic2 = new Bimap<Variable.ValueType, string>
        {
            {Variable.ValueType.VT_INT, "INT" },
            {Variable.ValueType.VT_FLOAT, "FLOAT" },
            {Variable.ValueType.VT_BOOL, "BOOL" },
            {Variable.ValueType.VT_VECTOR3, "VECTOR3" },
            {Variable.ValueType.VT_STRING, "STRING" },
            {Variable.ValueType.VT_ENUM, "ENUM" },
            {Variable.ValueType.VT_ENTITY, "ENTITY" },
            {Variable.ValueType.VT_ULONG, "ULONG" }
        };

        public static Dictionary<Variable.ValueType, string> DefaultValueDic = new Dictionary<ValueType, string>
        {
            {Variable.ValueType.VT_INT, "0" },
            {Variable.ValueType.VT_FLOAT, "0.0" },
            {Variable.ValueType.VT_BOOL, "F" },
            {Variable.ValueType.VT_VECTOR3, "0,0,0" },
            {Variable.ValueType.VT_STRING, "" },
            {Variable.ValueType.VT_ENTITY, "" },
            {Variable.ValueType.VT_ULONG, "0" }
        };
        /// <summary>
        /// Type of count
        /// </summary>
        public enum CountType
        {
            /// <summary>
            /// Invalid
            /// </summary>
            CT_NONE,
            /// <summary>
            /// Just a single value
            /// </summary>
            CT_SINGLE,
            /// <summary>
            /// Array of multiple values
            /// </summary>
            CT_LIST,
        }

        public static CountType GetCountType(char valueType, char countType)
        {
            if (valueType == countType)
                return CountType.CT_LIST;
            if (countType == ANY)
                return CountType.CT_NONE;
            return CountType.CT_SINGLE;
        }
        /// <summary>
        /// Type of pin
        /// </summary>
        public enum VariableType
        {
            /// <summary>
            /// Invalid
            /// </summary>
            VBT_NONE,
            /// <summary>
            /// A constant value
            /// </summary>
            VBT_Const,
            /// <summary>
            /// A reference to a variable of the tree
            /// </summary>
            VBT_Pointer,
        }
        public static Bimap<VariableType, char> VariableTypeDic = new Bimap<VariableType, char>
        {
            {VariableType.VBT_Const, CONST },
            {VariableType.VBT_Pointer, POINTER },
        };
        public static VariableType GetVariableType(char c, VariableType defaultType)
        {
            c = Char.ToUpper(c);
            return VariableTypeDic.GetKey(c, defaultType);
        }

        public static char GetVariableChar(VariableType type, char defaultChar, bool isLocal)
        {
            char c = VariableTypeDic.GetValue(type, defaultChar);
            if (isLocal)
                c = Char.ToLower(c);
            else
                c = Char.ToUpper(c);
            return c;
        }
        public static bool GetLocal(char c)
        {
            return Char.IsLower(c);
        }
        /// <summary>
        /// Type of Enable/Disable
        /// </summary>
        public enum EnableType
        {
            /// <summary>
            /// Invalid
            /// </summary>
            ET_NONE,
            /// <summary>
            /// Always enabled
            /// </summary>
            ET_FIXED,
            /// <summary>
            /// Currently enabled
            /// </summary>
            ET_Enable,
            /// <summary>
            /// Currently disabled
            /// </summary>
            ET_Disable,
        }
        public static Bimap<EnableType, char> EnableTypeDic = new Bimap<EnableType, char>
        {
            {EnableType.ET_Enable, ENABLE },
            {EnableType.ET_Disable, DISABLE },
            {EnableType.ET_FIXED, NONE },
        };
        public static EnableType GetEnableType(char c, EnableType defaultType)
        {
            c = Char.ToUpper(c);
            return EnableTypeDic.GetKey(c, defaultType);
        }

        public static char GetEnableChar(EnableType type, char defaultChar)
        {
            char c = EnableTypeDic.GetValue(type, defaultChar);
            return c;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ValueType m_vType = ValueType.VT_NONE;
        CountType m_cType = CountType.CT_NONE;
        VariableType m_vbType = VariableType.VBT_Const;
        EnableType m_eType = EnableType.ET_NONE;
        /// <summary>
        /// ValueType
        /// </summary>
        public ValueType vType
        {
            get { return m_vType; }
            set
            {
                if (m_vType == value)
                    return;

                ChangeVariableVTypeCommand command = new ChangeVariableVTypeCommand()
                {
                    OldType = m_vType,
                    NewType = value,
                    Variable = this
                };

                if (m_vTypeSet.Count == 0)
                {
                    m_vTypeSet.Add(value);
                }
                if (m_vTypeSet.Contains(value))
                    m_vType = value;
                else
                    m_vType = ValueType.VT_NONE;
                command.NewType = m_vType;

                RefreshCandidates();
                SetValue(null, IsLocal);
                OnPropertyChanged("vType");
                _OnConditionChanged();
                _OnVTypeChanged();
                WorkBenchMgr.Instance.PushCommand(command);
            }
        }
        /// <summary>
        /// CountType
        /// </summary>
        public CountType cType
        {
            get { return m_cType; }
            set
            {
                if (m_cType == value)
                    return;

                ChangeVariableCTypeCommand command = new ChangeVariableCTypeCommand()
                {
                    OldType = m_cType,
                    NewType = value,
                    Variable = this
                };

                m_cType = value;
                RefreshCandidates();
                //SetValue(null, IsLocal);
                OnPropertyChanged("cType");
                CandidatesReset();
                _OnConditionChanged();
                _OnCTypeChanged();
                WorkBenchMgr.Instance.PushCommand(command);
            }
        }
        /// <summary>
        /// EnableType
        /// </summary>
        public EnableType eType
        {
            get { return m_eType; }
            set
            {
                if (m_eType == value)
                    return;

                ChangeVariableETypeCommand command = new ChangeVariableETypeCommand()
                {
                    OldType = m_eType,
                    NewType = value,
                    Variable = this
                };


                m_eType = value;
                OnPropertyChanged("eType");
                _OnETypeChanged();
                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public event Action CandidatesResetEvent;
        /// <summary>
        /// Invoked when candidates are reset
        /// </summary>
        public void CandidatesReset()
        {
            CandidatesResetEvent?.Invoke();
        }
        /// <summary>
        /// VariableType
        /// </summary>
        public VariableType vbType
        {
            get { return m_vbType; }
            set
            {
                if (m_vbType == value)
                    return;

                ChangeVariableVBTypeCommand command = new ChangeVariableVBTypeCommand()
                {
                    OldType = m_vbType,
                    NewType = value,
                    Variable = this
                };

                m_vbType = value;
                RefreshCandidates();

                SetValue(null, true);

                OnPropertyChanged("vbType");
                _OnConditionChanged();
                _OnVTypeChanged();

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        List<ValueType> m_vTypeSet = new List<ValueType>();
        //List<CountType> m_cTypeSet = new List<CountType>();
        /// <summary>
        /// Collection of ValueTypes this can be
        /// </summary>
        public List<ValueType> vTypeSet { get { return m_vTypeSet; } }
        //public List<CountType> cTypeSet { get { return m_cTypeSet; } }
        bool m_bIsLocal = false;
        /// <summary>
        /// Whether it's local variable
        /// </summary>
        public bool IsLocal
        {
            get { return m_bIsLocal; }
            set
            {
                if (m_bIsLocal != value)
                {
                    m_bIsLocal = value;
                    _RefreshDisplayValue();
                    _OnValueChanged();
                    OnPropertyChanged("DisplayName");
                }
            }
        }
        /// <summary>
        /// Whether the data comes in or goes out
        /// </summary>
        public bool IsInput { get; set; } = true;

        /// <summary>
        /// To tell if it's local or not. Local: aaa'      Shared: aaa
        /// </summary>
        string m_DisplayValue;
        string m_Value;
        string m_Name;
        string m_Params = null;
        bool m_bCanbeRemoved = false;
        /// <summary>
        /// The variables of tree
        /// </summary>
        public IVariableDataSource SharedDataSource { get; set; } = null;
        /// <summary>
        /// The container
        /// </summary>
        public VariableCollection Container { get; set; } = null;
        public string Description { get; set; }

        public Variable(IVariableDataSource sharedData) => SharedDataSource = sharedData;
        /// <summary>
        /// Refresh candidates for a pin when any type, variable, etc. changes
        /// </summary>
        /// <param name="bForce"></param>
        public void RefreshCandidates(bool bForce = false)
        {
            if (!m_bInited && !bForce)
                return;
            m_bInited = true;
            if (SharedDataSource != null && SharedDataSource.SharedData != null)
            {
                if (this is TreeVariable)
                {
                    SharedDataSource.SharedData.RefreshCandidatas();
                }
                else
                {
                    if (IsIndex)
                        Candidates = SharedDataSource.SharedData.Candidatas.GetIndex();
                    else
                        Candidates = SharedDataSource.SharedData.Candidatas.Get(this);
                    OnPropertyChanged("Candidates");
                }
            }
        }

        /// <summary>
        /// If a variable of a node in TreeA has a pointer, but be pasted to TreeB,
        /// TreeB may not have that shared/local variable.
        /// We have to set its value to NULL.
        /// This function must be called after RefreshCandidates.
        /// </summary>
        public void ResetInvalidValue()
        {
            if (vbType == VariableType.VBT_Pointer && !string.IsNullOrEmpty(Value))
            {
                bool bFound = false;
                foreach (var c in Candidates.variables)
                {
                    if (c.variable == null)
                        continue;
                    if (c.variable.IsLocal == IsLocal &&
                        c.variable.Name == Value)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                    SetValue(null, false);
            }
        }
        /// <summary>
        /// Candidates of the pin
        /// </summary>
        public VariableCandidates.Candidates Candidates { get; set; }

        /// <summary>
        /// Parent is for the vectorindex variable
        /// </summary>
        Variable m_Parent;
        Variable m_VectorIndex = null;
        bool m_bVectorIndexEnabled = false;
        /// <summary>
        /// Index pin of array when it's a single pin
        /// </summary>
        public Variable VectorIndex { get { return m_VectorIndex; } set { m_VectorIndex = value; OnPropertyChanged("VectorIndex"); } }
        /// <summary>
        /// Whether it's single but references to an array
        /// </summary>
        public bool IsElement { get { return cType == CountType.CT_SINGLE && m_bVectorIndexEnabled && m_VectorIndex != null; } }
        /// <summary>
        /// Whether it's an index pin
        /// </summary>
        public bool IsIndex { get { return m_Parent != null; } }
        //bool m_bCandidatesDirty = true;
        bool m_bInited = false;
        /// <summary>
        /// Collections of all enum values
        /// </summary>
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
        /// <summary>
        /// Mainly for log
        /// </summary>
        public string DisplayName
        {
            get
            {
                return
                    m_bIsLocal &&
                    this is TreeVariable
                    ? m_Name + "'" : m_Name;
            }
        }
        /// <summary>
        /// Used to build the note of a tree node
        /// </summary>
        public string NoteValue
        {
            get
            {
                if (vbType == VariableType.VBT_Pointer && string.IsNullOrEmpty(Value))
                    return Name;
                if (IsElement)
                    return DisplayValue + '[' + m_VectorIndex.DisplayValue + ']';
                return DisplayValue;
            }
        }
        /// <summary>
        /// Formatted value
        /// </summary>
        public string DisplayValue
        {
            get { return m_DisplayValue; }
            set
            {
                //m_DisplayValue = value;
                ///> TODO: properly process this case
                if (value == null)
                {
                    value = string.Empty;
                    //return;
                }

                bool isLocal = IsLocal;
                if (vbType == VariableType.VBT_Pointer)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        isLocal = true;
                    }
                    else
                    {
                        int p = value.IndexOf("'");
                        if (p >= 0)
                        {
                            value = value.Substring(0, p);
                            isLocal = true;
                        }
                        else
                        {
                            isLocal = false;
                        }
                    }
                }
                SetValue(value, isLocal);
            }
        }
        public string Value
        {
            get { return m_Value; }
        }
        /// <summary>
        /// Set the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isLocal"></param>
        public void SetValue(string value, bool isLocal)
        {
            {
                if (value == null)
                {
                    if (vbType == VariableType.VBT_Const && cType != CountType.CT_LIST)
                    {
                        DefaultValueDic.TryGetValue(vType, out value);
                    }
                    else
                    {
                        value = string.Empty;
                    }
                }

                ChangeVariableValueCommand command = new ChangeVariableValueCommand()
                {
                    OldValue = m_Value,
                    NewValue = value,
                    OldIsLocal = IsLocal,
                    NewIsLocal = isLocal,
                    Variable = this,
                    //OldVectorIndex = m_VectorIndex
                };
                m_Value = value;
                IsLocal = isLocal;

                _RefreshDisplayValue();
                _RefreshIndex();
                _OnValueChanged();
                _OnConditionChanged();

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        private void _RefreshDisplayValue()
        {
            if (vbType == VariableType.VBT_Pointer && IsLocal)
                m_DisplayValue = Value + "'";
            else
                m_DisplayValue = Value;
        }

        private void _RefreshIndex()
        {
            if (!IsIndex)
            {
                if (vbType == VariableType.VBT_Pointer)
                {
                    if (cType == CountType.CT_SINGLE && VariableCandidates.IsNeedIndex(Candidates, DisplayValue))
                    {
                        if (m_VectorIndex == null)
                        {
                            m_VectorIndex = new Variable(SharedDataSource);
                            m_VectorIndex.m_Parent = this;
                            m_VectorIndex.SetVariable(ValueType.VT_INT, CountType.CT_SINGLE, VariableType.VBT_Const, EnableType.ET_FIXED, true, "0", null, this.Name + ".Index");
                            OnPropertyChanged("VectorIndex");
                        }
                        m_bVectorIndexEnabled = true;
                    }
                    else
                    {
                        m_bVectorIndexEnabled = false;
                        //m_VectorIndex = null;
                    }
                }
                else
                {
                    m_bVectorIndexEnabled = false;
                    //m_VectorIndex = null;
                }
            }
        }

        private void _OnValueChanged()
        {
            OnPropertyChanged("DisplayValue");
            OnPropertyChanged("IsElement");
            if (Container != null)
                Container.OnValueChanged(this);
            else if (m_Parent != null && m_Parent.Container != null)
                m_Parent.Container.OnValueChanged(m_Parent);
        }

        private void _OnConditionChanged()
        {
            OnPropertyChanged("IsValid");
        }

        void _OnVTypeChanged()
        {
            Container?.OnVTypeChanged(this);
            //(Container as ISameTypeGroupTypeChanged)?.OnVTypeChanged(this.Name);
        }

        void _OnCTypeChanged()
        {
            Container?.OnCTypeChanged(this);
            //(Container as ISameTypeGroupTypeChanged)?.OnCTypeChanged(this.Name);
        }

        void _OnETypeChanged()
        {
            Container?.OnETypeChanged(this);
        }

        void _OnVBTypeChanged()
        {
            if (Container != null)
                Container.OnVBTypeChanged(this);
            else if (m_Parent != null && m_Parent.Container != null)
                m_Parent.Container.OnVBTypeChanged(m_Parent);
        }
        /// <summary>
        /// Whether the VariableType is unchangeable
        /// </summary>
        public bool LockVBType { get; set; } = false;
        /// <summary>
        /// Whether the CountType is unchangeable
        /// </summary>
        public bool LockCType { get; set; } = false;
        /// <summary>
        /// Whether the EnableType is unchangeable
        /// </summary>
        public bool LockEType { get { return eType == EnableType.ET_FIXED; } }
        public bool CanBeRemoved { get { return m_bCanbeRemoved; } set { m_bCanbeRemoved = value; } }
        public bool CanSwitchConst { get { return !LockVBType; } }
        public bool CanSwitchList { get { return !LockCType; } }
        public bool CanSwitchEnable { get { return eType == EnableType.ET_Enable || eType == EnableType.ET_Disable; } }
        public virtual bool CanSwitchContainer { get { return false; } }
        public bool ShouldHaveConnection { get { return eType != EnableType.ET_Disable && vbType == VariableType.VBT_Pointer && string.IsNullOrEmpty(Value); } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        /// <summary>
        /// Used to save to file
        /// </summary>
        public string ValueInXml {
            get
            {
                char _vtype = ValueTypeDic.GetValue(m_vType, INT);
                char _ctype = m_cType == CountType.CT_LIST ? _vtype : SINGLE;
                char _vbtype = GetVariableChar(m_vbType, CONST, IsLocal);
                char _eType = GetEnableChar(m_eType, NONE);
                StringBuilder sb = new StringBuilder();
                sb.Append(_ctype).Append(_vtype).Append(_vbtype);
                if (_eType != NONE) sb.Append(_eType);
                sb.Append(' ').Append(Value);

                if (m_bVectorIndexEnabled && m_VectorIndex != null)
                {
                    sb.Append(" VI ").Append(GetVariableChar(m_VectorIndex.vbType, CONST, m_VectorIndex.IsLocal)).Append(' ').Append(m_VectorIndex.Value);
                }
                return sb.ToString();
            }
        }

        public bool IsEditable
        {
            get { return !NetworkMgr.Instance.IsConnected; }
        }

        public void DebugStateChanged()
        {
            OnPropertyChanged("IsEditable");
        }

        public void OnCandidatesChange()
        {
            RefreshCandidates(true);
            _RefreshIndex();
            if (m_VectorIndex != null)
                m_VectorIndex.RefreshCandidates(true);
            _OnValueChanged();
            _OnConditionChanged();
        }

        bool m_IsRefreshed = false;
        /// <summary>
        /// Just to trigger UI
        /// </summary>
        public bool IsRefreshed
        {
            get { return m_IsRefreshed; }
            set
            {
                m_IsRefreshed = value;
                OnPropertyChanged("IsRefreshed");
            }
        }
        /// <summary>
        /// Whether a variable is referenced by other pins
        /// </summary>
        public enum ReferencedType
        {
            /// <summary>
            /// Invalid
            /// </summary>
            None,
            /// <summary>
            /// Is defined but never used
            /// </summary>
            Disactive,
            /// <summary>
            /// Is in use
            /// </summary>
            Active,
        }

        ReferencedType m_ReferencedType = ReferencedType.None;
        /// <summary>
        /// ReferencedType
        /// </summary>
        public ReferencedType referencedType
        {
            get { return m_ReferencedType; }
            set
            {
                if (m_ReferencedType != value)
                {
                    m_ReferencedType = value;
                    OnPropertyChanged("referencedType");
                }
            }
        }
        /// <summary>
        /// Change the ReferencedType
        /// </summary>
        /// <param name="t"></param>
        public void TrySetReferencedType(ReferencedType t)
        {
            if (m_ReferencedType < t)
            {
                m_ReferencedType = t;
                OnPropertyChanged("referencedType");
            }
        }

        public bool IsValid
        {
            get { return CheckValid(); }
        }

        public bool CheckValid()
        {
            if (SharedDataSource == null || SharedDataSource.SharedData == null)
                return false;
            if (eType == EnableType.ET_Disable)
                return true;
            if (vbType == VariableType.VBT_Pointer)
            {
                ///> If Value is null, it may have a connection to another variable
                if (!this.IsIndex && string.IsNullOrEmpty(Value))
                    return true;
                
                Variable other = SharedDataSource.SharedData.GetVariable(Value, IsLocal);
                if (other == null)
                {
                    LogMgr.Instance.Log(string.Format("Pointer doesnt exist for variable {0}, while the pointer is {1} ", DisplayName, DisplayValue));
                    return false;
                }

                return CheckValid(this, other);
            }

            if (cType == CountType.CT_LIST)
            {
                if (string.IsNullOrEmpty(Value))
                    return true;
                string[] ss = Value.Split(ListSpliter, StringSplitOptions.RemoveEmptyEntries);
                foreach(var s in ss)
                {
                    if (!CheckValidSingle(s))
                        return false;
                }
                return true;
            }

            return CheckValidSingle(Value);
        }

        public static bool CheckValid(Variable me, Variable other)
        {
            if (other.vType != me.vType)
            {
                LogMgr.Instance.Log(string.Format("Types dont match: {0}.{1} != {2}.{3}", me.DisplayName, me.vType, other.DisplayName, other.vType));
                return false;
            }

            if (other.cType != me.cType)
            {
                if (me.m_bVectorIndexEnabled)
                {
                    if (me.m_VectorIndex == null)
                    {
                        LogMgr.Instance.Log(string.Format("Types dont match: {0}.{1} != {2}.{3}", me.DisplayName, me.cType, other.DisplayName, other.cType));
                        return false;
                    }
                    else if (!me.m_VectorIndex.CheckValid())
                    {
                        LogMgr.Instance.Log(string.Format("VectorIndex invalid: {0}.Index == {1}", me.DisplayName, me.m_VectorIndex.DisplayValue));
                        return false;
                    }
                }
                else
                {
                    LogMgr.Instance.Log(string.Format("Types dont match: {0}.{1} != {2}.{3}", me.DisplayName, me.cType, other.DisplayName, other.cType));
                    return false;
                }
            }
            else if (other.cType == CountType.CT_SINGLE && me.m_bVectorIndexEnabled)
            {
                LogMgr.Instance.Log(string.Format("Single Variable with VectorIndex: {0} -> {1}", me.DisplayName, other.DisplayName));
                return false;
            }
            return true;
        }
        private bool CheckValidSingle(string v)
        {
            switch(vType)
            {
                case ValueType.VT_INT:
                    {
                        if (int.TryParse(v, out int a))
                            return true;
                        LogMgr.Instance.Log(string.Format("Variable parse error: int {0} == {1}", Name, v));
                    }
                    break;
                case ValueType.VT_FLOAT:
                    {
                        if (float.TryParse(v, out float a))
                            return true;
                        LogMgr.Instance.Log(string.Format("Variable parse error: float {0} == {1}", Name, v));
                    }
                    break;
                case ValueType.VT_BOOL:
                    {
                        if (v == "F" || v == "T")
                            return true;
                        //if (bool.TryParse(v, out bool a) && System.Text.RegularExpressions.Regex.IsMatch(v, "[a-z]"))
                        //    return true;
                        LogMgr.Instance.Log(string.Format("Variable parse error: bool {0} == {1}", Name, v));
                    }
                    break;
                case ValueType.VT_STRING:
                    {
                        if (v.Contains('|'))
                        {
                            LogMgr.Instance.Log(string.Format("Variable parse error: single string contains |, {0} == {1}", Name, v));
                            return false;
                        }
                        return true;
                    }
                case ValueType.VT_VECTOR3:
                    {
                        string[] ss = v.Split(Vector3Spliter);
                        if (ss.Length == 3)
                        {
                            foreach (string s in ss)
                            {
                                if (!float.TryParse(s, out float a))
                                {
                                    LogMgr.Instance.Log(string.Format("Variable parse error: Vector3 {0} == {1}", Name, v));
                                    return false;
                                }
                            }
                            return true;
                        }
                        LogMgr.Instance.Log(string.Format("Variable parse error: Vector3 {0} == {1}", Name, v));
                        return false;
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
                case ValueType.VT_ENTITY:
                    {
                        /// TODO
                        return true;
                    }
                case ValueType.VT_ULONG:
                    {
                        if (ulong.TryParse(v, out ulong a))
                            return true;
                        LogMgr.Instance.Log(string.Format("Variable parse error: ulong {0} == {1}", Name, v));
                    }
                    break;
                default:
                    return false;
            }

            return false;
        }
        /// <summary>
        /// Load from file
        /// </summary>
        /// <param name="s"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool SetVariableInNode(string s, string newName = null)
        {
            string[] ss = s.Split(SpaceSpliter);
            if (ss.Length == 0 || ss[0].Length < 3)
            {
                LogMgr.Instance.Error("Format error when set variable from node: " + s);
                return false;
            }
            if (newName != null)
                m_Name = newName;

            if(!_SetVariable(ss[0][1], ss[0][0], ss[0][2], ss[0].Length > 3 ? ss[0][3] : NONE, ss.Length >= 2 ? ss[1] : string.Empty))
                return false;

            if (ss.Length >= 5 && ss[2] == "VI")
            {
                SetVectorIndex(ss[3], ss[4]);
            }
            return true;
        }

        private bool SetVectorIndex(string variableType, string value)
        {
            m_bVectorIndexEnabled = true;
            m_VectorIndex = new Variable(SharedDataSource);
            m_VectorIndex.m_Parent = this;
            m_VectorIndex.SetVariable(
                ValueType.VT_INT, 
                CountType.CT_SINGLE, 
                GetVariableType(variableType[0],VariableType.VBT_NONE),
                EnableType.ET_FIXED,
                GetLocal(variableType[0]), 
                value,
                null,
                Name + ".Index");
            return true;
        }
        bool _SetVariable(char valueType, char countType, char variableType, char enableType, string value, string param = null)
        {
            m_bInited = false;
            m_vType = ValueTypeDic.GetKey(valueType, ValueType.VT_NONE);
            if (vType == ValueType.VT_NONE)
                return false;
            if (!m_vTypeSet.Contains(m_vType))
                m_vTypeSet.Add(m_vType);

            if (!LockVBType)
            {
                m_vbType = GetVariableType(variableType, VariableType.VBT_NONE);
                if (vbType == VariableType.VBT_NONE)
                    return false;
            }

            if (!LockCType)
            {
                m_cType = GetCountType(valueType, countType);
                if (cType == CountType.CT_NONE)
                    return false;
            }

            if (!LockEType)
            {
                var e = GetEnableType(enableType, EnableType.ET_NONE);
                if (e == EnableType.ET_NONE)
                    return false;
                ///> Enable/Disable cant convert to Fixed
                else if (eType == EnableType.ET_NONE || (e == EnableType.ET_Enable || e == EnableType.ET_Disable))
                    m_eType = e;
                ///> Not a default value, Fixed convert to Enable
                else if (m_Value != value)
                    m_eType = EnableType.ET_Enable;
            }

            m_bIsLocal = Char.IsLower(variableType);

            m_Value = value;
            _RefreshDisplayValue();

            if (param != null)
                m_Params = param;

            RefreshCandidates(true);
            return true;
        }
        /// <summary>
        /// Create from user
        /// </summary>
        /// <param name="vtype"></param>
        /// <param name="ctype"></param>
        /// <param name="vbtype"></param>
        /// <param name="etype"></param>
        /// <param name="isLocal"></param>
        /// <param name="value"></param>
        /// <param name="param"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool SetVariable(ValueType vtype, CountType ctype, VariableType vbtype, EnableType etype, bool isLocal, string value, string param = null, string newName = null)
        {
            m_bInited = false;
            if (newName != null)
                m_Name = newName;

            m_vType = vtype;
            if (vType == ValueType.VT_NONE)
                return false;

            m_cType = ctype;
            if (cType == CountType.CT_NONE)
            {
                m_cType = CountType.CT_SINGLE;
                LockCType = false;
            }
            else
            {
                LockCType = true;
            }

            m_vbType = vbtype;
            if (vbType == VariableType.VBT_NONE)
            {
                m_vbType = VariableType.VBT_Const;
                LockVBType = false;
            }
            else
            {
                LockVBType = true;
            }

            m_eType = etype;

            m_bIsLocal = isLocal;

            m_Value = value;
            _RefreshDisplayValue();

            if (param != null)
                m_Params = param;

            RefreshCandidates(true);

            return true;
        }

        public Variable Clone(IVariableDataSource newDataSource = null)
        {
            Variable v = Activator.CreateInstance(GetType(), newDataSource == null ? SharedDataSource : newDataSource) as Variable;
            //Variable v = new Variable(newDataSource == null ? SharedDataSource : newDataSource);
            v.vTypeSet.AddRange(vTypeSet.ToArray());
            v.vType = vType;
            v.cType = cType;
            v.vbType = vbType;
            v.eType = eType;
            v.m_Name = m_Name;
            v.m_Value = m_Value;
            v.m_bIsLocal = m_bIsLocal;
            v.m_DisplayValue = m_DisplayValue;
            v.LockVBType = LockVBType;
            v.LockCType = LockCType;
            v.m_bCanbeRemoved = m_bCanbeRemoved;
            v.m_bInited = m_bInited;
            v.m_Params = m_Params;
            v.Description = Description;
            v.IsInput = IsInput;
            if (m_VectorIndex != null && m_bVectorIndexEnabled)
            {
                v.m_VectorIndex = m_VectorIndex.Clone(newDataSource);
                v.m_VectorIndex.m_Parent = v;
                v.m_bVectorIndexEnabled = true;
            }
            return v;
        }
    }

}
