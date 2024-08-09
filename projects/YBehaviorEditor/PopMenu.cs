using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// Interface of menu item ViewModel
    /// </summary>
    public interface IMenuItemViewModel
    {
        string Text { get; set; }
    }
    /// <summary>
    /// Class of MenuItem Headline ViewModel
    /// </summary>
    public class MenuItemHeadViewModel : IMenuItemViewModel
    {
        public string Text { get; set; }
    }
    /// <summary>
    /// Class of MenuItem ViewModel
    /// </summary>
    public class MenuItemViewModel : IMenuItemViewModel
    {
        private readonly System.Windows.Input.ICommand _command;

        public MenuItemViewModel(Action action)
        {
            _command = new CommandViewModel(action);
        }

        public string Text { get; set; }
        /// <summary>
        /// Collection of items
        /// </summary>
        public List<IMenuItemViewModel> MenuItems { get; set; }

        public System.Windows.Input.ICommand Command
        {
            get
            {
                return _command;
            }
        }
    }
    /// <summary>
    /// A simple command ViewModel for menuitem
    /// </summary>
    public class CommandViewModel : System.Windows.Input.ICommand
    {
        private readonly Action _action;

        public CommandViewModel(Action action)
        {
            _action = action;
        }

        public void Execute(object o)
        {
            _action();
        }

        public bool CanExecute(object o)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
    /// <summary>
    /// Utility about pop menu
    /// </summary>
    public class PopMenuUtility
    {
        /// <summary>
        /// Create a pop menu for connecting two fsm state nodes
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static MenuItemViewModel CreateFSMConnectionDropMenu(FSMStateNode from, FSMStateNode to)
        {
            MenuItemViewModel menu = null;
            if (to is FSMMetaStateNode)
                menu = _CreateMachineMenuItem(from, to as FSMMetaStateNode, to.OwnerMachine);
            else if (to is FSMUpperStateNode)
                menu = _CreateMachineMenuItem(from, to.RootMachine, to.OwnerMachine);

            if (menu == null)
                return null;

            menu.MenuItems.Insert(0, new MenuItemHeadViewModel() { Text = string.Format("{0} => ", from.ForceGetRenderer.UITitle) });
            return menu;
        }

        static MenuItemViewModel _CreateMachineMenuItem(FSMStateNode from, FSMMetaStateNode to, FSMMachineNode except)
        {
            MenuItemViewModel popMenu = new MenuItemViewModel(null) { Text = to.ForceGetRenderer.UITitle };
            popMenu.MenuItems = new List<IMenuItemViewModel>();
            popMenu.MenuItems.Add(_CreateMenu(from, to));
            var menu = _CreateMachineMenuItem(from, to.SubMachine, except);
            if (menu != null)
                popMenu.MenuItems.Add(menu);
            return popMenu;
        }

        static MenuItemViewModel _CreateMachineMenuItem(FSMStateNode from, FSMMachineNode to, FSMMachineNode except)
        {
            MenuItemViewModel popMenu = new MenuItemViewModel(null) { Text = to.ForceGetRenderer.UITitle };
            popMenu.MenuItems = new List<IMenuItemViewModel>();
            foreach (var state in to.States)
            {
                if (state.Type == FSMStateType.Special)
                    continue;

                MenuItemViewModel model;
                ///> The states
                model = _CreateMenu(from, state);

                popMenu.MenuItems.Add(model);

                ///> The substates
                if (state is FSMMetaStateNode)
                {
                    FSMMachineNode subMachine = (state as FSMMetaStateNode).SubMachine;
                    if (subMachine != except)
                    {
                        model = _CreateMachineMenuItem(from, subMachine, except);
                        if (model != null)
                            popMenu.MenuItems.Add(model);
                    }
                }
            }

            if (popMenu.MenuItems.Count == 0)
                return null;
            return popMenu;

        }

        static MenuItemViewModel _CreateMenu(FSMStateNode from, FSMStateNode to)
        {
            return new MenuItemViewModel(() =>
            {
                WorkBenchMgr.Instance.ConnectNodes(from.Conns.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN), to.Conns.ParentConnector);
            })
            { Text = to.ForceGetRenderer.UITitle };
        }
    }
}
