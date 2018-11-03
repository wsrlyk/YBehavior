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

    public interface IVariableCollection
    {
        DelayableNotificationCollection<VariableHolder> Datas { get; }
        Variable GetVariable(string name);
    }

    public class VariableCollection: IVariableCollection
    {
        protected Dictionary<string, VariableHolder> m_Variables = new Dictionary<string, VariableHolder>();
        protected DelayableNotificationCollection<VariableHolder> m_VariableList = new DelayableNotificationCollection<VariableHolder>();
        public DelayableNotificationCollection<VariableHolder> Datas { get { return m_VariableList; } }

        protected Node m_Owner;
        public VariableCollection(Node owner)
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

        public void OnVariableValueChanged(Variable v)
        {
            if (m_Owner != null)
                m_Owner.OnPropertyChanged("Note");
        }

        public void CloneTo(VariableCollection other)
        {
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                using (var delay = other.m_VariableList.Delay())
                {
                    other.m_VariableList.Clear();
                    other.m_Variables.Clear();
                    foreach (VariableHolder v in m_VariableList)
                    {
                        Variable vv = v.Variable.Clone();
                        other.DoAddVariable(vv);
                    }
                }
            }
        }

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

                        ///> We just clone the new variable
                        VariableHolder holder = new VariableHolder()
                        {
                            Variable = otherholder.Variable.Clone(),
                            Index = tempList.Count
                        };
                        ///> Keep the original value
                        if (holder.Variable.Value != v.Variable.Value)
                            holder.Variable.Value = v.Variable.Value;
                        tempList.Add(holder);
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
                            LogMgr.Instance.Error("Something is wrong with the list. It Shouldnt happen.");
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
                    foreach(var v in tempList)
                    {
                        m_VariableList.Add(v);
                        m_Variables[v.Variable.Name] = v;
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
