using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class VariableCandidates
    {
        public struct Key
        {
            public Variable.ValueType vType;
            public Variable.CountType cType;
        }

        public struct Candidate : IComparable<Candidate>, IEquatable<Candidate>
        {
            public Variable variable { get; set; }

            public int CompareTo(Candidate other)
            {
                if (variable == null || other.variable == null)
                {
                    if (variable == null && other.variable == null)
                        return 0;
                    if (variable == null)
                        return -1;
                    return 1;
                }
                if (variable.IsLocal != other.variable.IsLocal)
                {
                    return variable.IsLocal ? -1 : 1;
                }

                //if (a.variable.cType != b.variable.cType)
                //{
                //    return a.variable.cType == Variable.CountType.CT_SINGLE ? -1 : 1;
                //}

                return variable.Name.CompareTo(other.variable.Name);
            }

            public bool Equals(Candidate other)
            {
                return variable == other.variable;
            }

        }

        public class Candidates
        {
            public Key key;

            public DelayableNotificationCollection<Candidate> variables { get; } = new DelayableNotificationCollection<Candidate>();

            DelayableNotificationCollection<Candidate>.DelayHandler m_DelayHandler;
            public void StartRefresh()
            {
                m_DelayHandler = variables.Delay();
            }

            public void EndRefresh()
            {
                m_DelayHandler.Dispose();
            }

            public void Add(Variable v)
            {
                Candidate candidate = new Candidate();
                candidate.variable = v;
                variables.Add(candidate);
            }
        }

        public static bool IsNeedIndex(Candidates candidates, string displayName)
        {
            if (candidates == null || displayName == null)
                return false;
            foreach(var v in candidates.variables)
            {
                if (v.variable != null && v.variable.DisplayName == displayName)
                {
                    return v.variable.cType == Variable.CountType.CT_LIST;
                }
            }
            return false;
        }
        Dictionary<Key, Candidates> m_Dic = new Dictionary<Key, Candidates>();
        Candidates m_IndexCandidates;

        public VariableCandidates()
        {
            ///> We treat CT_SINGLE as CT_NONE, because CT_SINGLE can be 
            ///  either a single variable or an element of array
            
            foreach (Variable.CountType cType in Enum.GetValues(typeof(Variable.CountType)))
            {
                if (cType == Variable.CountType.CT_SINGLE)
                    continue;
                foreach (Variable.ValueType vType in Enum.GetValues(typeof(Variable.ValueType)))
                {
                    Key key;
                    key.cType = cType;
                    key.vType = vType;
                    Candidates candidates = new Candidates();
                    candidates.key = key;
                    m_Dic[key] = candidates;
                }
            }

            ///> Index of array can only be CT_SINGLE, because for now we dont want
            ///  something like a[a[a[b]]] which is difficult to present on UI
            m_IndexCandidates = new Candidates();
            m_IndexCandidates.key.cType = Variable.CountType.CT_SINGLE;
            m_IndexCandidates.key.vType = Variable.ValueType.VT_INT;
            m_Dic[m_IndexCandidates.key] = m_IndexCandidates;
        }

        public void Refresh(IVariableCollection collection)
        {
            foreach (var v in m_Dic.Values)
            {
                v.StartRefresh();
                v.variables.Clear();
                v.Add(null);
            }

            foreach (var v in collection.Datas)
            {
                {
                    Candidates candidates = Get(v.Variable.vType, v.Variable.cType);
                    if (candidates != null)
                        candidates.Add(v.Variable);
                }

                {
                    Candidates candidates = Get(v.Variable.vType, Variable.CountType.CT_NONE);
                    if (candidates != null)
                        candidates.Add(v.Variable);
                }
            }

            foreach (var v in m_Dic.Values)
            {
                v.variables.Sort();
                v.EndRefresh();
            }
        }

        public Candidates Get(Variable v)
        {
            return Get(v.vType, v.cType == Variable.CountType.CT_SINGLE ? Variable.CountType.CT_NONE : v.cType);
        }

        public Candidates GetIndex()
        {
            return m_IndexCandidates;
        }

        public Candidates Get(Variable.ValueType vType, Variable.CountType cType)
        {
            Key key;
            key.cType = cType;
            key.vType = vType;

            Candidates candidates;
            m_Dic.TryGetValue(key, out candidates);
            return candidates;
        }
    }
}
