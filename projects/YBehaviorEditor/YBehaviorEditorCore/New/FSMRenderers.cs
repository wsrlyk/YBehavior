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
    }
}
