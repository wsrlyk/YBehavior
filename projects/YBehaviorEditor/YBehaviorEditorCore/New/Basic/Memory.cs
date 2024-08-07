using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Shared or local variable of tree
    /// </summary>
    public class TreeVariable : Variable
    {
        public TreeVariable(IVariableDataSource source) : base(source)
        {

        }

        public override bool CanSwitchContainer => true;
    }
    /// <summary>
    /// Collection of shared and local variables
    /// </summary>
    public class TreeMemory : IVariableCollection
    {
        protected IVariableCollectionOwner m_Owner;

        VariableCollection m_SharedVariables;
        VariableCollection m_LocalVariables;
        DelayableNotificationCollection<VariableHolder> m_DataList = new DelayableNotificationCollection<VariableHolder>();
        /// <summary>
        /// All variables
        /// </summary>
        public DelayableNotificationCollection<VariableHolder> Datas { get { return m_DataList; } }
        public Variable GetVariable(string name) { return null; }
        /// <summary>
        /// Collection of shared variables
        /// </summary>
        public IVariableCollection SharedMemory { get { return m_SharedVariables; } }
        /// <summary>
        /// Collection of local variables
        /// </summary>
        public IVariableCollection LocalMemory { get { return m_LocalVariables; } }
        /// <summary>
        /// Candidates for each type
        /// </summary>
        public VariableCandidates Candidatas { get; } = new VariableCandidates();
        /// <summary>
        /// Refresh the candidates when variables change
        /// </summary>
        public void RefreshCandidatas()
        {
            Candidatas.Refresh(this);
            SharedVariableChangedArg arg = new SharedVariableChangedArg();
            EventMgr.Instance.Send(arg);
        }
        public TreeMemory(IVariableCollectionOwner owner)
        {
            m_Owner = owner;
            m_SharedVariables = new VariableCollection(owner);
            m_LocalVariables = new VariableCollection(owner);
        }
        /// <summary>
        /// Create a variable from file
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="value">Formatted value with types</param>
        /// <returns></returns>
        public bool TryAddData(string name, string value)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            TreeVariable v = new TreeVariable(m_Owner);
            v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
            if (!v.SetVariableInNode(value, name))
                return false;

            v.LockVBType = true;
            v.LockCType = false;
            v.CanBeRemoved = true;

            //if (!v.CheckValid())
            //    return false;

            return _AddVariable(v);
        }
        /// <summary>
        /// Create a variable from user
        /// </summary>
        /// <param name="name">Variable Name</param>
        /// <param name="value">Value</param>
        /// <param name="vType">ValueType</param>
        /// <param name="cType">CountType</param>
        /// <param name="isLocal">Local or shared</param>
        /// <returns></returns>
        public bool TryCreateVariable(string name, string value, Variable.ValueType vType, Variable.CountType cType, bool isLocal)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            Variable v;
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                v = new TreeVariable(m_Owner);
                v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
                if (!v.SetVariable(vType, cType, Variable.VariableType.VBT_Const, Variable.EnableType.ET_FIXED, isLocal, value, null, name))
                    return false;

                v.LockVBType = true;
                v.LockCType = false;
                v.CanBeRemoved = true;

                bool res = _AddVariable(v);
                if (!res)
                    return false;
            }

            {
                AddSharedVariableCommand addSharedVariableCommand = new AddSharedVariableCommand()
                {
                    VariableHolder = GetVariableHolder(v.Name, v.IsLocal)
                };
                WorkBenchMgr.Instance.PushCommand(addSharedVariableCommand);
            }
            return true;
        }

        bool _AddVariable(Variable v)
        {
            if (v == null)
                return false;

            if (Utility.ReservedAttributesAll.Contains(v.Name))
            {
                LogMgr.Instance.Error("Reserved name: " + v.Name);
                return false;
            }

            VariableHolder holder = null;
            if(v.IsLocal)
            {
                holder = m_LocalVariables.DoAddVariable(v);
            }
            else
            {
                holder = m_SharedVariables.DoAddVariable(v);
            }

            if (holder == null)
                return false;

            m_DataList.Add(holder);

            ///> TODO: make it better
            RefreshCandidatas();

            return true;
        }
        /// <summary>
        /// Add the variable from Undo/Redo
        /// </summary>
        /// <param name="holder"></param>
        /// <returns></returns>
        public bool AddBackVariable(VariableHolder holder)
        {
            if (holder.Variable.IsLocal)
            {
                if (!m_LocalVariables.DoInsertVariable(holder))
                    return false;
            }
            else
            {
                if (!m_SharedVariables.DoInsertVariable(holder))
                    return false;
            }

            m_DataList.Add(holder);

            ///> TODO: make it better
            RefreshCandidatas();

            return true;
        }
        /// <summary>
        /// Remove a variable
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool RemoveVariable(Variable v)
        {
            if (v == null)
                return false;

            VariableHolder holder = GetVariableHolder(v.Name, v.IsLocal);
            if (v.IsLocal)
            {
                if (!m_LocalVariables.DoRemove(holder))
                    return false;
            }
            else
            {
                if (!m_SharedVariables.DoRemove(holder))
                    return false;
            }

            m_DataList.Remove(holder);

            ///> TODO: make it better
            RefreshCandidatas();

            RemoveSharedVariableCommand removeSharedVariableCommand = new RemoveSharedVariableCommand()
            {
                VariableHolder = holder
            };
            WorkBenchMgr.Instance.PushCommand(removeSharedVariableCommand);

            return true;
        }
        /// <summary>
        /// Switch between local and shared collections
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool SwitchVariable(Variable v)
        {
            if (v == null)
                return false;

            VariableHolder holder = GetVariableHolder(v.Name, v.IsLocal);
            if (holder == null)
                return false;
            if (v.IsLocal)
            {
                if (!m_LocalVariables.DoRemove(holder))
                    return false;
            }
            else
            {
                if (!m_SharedVariables.DoRemove(holder))
                    return false;
            }
            m_DataList.Remove(holder);

            v.IsLocal = !v.IsLocal;
            if (v.IsLocal)
            {
                holder = m_LocalVariables.DoAddVariable(v);
            }
            else
            {
                holder = m_SharedVariables.DoAddVariable(v);
            }

            if (holder == null)
                return false;

            m_DataList.Add(holder);

            RefreshCandidatas();

            return true;
        }
        /// <summary>
        /// Get the holder of a variable by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bIsLocal"></param>
        /// <returns></returns>
        public VariableHolder GetVariableHolder(string name, bool bIsLocal)
        {
            VariableHolder holder = null;
            if (bIsLocal)
            {
                holder = m_LocalVariables.GetVariableHolder(name);
            }
            else
            {
                holder = m_SharedVariables.GetVariableHolder(name);
            }
            return holder;
        }
        /// <summary>
        /// Get the variable by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bIsLocal"></param>
        /// <returns></returns>
        public Variable GetVariable(string name, bool bIsLocal)
        {
            return GetVariableHolder(name, bIsLocal)?.Variable;
        }
        /// <summary>
        /// Clone this to other. 
        /// For debugging.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public TreeMemory Clone(IVariableCollectionOwner owner = null)
        {
            TreeMemory memory = new TreeMemory(owner ?? m_Owner);

            memory.m_LocalVariables.CloneFrom(m_LocalVariables);
            memory.m_SharedVariables.CloneFrom(m_SharedVariables);

            foreach (var v in m_DataList)
                memory.m_DataList.Add(v);
            return memory;
        }
    }
    /// <summary>
    /// Input and output pin
    /// </summary>
    public class InOutVariable : Variable
    {
        public InOutVariable(IVariableDataSource source): base(source)
        {

        }
    }
    /// <summary>
    /// Collection of input and output pins
    /// </summary>
    public class InOutMemory
    {
        protected IVariableCollectionOwner m_Owner;
        bool m_bIsCore = true;
        VariableCollection m_InputVariables;
        VariableCollection m_OutputVariables;
        /// <summary>
        /// Input collection
        /// </summary>
        public IVariableCollection InputMemory { get { return m_InputVariables; } }
        /// <summary>
        /// Output collection
        /// </summary>
        public IVariableCollection OutputMemory { get { return m_OutputVariables; } }

        public InOutMemory(IVariableCollectionOwner owner, bool bIsCore)
        {
            m_Owner = owner;
            m_bIsCore = bIsCore;
            m_InputVariables = new VariableCollection(owner);
            m_OutputVariables = new VariableCollection(owner);
        }
        /// <summary>
        /// Try to create pin from file
        /// </summary>
        /// <param name="name">Pin name</param>
        /// <param name="value">Formatted value with types</param>
        /// <param name="bIsInput">Input or output</param>
        /// <returns></returns>
        public bool TryAddData(string name, string value, bool bIsInput)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            InOutVariable v = new InOutVariable(m_Owner);
            if (m_bIsCore)
                v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
            if (!v.SetVariableInNode(value, name))
                return false;

            if ((bIsInput && m_bIsCore) || (!bIsInput && !m_bIsCore))
            {
                ///> In these two situations, variable can only be pointer
                if (v.vbType != Variable.VariableType.VBT_Pointer)
                    v.vbType = Variable.VariableType.VBT_Pointer;
                v.LockVBType = true;
            }
            else
            {
                v.LockVBType = false;
            }

            if (m_bIsCore)
                v.LockCType = false;
            else
                v.LockCType = true;

            v.CanBeRemoved = true;
            v.IsInput = bIsInput;

            if (m_bIsCore)
            {
                if (!v.CheckValid())
                {
                    LogMgr.Instance.Error("Format error when TryAddData " + name + " " + value);
                    //return false;
                }
            }

            return _AddVariable(v);
        }
        /// <summary>
        /// Try to create pin from user
        /// </summary>
        /// <param name="name">Pin name</param>
        /// <param name="vType">ValueType</param>
        /// <param name="cType">CountType</param>
        /// <param name="bIsInput">Input or output</param>
        /// <returns></returns>
        public bool TryCreateVariable(string name, Variable.ValueType vType, Variable.CountType cType, bool bIsInput)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            InOutVariable v;
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                v = new InOutVariable(m_Owner);
                if (m_bIsCore)
                    v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
                if (!v.SetVariable(vType, cType, Variable.VariableType.VBT_Pointer, Variable.EnableType.ET_FIXED, false, "", null, name))
                    return false;

                if ((bIsInput && m_bIsCore) || (!bIsInput && !m_bIsCore))
                    v.LockVBType = true;
                else
                    v.LockVBType = false;
                if (m_bIsCore)
                    v.LockCType = false;
                else
                    v.LockCType = true;

                v.CanBeRemoved = true;
                v.IsInput = bIsInput;

                bool res = _AddVariable(v);
                if (!res)
                    return false;
            }

            {
                AddInOutVariableCommand addInOutVariableCommand = new AddInOutVariableCommand()
                {
                    VariableHolder = _GetVariableHolder(v.Name, v.IsInput)
                };
                WorkBenchMgr.Instance.PushCommand(addInOutVariableCommand);
            }
            return true;
        }

        protected bool _AddVariable(InOutVariable v)
        {
            if (v == null)
                return false;

            if (Utility.ReservedAttributesAll.Contains(v.Name))
            {
                LogMgr.Instance.Error("Reserved name: " + v.Name);
                return false;
            }

            VariableHolder holder = null;
            if (v.IsInput)
            {
                holder = m_InputVariables.DoAddVariable(v);
            }
            else
            {
                holder = m_OutputVariables.DoAddVariable(v);
            }

            if (holder == null)
                return false;

            return true;
        }

        //public bool AddBackVariable(VariableHolder holder)
        //{
        //    if (!(holder.Variable is InOutVariable))
        //        return false;
        //    InOutVariable v = holder.Variable as InOutVariable;
        //    if (v.IsInput)
        //    {
        //        if (!m_InputVariables.DoInsertVariable(holder))
        //            return false;
        //    }
        //    else
        //    {
        //        if (!m_OutputVariables.DoInsertVariable(holder))
        //            return false;
        //    }

        //    return true;
        //}

        /// <summary>
        /// Remove a pin
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool RemoveVariable(Variable v)
        {
            if (v == null || !(v is InOutVariable))
                return false;
            InOutVariable vv = v as InOutVariable;
            VariableHolder holder = _GetVariableHolder(v.Name, vv.IsInput);
            if (vv.IsInput)
            {
                if (!m_InputVariables.DoRemove(holder))
                    return false;
            }
            else
            {
                if (!m_OutputVariables.DoRemove(holder))
                    return false;
            }

            RemoveInOutVariableCommand removeInOutVariableCommand = new RemoveInOutVariableCommand()
            {
                VariableHolder = holder
            };
            WorkBenchMgr.Instance.PushCommand(removeInOutVariableCommand);

            return true;
        }

        VariableHolder _GetVariableHolder(string name, bool bIsInput)
        {
            VariableHolder holder = null;
            if (bIsInput)
            {
                holder = m_InputVariables.GetVariableHolder(name);
            }
            else
            {
                holder = m_OutputVariables.GetVariableHolder(name);
            }
            return holder;
        }

        //public Variable GetVariable(string name, bool bIsInput)
        //{
        //    return _GetVariableHolder(name, bIsInput)?.Variable;
        //}

        /// <summary>
        /// Refresh candidates
        /// </summary>
        public void RefreshVariables()
        {
            _RefreshVariables(m_InputVariables);
            _RefreshVariables(m_OutputVariables);
        }
        void _RefreshVariables(VariableCollection variables)
        {
            foreach (var v in variables.Datas)
            {
                v.Variable.RefreshCandidates(true);
                if (v.Variable.VectorIndex != null)
                    v.Variable.VectorIndex.RefreshCandidates(true);
            }
        }
        //public InOutMemory Clone(IVariableCollectionOwner owner = null)
        //{
        //    InOutMemory memory = new InOutMemory(owner ?? m_Owner, m_bIsCore);

        //    memory.m_InputVariables.CloneFrom(m_InputVariables);
        //    memory.m_OutputVariables.CloneFrom(m_OutputVariables);

        //    return memory;
        //}

        /// <summary>
        /// Clone from others
        /// </summary>
        /// <param name="other"></param>
        public void CloneFrom(InOutMemory other)
        {
            m_InputVariables.CloneFrom(other.m_InputVariables);
            m_OutputVariables.CloneFrom(other.m_OutputVariables);
        }

        /// <summary>
        /// If do have same pin, do nothing to it;
        /// If dont have a pin, add it;
        /// If have an extra pin, remove it;
        /// </summary>
        /// <param name="other"></param>
        public void DiffReplaceBy(InOutMemory other)
        {
            m_InputVariables.DiffReplaceBy(other.m_InputVariables);
            m_OutputVariables.DiffReplaceBy(other.m_OutputVariables);
        }
    }

    //interface ISameTypeGroupTypeChanged
    //{
    //    void OnVTypeChanged(string name);
    //    void OnCTypeChanged(string name);
    //}

    /// <summary>
    /// Collection of node pins
    /// </summary>
    public class NodeMemory : VariableCollection, IVariableCollection/*, ISameTypeGroupTypeChanged*/
    {
        public SameTypeGroup vTypeGroup { get; set; } = null;
        public SameTypeGroup cTypeGroup { get; set; } = null;

        public NodeMemory(IVariableCollectionOwner owner) : base(owner)
        {
            vTypeChanged += _OnVTypeChanged;
            cTypeChanged += _OnCTypeChanged;
        }

        /// <summary>
        /// Add a pin
        /// </summary>
        /// <param name="v"></param>
        /// <param name="name">New name</param>
        /// <param name="defaultValue"></param>
        /// <param name="valueType"></param>
        /// <param name="countType"></param>
        /// <param name="vbType"></param>
        /// <param name="eType"></param>
        /// <param name="vTypeGroup">ValueType group</param>
        /// <param name="cTypeGroup">CountType group</param>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool AddVariable(
            Variable v,
            string name,
            string defaultValue,
            Variable.ValueType[] valueType,
            Variable.CountType countType,
            Variable.VariableType vbType,
            Variable.EnableType eType,
            int vTypeGroup = 0,
            int cTypeGroup = 0,
            string param = null)
        {
            v.vTypeSet.AddRange(valueType);

            v.SetVariable(valueType[0], countType, vbType, eType, false, defaultValue, param, name);

            if (_AddVariable(v, vTypeGroup, cTypeGroup))
                return true;
            return false;
        }
        /// <summary>
        /// Create a pin
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <param name="valueType"></param>
        /// <param name="countType"></param>
        /// <param name="vbType"></param>
        /// <param name="eType"></param>
        /// <param name="vTypeGroup">ValueType group</param>
        /// <param name="cTypeGroup">CountType group</param>
        /// <param name="param"></param>
        /// <returns></returns>
        public Variable CreateVariable(
            string name,
            string defaultValue,
            Variable.ValueType[] valueType,
            Variable.CountType countType,
            Variable.VariableType vbType,
            Variable.EnableType eType,
            int vTypeGroup = 0,
            int cTypeGroup = 0,
            string param = null)
        {
            Variable v = new Variable(m_Owner);
            if (AddVariable(v, name, defaultValue, valueType, countType, vbType, eType, vTypeGroup, cTypeGroup, param))
                return v;
            return null;
        }

        bool _AddVariable(Variable v, int vTypeGroup = 0, int cTypeGroup = 0)
        {
            if (v == null)
                return false;

            if (Utility.ReservedAttributesAll.Contains(v.Name))
            {
                LogMgr.Instance.Error("Reserved name: " + v.Name);
                return false;
            }

            if (DoAddVariable(v) == null)
                return false;

            if (vTypeGroup != 0)
            {
                if (this.vTypeGroup == null)
                    this.vTypeGroup = new SameTypeGroup();
                this.vTypeGroup.Add(v.Name, vTypeGroup);
            }

            if (cTypeGroup != 0)
            {
                if (this.cTypeGroup == null)
                    this.cTypeGroup = new SameTypeGroup();
                this.cTypeGroup.Add(v.Name, cTypeGroup);
            }

            return true;
        }
        /// <summary>
        /// Called when candidates change
        /// </summary>
        public void RefreshVariables()
        {
            foreach (var v in m_Variables.Values)
            {
                v.Variable.OnCandidatesChange();
            }
        }
        /// <summary>
        /// Clone from other
        /// </summary>
        /// <param name="other"></param>
        public void CloneFrom(NodeMemory other)
        {
            base.CloneFrom(other);
            if (other.vTypeGroup != null)
                vTypeGroup = other.vTypeGroup.Clone();
            if (other.cTypeGroup != null)
                cTypeGroup = other.cTypeGroup.Clone();
        }

        bool m_bProcessingSameTypeGroup = false;
        void _OnVTypeChanged(Variable v)
        {
            if (m_bProcessingSameTypeGroup || v == null)
                return;
            string name = v.Name;
            m_bProcessingSameTypeGroup = true;
            if (vTypeGroup != null)
            {
                bool bFound = false;
                foreach (HashSet<string> group in vTypeGroup)
                {
                    foreach (string n in group)
                    {
                        if (n == name)
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (!bFound)
                        continue;

                    //Variable v = GetVariable(name);
                    //if (v != null)
                    {
                        Variable.ValueType targetVType = v.vType;
                        foreach (string n in group)
                        {
                            if (n == name)
                                continue;
                            Variable vv = GetVariable(n);
                            if (vv != null)
                                vv.vType = targetVType;
                        }
                    }
                    break;
                }
            }
            m_bProcessingSameTypeGroup = false;
        }
        void _OnCTypeChanged(Variable v)
        {
            if (m_bProcessingSameTypeGroup || v == null)
                return;
            string name = v.Name;
            m_bProcessingSameTypeGroup = true;
            if (cTypeGroup != null)
            {
                bool bFound = false;
                foreach (HashSet<string> group in cTypeGroup)
                {
                    foreach (string n in group)
                    {
                        if (n == name)
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (!bFound)
                        continue;

                    //Variable v = GetVariable(name);
                    //if (v != null)
                    {
                        Variable.CountType targetCType = v.cType;
                        foreach (string n in group)
                        {
                            if (n == name)
                                continue;
                            Variable vv = GetVariable(n);
                            if (vv != null)
                                vv.cType = targetCType;
                        }
                    }
                    break;
                }
            }
            m_bProcessingSameTypeGroup = false;
        }
    }
}
