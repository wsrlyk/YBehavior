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
    }
}
