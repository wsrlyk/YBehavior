using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class FSM : Graph
    {
        List<FSMRootMachineNode> m_RootMachines = new List<FSMRootMachineNode>();

        /// <summary>
        /// Now we just support one layer
        /// </summary>
        public FSMRootMachineNode RootMachine { get { return m_RootMachines.Count == 0 ? null : m_RootMachines[0]; } }

        public void CreateRoot()
        {
            FSMRootMachineNode root = FSMNodeMgr.Instance.CreateNode<FSMRootMachineNode>();
            root.Graph = this;
            m_RootMachines.Add(root);
        }
    }
}
