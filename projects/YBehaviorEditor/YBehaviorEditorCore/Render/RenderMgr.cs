using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class RenderMgr : Singleton<RenderMgr>
    {
        public DelayableNotificationCollection<Renderer> NodeList { get; } = new DelayableNotificationCollection<Renderer>();
        public DelayableNotificationCollection<ConnectionRenderer> ConnectionList { get; } = new DelayableNotificationCollection<ConnectionRenderer>();

        public void AddConnection(ConnectionRenderer connectionRenderer)
        {
            ConnectionList.Add(connectionRenderer);
        }
        public void RemoveConnection(ConnectionRenderer connectionRenderer)
        {
            ConnectionList.Remove(connectionRenderer);
        }

        public void AddNode(Renderer renderer)
        {
            NodeList.Add(renderer);
        }

        public void ClearNodes()
        {
            NodeList.Clear();
        }

        public void ClearConnections()
        {
            ConnectionList.Clear();
        }
    }
}
