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

        public struct UID
        {
            public uint Layer;		///> Up to 4 Layers
			public uint Level;		///> Up to 8 Levels of SubMachines per Layer
			public uint Machine;		///> Up to 32 SubMachines in the same Level
			public uint State;		///> Up to 64 State in the same machine

            public override string ToString()
            {
                return string.Format("[{0} {1} {2} {3}]", Layer, Level, Machine, State);
            }

            public uint ToUID()
            {
                return (Layer & 0x3)
                    | ((Level << 2) & 0x7)
                    | ((Machine << (2 + 3)) & 0x1f)
                    | ((State << (2 + 3 + 5)) & 0x3f);
            }

            public static uint GetLayer(uint uid)
            {
                return uid & 0x3;
            }

            public static uint GetLevel(uint uid)
            {
                return (uid >> 2) & 0x7;
            }

            public static uint GetMachine(uint uid)
            {
                return (uid >> (2 + 3)) & 0x1f;
            }


            public static uint GetState(uint uid)
            {
                return (uid >> (2 + 3 + 5)) & 0x3f;
            }

            public static string ToString(uint uid)
            {
                UID id;
                id.Layer = GetLayer(uid);
                id.Level = GetLevel(uid);
                id.Machine = GetMachine(uid);
                id.State = GetState(uid);
                return id.ToString();
            }
        }
        public override void RefreshNodeUID()
        {
            if (IsInState(FLAG_LOADING))
                return;

            UID uid = new UID();


            foreach (FSMRootMachineNode machine in m_RootMachines)
            {
                uint[] levelMachineCount = new uint[8];

                _RefreshMachineUID(machine, uid, levelMachineCount);
                ++uid.Layer;
            }
        }

        protected void _RefreshMachineUID(FSMMachineNode machine, UID uid, uint[] levelMachineCount)
        {
            UID id = uid;
            id.Machine = ++levelMachineCount[uid.Level];
            machine.UID = id.ToUID();
            LogMgr.Instance.Log("Machine " + id.ToString());

            foreach (var state in machine.States)
            {
                ++id.State;
                state.UID = id.ToUID();
                LogMgr.Instance.Log("State " + id.ToString());
                if (state is FSMMetaStateNode)
                {
                    FSMMachineNode subMachine = (state as FSMMetaStateNode).SubMachine;
                    UID subUID = id;
                    ++subUID.Level;
                    subUID.State = 0;

                    _RefreshMachineUID(subMachine, subUID, levelMachineCount);
                }
            }
        }
    }
}
