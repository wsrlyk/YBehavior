using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
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
        public FSMUIOutConnector From;
        public FSMNode Target;

        public override void Do()
        {
        }
    }

    public class PopMenuUtility
    {
        public static PopMenu CreateFSMConnectionDropMenu(FSMUIOutConnector from, FSMUIInConnector to)
        {
            PopMenu popMenu = new PopMenu();

            return popMenu;
        }
    }
}
