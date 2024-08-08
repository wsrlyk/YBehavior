using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// ViewModel of FSM state node
    /// </summary>
    public class FSMStateRenderer : NodeBaseRenderer
    {
        FSMStateNode m_FSMStateOwner;
        /// <summary>
        /// Model
        /// </summary>
        public FSMStateNode FSMStateOwner { get { return m_FSMStateOwner; } }

        public FSMStateRenderer(FSMStateNode stateNode) : base(stateNode)
        {
            m_FSMStateOwner = stateNode;
        }
        /// <summary>
        /// The tree that would be executed
        /// </summary>
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
                PropertyChange(RenderProperty.Note);
                ////WorkBenchMgr.Instance.PushCommand(command);
            }
        }
        /// <summary>
        /// For displaying the trees that could be selected
        /// </summary>
        public DelayableNotificationCollection<string> TreeList
        {
            get { return FileMgr.Instance.TreeList; }
        }
        public string Type
        {
            get
            {
                return FSMStateOwner.Name;
            }
        }
        /// <summary>
        /// Whether this node is default state
        /// </summary>
        public bool IsDefaultState
        {
            get
            {
                return m_FSMStateOwner.OwnerMachine.DefaultState == m_FSMStateOwner;
            }
        }

        protected override bool _BeforeDelete(int param)
        {
            if (m_FSMStateOwner.OwnerMachine.DefaultState == m_FSMStateOwner)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).ResetDefault(m_FSMStateOwner.OwnerMachine);
            }
            return true;
        }
    }
    /// <summary>
    /// ViewModel of FSM machine
    /// </summary>
    public class FSMMachineRenderer : NodeBaseRenderer
    {
        FSMMachineNode m_FSMMachineOwner;
        /// <summary>
        /// Model
        /// </summary>
        public FSMMachineNode FSMMachineOwner { get { return m_FSMMachineOwner; } }

        public FSMMachineRenderer(FSMMachineNode machineNode) : base(machineNode)
        {
            m_FSMMachineOwner = machineNode;
        }
        /// <summary>
        /// Root or the meta state this machine belongs to
        /// </summary>
        public override string FullName
        {
            get
            {
                return m_FSMMachineOwner.MetaState == null ? "Root" : m_FSMMachineOwner.MetaState.ForceGetRenderer.FullName;
            }
        }
    }
}
