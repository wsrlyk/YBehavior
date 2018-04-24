using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class SharedData
    {
        ObservableDictionary<string, Variable> m_Variables = new ObservableDictionary<string, Variable>();
        Node m_Owner;

        public SharedData(Node owner)
        {
            m_Owner = owner;
        }

        public static bool IsValidVariableName(string name)
        {
            string pattern = @"^[a-zA-Z0-9]*$";
            bool res = false;
            if (name.Length > 0 && name.Length <= 15)
            {
                res = (System.Text.RegularExpressions.Regex.IsMatch(name, pattern));
            }
            else
            {
                LogMgr.Instance.Error("Name Length (Only: 1~15): " + name);
                return false;
            }
            if (!res)
                LogMgr.Instance.Error("Contains invalid characters (Only: a~z, A~Z, 0~9): " + name);
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
            Variable v = new Variable(this);
            if (!v.SetVariableInNode(value, name))
                return false;

            v.AlwaysConst = true;
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
            Variable v = new Variable(this);
            if (!v.SetVariable(vType, cType, Variable.VariableType.VBT_Const, value, null, name))
                return false;

            v.AlwaysConst = true;
            v.CanBeRemoved = true;

            if (!v.CheckValid())
                return false;

            return AddVariable(v);
        }

        public bool AddVariable(Variable v)
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
            m_Variables[v.Name] = v;
            v.Container = this;
            
            if (m_Owner is Tree)
            {
                SharedVariableChangedArg arg = new SharedVariableChangedArg();
                EventMgr.Instance.Send(arg);
            }
            return true;
        }

        public bool RemoveVariable(Variable v)
        {
            if (v == null)
                return false;

            if (!m_Variables.ContainsKey(v.Name))
            {
                LogMgr.Instance.Error("Variable not exist when try remove: " + v.Name);
                return false;
            }

            m_Variables.Remove(v.Name);

            if (m_Owner is Tree)
            {
                SharedVariableChangedArg arg = new SharedVariableChangedArg();
                EventMgr.Instance.Send(arg);
            }
            return true;
        }
        public Variable GetVariable(string name)
        {
            m_Variables.TryGetValue(name, out Variable v);
            return v;
        }

        public ObservableDictionary<string, Variable> Datas { get { return m_Variables; } }

        public void OnVariableValueChanged(Variable v)
        {
            if (m_Owner != null)
                m_Owner.OnPropertyChanged("Note");
        }

        public void RefreshVariables()
        {
            foreach (var v in m_Variables.Values)
            {
                v.RefreshCandidates(true);
            }
        }

        public SharedData Clone()
        {
            SharedData sharedData = new SharedData(m_Owner);

            foreach (KeyValuePair<string, Variable> v in m_Variables)
            {
                Variable vv = v.Value.Clone();
                sharedData.AddVariable(vv);
            }

            return sharedData;
        }
    }
}
