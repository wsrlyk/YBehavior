using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class FSMStateRenderer : NodeBaseRenderer
    {
        FSMStateNode m_FSMStateOwner;
        public FSMStateNode FSMStateOwner { get { return m_FSMStateOwner; } }

        public FSMStateRenderer(FSMStateNode stateNode) : base(stateNode)
        {
            m_FSMStateOwner = stateNode;
        }

        public string Tree
        {
            get { return FSMStateOwner.Tree; }
            set
            {
                ////ChangeNodeCommentCommand command = new ChangeNodeCommentCommand()
                ////{
                ////    NodeRenderer = this,
                ////    OriginComment = Owner.Comment,
                ////    FinalComment = value,
                ////};

                FSMStateOwner.Tree = value;

                ////WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public string Type
        {
            get
            {
                return FSMStateOwner.Name;
            }
        }

        public bool IsDefaultState
        {
            get
            {
                return m_FSMStateOwner.OwnerMachine.DefaultState == m_FSMStateOwner;
            }
        }
    }

    public class FSMMachineRenderer : NodeBaseRenderer
    {
        FSMMachineNode m_FSMMachineOwner;
        public FSMMachineNode FSMMachineOwner { get { return m_FSMMachineOwner; } }

        public FSMMachineRenderer(FSMMachineNode machineNode) : base(machineNode)
        {
            m_FSMMachineOwner = machineNode;
        }

        public override string FullName
        {
            get
            {
                return m_FSMMachineOwner.MetaState == null ? "Root" : m_FSMMachineOwner.MetaState.ForceGetRenderer.FullName;
            }
        }
    }
}
