using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Finite State Machine
    /// </summary>
    public class FSM : Graph
    {
        List<FSMRootMachineNode> m_RootMachines = new List<FSMRootMachineNode>();

        /// <summary>
        /// The root layer
        /// </summary>
        public FSMRootMachineNode RootMachine { get { return m_RootMachines.Count == 0 ? null : m_RootMachines[0]; } }

        public FSM()
        {
            FSMRootMachineNode root = FSMNodeMgr.Instance.CreateNode<FSMRootMachineNode>();
            root.Graph = this;
            m_RootMachines.Add(root);
        }

        public override void RefreshNodeUID(uint startUID)
        {
            if (IsInState(FLAG_LOADING))
                return;

            uint uid = startUID;

            foreach (FSMRootMachineNode machine in m_RootMachines)
            {
                uint level = 0;

                _RefreshMachineUID(machine, ref uid, level);
            }
        }

        protected void _RefreshMachineUID(FSMMachineNode machine,  ref uint uid, uint level)
        {
            machine.Level = level;

            foreach (var state in machine.States)
            {
                state.UID = ++uid;
            }
            foreach (var state in machine.States)
            {
                if (state is FSMMetaStateNode)
                {
                    FSMMachineNode subMachine = (state as FSMMetaStateNode).SubMachine;
                    subMachine.UID = state.UID;
                    _RefreshMachineUID(subMachine, ref uid, level + 1);
                }
            }
        }
    }
}
