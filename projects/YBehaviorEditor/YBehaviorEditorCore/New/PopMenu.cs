using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class PopMenu
    {
        public DelayableNotificationCollection<PopMenuItem> Children { get; } = new DelayableNotificationCollection<PopMenuItem>();
    }

    public class PopMenuItem
    {
        public string Name { get; set; }
        public List<PopMenuItem> Children { get; } = new List<PopMenuItem>();
        public PopMenuCommand Command { get; set; }
    }

    public class PopMenuCommand
    {
        public virtual void Do() { }
    }

    public class MakeTransitionPopMenuCommand : PopMenuCommand
    {
         
        public override void Do()
        {
        }
    }

    public class PopMenuUtility
    {
        public static PopMenu CreateFSMStateStartMenu()
        {
            PopMenu popMenu = new PopMenu();

            return popMenu;
        }
    }
}
