using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class SameTypeGroup : System.Collections.IEnumerable
    {
        Dictionary<int, HashSet<string>> m_Groups = new Dictionary<int, HashSet<string>>();

        public void Add(string key, int group)
        {
            HashSet<string> groupSet;
            if (!m_Groups.TryGetValue(group, out groupSet))
            {
                groupSet = new HashSet<string>();
                m_Groups[group] = groupSet;
            }

            groupSet.Add(key);
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return m_Groups.Values.GetEnumerator();
        }

        public SameTypeGroup Clone()
        {
            SameTypeGroup other = new SameTypeGroup();
            
            foreach (var keypair in m_Groups)
            {
                HashSet<string> groupSet = new HashSet<string>();

                foreach (var groups in keypair.Value)
                {
                    groupSet.Add(groups);
                }

                other.m_Groups[keypair.Key] = groupSet;
            }

            return other;
        }
    }

    public class VariableHolder
    {
        public Variable Variable { get; set; }
        public int Index;
    }
    public class SharedData
    {
        ObservableDictionary<string, VariableHolder> m_Variables = new ObservableDictionary<string, VariableHolder>();
        DelayableNotificationCollection<VariableHolder> m_VariableList = new DelayableNotificationCollection<VariableHolder>();

        Node m_Owner;
        public SameTypeGroup SameTypeGroup { get; set; } = null;

        public SharedData(Node owner)
        {
            m_Owner = owner;
        }

        public static bool IsValidVariableName(string name)
        {
            string pattern = @"^[a-zA-Z0-9_]*$";
            bool res = false;
            if (name.Length > 0 && name.Length <= 20)
            {
                res = (System.Text.RegularExpressions.Regex.IsMatch(name, pattern));
            }
            else
            {
                LogMgr.Instance.Error("Name Length (Only: 1~20): " + name);
                return false;
            }
            if (!res)
                LogMgr.Instance.Error("Contains invalid characters (Only: a~z, A~Z, 0~9, _ ): " + name);
            return res;
        }

        /// <summary>
        /// This function is only for the variables of the whole tree, not for a node.
        /// Add From xml
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAddData(string name, string value)
        {
            if (!IsValidVariableName(name))
                return false;
            Variable v = new Variable(this.m_Owner);
            if (!v.SetVariableInNode(value, name))
                return false;

            v.LockVBType = true;
            v.CanBeRemoved = true;

            if (!v.CheckValid())
                return false;

            return AddVariable(v);
        }
        /// <summary>
        /// This function is only for the variables of the whole tree, not for a node
        /// Create from Editor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="vType"></param>
        /// <param name="cType"></param>
        /// <returns></returns>
        public bool TryCreateVariable(string name, string value, Variable.ValueType vType, Variable.CountType cType)
        {
            if (!IsValidVariableName(name))
                return false;
            Variable v;
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                v = new Variable(this.m_Owner);
                if (!v.SetVariable(vType, cType, Variable.VariableType.VBT_Const, value, null, name))
                    return false;

                v.LockVBType = true;
                v.CanBeRemoved = true;

                if (!v.CheckValid())
                    return false;

                bool res = AddVariable(v);
                if (!res)
                    return false;
            }

            {
                AddSharedVariableCommand addSharedVariableCommand = new AddSharedVariableCommand()
                {
                    VariableHolder = m_Variables[v.Name]
                };
                WorkBenchMgr.Instance.PushCommand(addSharedVariableCommand);
            }
            return true;
        }

        public Variable CreateVariableInNode(
            string name, 
            string defaultValue, 
            Variable.ValueType[] valueType, 
            Variable.CountType countType, 
            Variable.VariableType vbType, 
            bool bLockVBType,
            int typeGroup = 0,
            string param = null)
        {
            Variable v = new Variable(m_Owner);
            v.vTypeSet.AddRange(valueType);
            v.cType = countType;
            v.vbType = vbType;
            v.SetVariable(valueType[0], countType, vbType, defaultValue, param, name);
            v.LockVBType = bLockVBType;

            if (AddVariable(v, typeGroup))
                return v;
            return null;
        }

        private VariableHolder _AddVariable(Variable v)
        {
            VariableHolder holder = new VariableHolder()
            {
                Variable = v,
                Index = m_VariableList.Count
            };
            m_Variables[v.Name] = holder;
            m_VariableList.Add(holder);
            return holder;
        }
        public bool AddVariable(Variable v, int sameTypeGroup = 0)
        {
            if (v == null)
                return false;

            if (m_Variables.ContainsKey(v.Name))
            {
                LogMgr.Instance.Error("Duplicated variable name: " + v.Name);
                return false;
            }
            if (Node.ReservedAttributesAll.Contains(v.Name))
            {
                LogMgr.Instance.Error("Reserved name: " + v.Name);
                return false;
            }
            _AddVariable(v);
            v.Container = this;
            
            if (sameTypeGroup != 0)
            {
                if (SameTypeGroup == null)
                    SameTypeGroup = new SameTypeGroup();
                SameTypeGroup.Add(v.Name, sameTypeGroup);
            }

            if (m_Owner is Tree)
            {
                SharedVariableChangedArg arg = new SharedVariableChangedArg();
                EventMgr.Instance.Send(arg);
            }
            return true;
        }

        public bool AddBackVariable(VariableHolder holder)
        {
            if (m_Variables.ContainsKey(holder.Variable.Name))
            {
                LogMgr.Instance.Error("Duplicated variable name: " + holder.Variable.Name);
                return false;
            }

            m_Variables[holder.Variable.Name] = holder;
            m_VariableList.Insert(holder.Index, holder);

            for (int i = holder.Index + 1; i < m_VariableList.Count; ++i)
            {
                ++m_VariableList[i].Index;
            }

            SharedVariableChangedArg arg = new SharedVariableChangedArg();
            EventMgr.Instance.Send(arg);

            return true;
        }
        private bool _Remove(VariableHolder holder)
        {
            m_Variables.Remove(holder.Variable.Name);
            m_VariableList.RemoveAt(holder.Index);

            for (int i = holder.Index; i < m_VariableList.Count; ++i)
            {
                --m_VariableList[i].Index;
            }
            return true;
        }
        /// <summary>
        /// Only for the variables for the whole tree
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool RemoveVariable(Variable v)
        {
            if (v == null)
                return false;

            if (!m_Variables.TryGetValue(v.Name, out VariableHolder holder) || !_Remove(holder))
            {
                LogMgr.Instance.Error("Variable not exist when try remove: " + v.Name);
                return false;
            }

            if (m_Owner is Tree)
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
        public Variable GetVariable(string name)
        {
            m_Variables.TryGetValue(name, out VariableHolder v);
            return v?.Variable;
        }

        public DelayableNotificationCollection<VariableHolder> Datas { get { return m_VariableList; } }

        public void OnVariableValueChanged(Variable v)
        {
            if (m_Owner != null)
                m_Owner.OnPropertyChanged("Note");
        }

        public void RefreshVariables()
        {
            foreach (var v in m_Variables.Values)
            {
                v.Variable.RefreshCandidates(true);
                if (v.Variable.VectorIndex != null)
                    v.Variable.VectorIndex.RefreshCandidates(true);
            }
        }

        public SharedData Clone(Node owner = null)
        {
            SharedData sharedData = new SharedData(owner ?? m_Owner);
            CloneTo(sharedData);
            return sharedData;
        }

        public void CloneTo(SharedData other)
        {
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                foreach (VariableHolder v in m_VariableList)
                {
                    Variable vv = v.Variable.Clone();
                    other.AddVariable(vv);
                }

                if (SameTypeGroup != null)
                    other.SameTypeGroup = this.SameTypeGroup.Clone();
            }
        }
    }
}
