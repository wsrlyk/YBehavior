using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core
{
    class Utility
    {
        public static Node CloneNode(Node template, bool bIncludeChildren)
        {
            Node node = template.Clone();
            node.Init();

            if (bIncludeChildren)
            {
                foreach (Node templatechild in template.Conns)
                {
                    Node child = CloneNode(templatechild, true);
                    node.Conns.Connect(child, templatechild.ParentConn.Identifier);
                }
            }

            return node;
        }
    }
}
