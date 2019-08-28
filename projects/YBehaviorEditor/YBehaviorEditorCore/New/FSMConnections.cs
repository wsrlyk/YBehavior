using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class FSMConnection : Connection
    {
        public List<TransitionResult> Trans { get; } = new List<TransitionResult>();

        public FSMConnectionRenderer FSMRenderer;

        public FSMConnection(Connector from, Connector to) : base(from, to)
        {
            FSMRenderer = Renderer as FSMConnectionRenderer;
            FSMRenderer.FSMOwner = this;
        }

        protected override ConnectionRenderer _CreateRenderer()
        {
            return new FSMConnectionRenderer();
        }
    }

    public class FSMConnectionRenderer : ConnectionRenderer
    {
        public FSMConnection FSMOwner;
        public string Name
        {
            get
            {
                return string.Format("{0} -> {1}"
                    , FSMOwner.Ctr.From.Owner.ForceGetRenderer.FullName
                    , FSMOwner.Ctr.To.Owner.ForceGetRenderer.FullName);
            }
        }

    }
}
