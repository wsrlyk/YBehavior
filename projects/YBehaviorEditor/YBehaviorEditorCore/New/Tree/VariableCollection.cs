using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// When in a group, changes of any pin in the group about the types will apply to other pins in the group
    /// </summary>
    public class SameTypeGroup : System.Collections.IEnumerable
    {
        Dictionary<int, HashSet<string>> m_Groups = new Dictionary<int, HashSet<string>>();
        /// <summary>
        /// Add to group
        /// </summary>
        /// <param name="key">Pin name</param>
        /// <param name="group">Group ID</param>
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
    /// <summary>
    /// A wrapper with an index,
    /// just for sorting to make the order in the collection unchanged
    /// </summary>
    public class VariableHolder
    {
        public Variable Variable { get; set; }
        public int Index;
    }
    /// <summary>
    /// Interface of collection
    /// </summary>
    public interface IVariableCollection
    {
        /// <summary>
        /// Collection of data
        /// </summary>
        DelayableNotificationCollection<VariableHolder> Datas { get; }
        /// <summary>
        /// Get by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Variable GetVariable(string name);
    }
    /// <summary>
    /// Interface of callbacks
    /// </summary>
    public interface IVariableCollectionOwner : IVariableDataSource
    {
        void OnVariableValueChanged(Variable v);
        void OnVariableVBTypeChanged(Variable v);
        void OnVariableETypeChanged(Variable v);
    }
    /// <summary>
    /// Collection of variables/pins
    /// </summary>
    public class VariableCollection: IVariableCollection
    {
        public event Action<Variable> valueChanged;
        public event Action<Variable> vbTypeChanged;
        public event Action<Variable> cTypeChanged;
        public event Action<Variable> vTypeChanged;
        public event Action<Variable> eTypeChanged;
        public void OnValueChanged(Variable v) => valueChanged?.Invoke(v);
        public void OnVBTypeChanged(Variable v) => vbTypeChanged?.Invoke(v);
        public void OnCTypeChanged(Variable v) => cTypeChanged?.Invoke(v);
        public void OnVTypeChanged(Variable v) => vTypeChanged?.Invoke(v);
        public void OnETypeChanged(Variable v) => eTypeChanged?.Invoke(v);

        protected Dictionary<string, VariableHolder> m_Variables = new Dictionary<string, VariableHolder>();
        protected DelayableNotificationCollection<VariableHolder> m_VariableList = new DelayableNotificationCollection<VariableHolder>();
        public DelayableNotificationCollection<VariableHolder> Datas { get { return m_VariableList; } }

        protected IVariableCollectionOwner m_Owner;
        public IVariableCollectionOwner Owner { get { return m_Owner; } }
        public VariableCollection(IVariableCollectionOwner owner)
        {
            m_Owner = owner;
            if (owner != null)
            {
                valueChanged += owner.OnVariableValueChanged;
                vbTypeChanged += owner.OnVariableVBTypeChanged;
                eTypeChanged += owner.OnVariableETypeChanged;
            }
        }

        public static bool IsValidVariableName(string name)
        {
            string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
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
                LogMgr.Instance.Error("(Only: a~z, A~Z, 0~9, _ ) and (Start with a~z, A~Z, _ ): " + name);
            return res;
        }

        public VariableHolder DoAddVariable(Variable v)
        {
            if (v == null)
                return null;
            if (m_Variables.ContainsKey(v.Name))
            {
                LogMgr.Instance.Error("Duplicated variable name: " + v.Name);
                return null;
            }

            VariableHolder holder = new VariableHolder()
            {
                Variable = v,
                Index = m_VariableList.Count
            };

            m_Variables[v.Name] = holder;
            m_VariableList.Add(holder);
            if (v.SharedDataSource != m_Owner)
            {
                v.SharedDataSource = m_Owner;
                v.RefreshCandidates(true);
            }
            v.Container = this;
            return holder;
        }

        public bool DoInsertVariable(VariableHolder holder)
        {
            if (holder == null)
                return false;
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

            return true;
        }

        public bool DoRemove(VariableHolder holder)
        {
            if (holder == null)
                return false;
            if (!m_Variables.ContainsKey(holder.Variable.Name))
            {
                LogMgr.Instance.Error("Cant find variable name: " + holder.Variable.Name);
                return false;
            }
            m_Variables.Remove(holder.Variable.Name);
            m_VariableList.RemoveAt(holder.Index);

            for (int i = holder.Index; i < m_VariableList.Count; ++i)
            {
                --m_VariableList[i].Index;
            }
            return true;
        }
        /// <summary>
        /// Get holder by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public VariableHolder GetVariableHolder(string name)
        {
            if (m_Variables.TryGetValue(name, out VariableHolder v))
                return v;

            return null;
        }
        public Variable GetVariable(string name)
        {
            return GetVariableHolder(name)?.Variable;
        }

        public void CloneFrom(VariableCollection other)
        {
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                using (var delay = m_VariableList.Delay())
                {
                    m_VariableList.Clear();
                    m_Variables.Clear();
                    foreach (VariableHolder v in other.m_VariableList)
                    {
                        Variable vv = v.Variable.Clone();
                        DoAddVariable(vv);
                    }
                }
            }
        }
        /// <summary>
        /// Try to make me the same with other
        /// </summary>
        /// <param name="other"></param>
        public void DiffReplaceBy(VariableCollection other)
        {
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                using (var delay = m_VariableList.Delay())
                {
                    List<VariableHolder> tempList = new List<VariableHolder>();

                    foreach (VariableHolder v in m_VariableList)
                    {
                        VariableHolder otherholder = other.GetVariableHolder(v.Variable.Name);
                        ///> Remove extra ones
                        if (otherholder == null)
                            continue;

                        bool bNeedRefresh = false;
                        if (v.Variable.cType != otherholder.Variable.cType)
                        {
                            v.Variable.cType = otherholder.Variable.cType;
                            bNeedRefresh = true;
                        }
                        if (v.Variable.vType != otherholder.Variable.vType)
                        {
                            v.Variable.vTypeSet.Clear();
                            v.Variable.vType = otherholder.Variable.vType;
                            bNeedRefresh = true;
                        }
                        if (bNeedRefresh)
                            v.Variable.RefreshCandidates(true);

                        tempList.Add(v);
                    }
                    ///> Add new ones
                    foreach (VariableHolder v in other.m_VariableList)
                    {
                        if (GetVariableHolder(v.Variable.Name) == null)
                        {
                            VariableHolder holder = new VariableHolder()
                            {
                                Variable = v.Variable.Clone(),
                                Index = tempList.Count
                            };

                            tempList.Add(holder);
                        }
                    }
                    ///> Keep the orders the same
                    foreach (VariableHolder v in tempList)
                    {
                        VariableHolder ov = other.GetVariableHolder(v.Variable.Name);
                        if (ov == null)
                        {
                            LogMgr.Instance.Error("Something is wrong with the list. It Should not happen.");
                            continue;
                        }
                        v.Index = ov.Index;
                    }
                    tempList.Sort(
                        (VariableHolder left, VariableHolder right) =>
                            {
                                return left.Index.CompareTo(right.Index);
                            }
                    );

                    ///> assign new list to the collection
                    m_VariableList.Clear();
                    m_Variables.Clear();
                    int index = 0;
                    foreach(var v in tempList)
                    {
                        m_VariableList.Add(v);
                        m_Variables[v.Variable.Name] = v;
                        v.Index = index++;
                        if (v.Variable.SharedDataSource != m_Owner)
                        {
                            v.Variable.SharedDataSource = m_Owner;
                            v.Variable.RefreshCandidates(true);
                        }
                        v.Variable.Container = this;
                    }
                }
            }
        }

    }
}
