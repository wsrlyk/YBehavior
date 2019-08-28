using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class FSMConnection : Connection
    {
        public List<TransitionResult> Trans { get; } = new List<TransitionResult>();

        public FSMConnection(Connector from, Connector to) : base(from, to)
        {
        }
    }
}
