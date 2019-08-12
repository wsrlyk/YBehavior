﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class TreeVariable : Variable
    {
        public TreeVariable(IVariableDataSource source) : base(source)
        {

        }
    }

    public class TreeMemory : IVariableCollection
    {
        protected TreeNode m_Owner;

        VariableCollection m_SharedVariables;
        VariableCollection m_LocalVariables;
        DelayableNotificationCollection<VariableHolder> m_DataList = new DelayableNotificationCollection<VariableHolder>();
        public DelayableNotificationCollection<VariableHolder> Datas { get { return m_DataList; } }
        public Variable GetVariable(string name) { return null; }
        public IVariableCollection SharedMemory { get { return m_SharedVariables; } }
        public IVariableCollection LocalMemory { get { return m_LocalVariables; } }

        public TreeMemory(TreeNode owner)
        {
            m_Owner = owner;
            m_SharedVariables = new VariableCollection(owner);
            m_LocalVariables = new VariableCollection(owner);
        }

        public bool TryAddData(string name, string value)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            TreeVariable v = new TreeVariable(m_Owner.Tree);
            v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
            if (!v.SetVariableInNode(value, name))
                return false;

            v.LockVBType = true;
            v.LockCType = false;
            v.CanBeRemoved = true;

            //if (!v.CheckValid())
            //    return false;

            return AddVariable(v);
        }

        public bool TryCreateVariable(string name, string value, Variable.ValueType vType, Variable.CountType cType, bool isLocal)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            Variable v;
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                v = new TreeVariable(m_Owner.Tree);
                v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
                if (!v.SetVariable(vType, cType, Variable.VariableType.VBT_Const, isLocal, value, null, name))
                    return false;

                v.LockVBType = true;
                v.LockCType = false;
                v.CanBeRemoved = true;

                bool res = AddVariable(v);
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

        public bool AddVariable(Variable v)
        {
            if (v == null)
                return false;

            if (Node.ReservedAttributesAll.Contains(v.Name))
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

            SharedVariableChangedArg arg = new SharedVariableChangedArg();
            EventMgr.Instance.Send(arg);

            return true;
        }

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

            SharedVariableChangedArg arg = new SharedVariableChangedArg();
            EventMgr.Instance.Send(arg);

            return true;
        }

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

            {
                SharedVariableChangedArg arg = new SharedVariableChangedArg();
                EventMgr.Instance.Send(arg);
            }

            RemoveSharedVariableCommand removeSharedVariableCommand = new RemoveSharedVariableCommand()
            {
                VariableHolder = holder
            };
            WorkBenchMgr.Instance.PushCommand(removeSharedVariableCommand);

            return true;
        }

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

        public Variable GetVariable(string name, bool bIsLocal)
        {
            return GetVariableHolder(name, bIsLocal)?.Variable;
        }

        public TreeMemory Clone(TreeNode owner = null)
        {
            TreeMemory memory = new TreeMemory(owner ?? m_Owner);

            memory.m_LocalVariables.CloneFrom(m_LocalVariables);
            memory.m_SharedVariables.CloneFrom(m_SharedVariables);

            foreach (var v in m_DataList)
                memory.m_DataList.Add(v);
            return memory;
        }
    }

    public class InOutVariable : Variable
    {
        public InOutVariable(IVariableDataSource source): base(source)
        {

        }

        public bool IsInput { get; set; }
    }
    public class InOutMemory
    {
        protected TreeNode m_Owner;
        bool m_bIsCore = true;
        VariableCollection m_InputVariables;
        VariableCollection m_OutputVariables;

        public IVariableCollection InputMemory { get { return m_InputVariables; } }
        public IVariableCollection OutputMemory { get { return m_OutputVariables; } }

        public InOutMemory(TreeNode owner, bool bIsCore)
        {
            m_Owner = owner;
            m_bIsCore = bIsCore;
            m_InputVariables = new VariableCollection(owner);
            m_OutputVariables = new VariableCollection(owner);
        }

        public bool TryAddData(string name, string value, bool bIsInput)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            InOutVariable v = new InOutVariable(m_Owner.Tree);
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

            return AddVariable(v);
        }

        public bool TryCreateVariable(string name, Variable.ValueType vType, Variable.CountType cType, bool bIsInput)
        {
            if (!VariableCollection.IsValidVariableName(name))
                return false;
            InOutVariable v;
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                v = new InOutVariable(m_Owner.Tree);
                if (m_bIsCore)
                    v.vTypeSet.AddRange(Variable.CreateParams_AllTypes);
                if (!v.SetVariable(vType, cType, Variable.VariableType.VBT_Pointer, false, "", null, name))
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

                bool res = AddVariable(v);
                if (!res)
                    return false;
            }

            {
                AddInOutVariableCommand addInOutVariableCommand = new AddInOutVariableCommand()
                {
                    VariableHolder = GetVariableHolder(v.Name, v.IsInput)
                };
                WorkBenchMgr.Instance.PushCommand(addInOutVariableCommand);
            }
            return true;
        }

        public bool AddVariable(InOutVariable v)
        {
            if (v == null)
                return false;

            if (Node.ReservedAttributesAll.Contains(v.Name))
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

        public bool AddBackVariable(VariableHolder holder)
        {
            if (!(holder.Variable is InOutVariable))
                return false;
            InOutVariable v = holder.Variable as InOutVariable;
            if (v.IsInput)
            {
                if (!m_InputVariables.DoInsertVariable(holder))
                    return false;
            }
            else
            {
                if (!m_OutputVariables.DoInsertVariable(holder))
                    return false;
            }

            return true;
        }

        public bool RemoveVariable(Variable v)
        {
            if (v == null || !(v is InOutVariable))
                return false;
            InOutVariable vv = v as InOutVariable;
            VariableHolder holder = GetVariableHolder(v.Name, vv.IsInput);
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

        public VariableHolder GetVariableHolder(string name, bool bIsInput)
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

        public Variable GetVariable(string name, bool bIsInput)
        {
            return GetVariableHolder(name, bIsInput)?.Variable;
        }

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
        public InOutMemory Clone(TreeNode owner = null)
        {
            InOutMemory memory = new InOutMemory(owner ?? m_Owner, m_bIsCore);

            memory.m_InputVariables.CloneFrom(m_InputVariables);
            memory.m_OutputVariables.CloneFrom(m_OutputVariables);

            return memory;
        }

        public void CloneFrom(InOutMemory other)
        {
            m_InputVariables.CloneFrom(other.m_InputVariables);
            m_OutputVariables.CloneFrom(other.m_OutputVariables);
        }

        /// <summary>
        /// If do have same variable, do nothing to it;
        /// If dont have a variable, add it;
        /// If have an extra variable, remove it;
        /// </summary>
        /// <param name="other"></param>
        public void DiffReplaceBy(InOutMemory other)
        {
            m_InputVariables.DiffReplaceBy(other.m_InputVariables);
            m_OutputVariables.DiffReplaceBy(other.m_OutputVariables);
        }
    }

    public class NodeMemory : VariableCollection, IVariableCollection
    {
        public SameTypeGroup SameTypeGroup { get; set; } = null;

        public NodeMemory(TreeNode owner) : base(owner)
        {

        }

        public Variable CreateVariable(
            string name,
            string defaultValue,
            Variable.ValueType[] valueType,
            Variable.CountType countType,
            Variable.VariableType vbType,
            int typeGroup = 0,
            string param = null)
        {
            Variable v = new Variable(m_Owner.Tree);
            v.vTypeSet.AddRange(valueType);

            v.SetVariable(valueType[0], countType, vbType, false, defaultValue, param, name);

            if (AddVariable(v, typeGroup))
                return v;
            return null;
        }

        public bool AddVariable(Variable v, int sameTypeGroup = 0)
        {
            if (v == null)
                return false;

            if (Node.ReservedAttributesAll.Contains(v.Name))
            {
                LogMgr.Instance.Error("Reserved name: " + v.Name);
                return false;
            }

            if (DoAddVariable(v) == null)
                return false;

            if (sameTypeGroup != 0)
            {
                if (SameTypeGroup == null)
                    SameTypeGroup = new SameTypeGroup();
                SameTypeGroup.Add(v.Name, sameTypeGroup);
            }

            return true;
        }

        public void RefreshVariables()
        {
            foreach (var v in m_Variables.Values)
            {
                v.Variable.OnCandidatesChange();
            }
        }

        public void CloneFrom(NodeMemory other)
        {
            base.CloneFrom(other);
            if (other.SameTypeGroup != null)
                SameTypeGroup = other.SameTypeGroup.Clone();
        }
    }
}
