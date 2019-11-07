using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    public class MenuItemViewModel
    {
        private readonly System.Windows.Input.ICommand _command;

        public MenuItemViewModel(Action action)
        {
            _command = new CommandViewModel(action);
        }

        public string Header { get; set; }

        public List<MenuItemViewModel> MenuItems { get; set; }

        public System.Windows.Input.ICommand Command
        {
            get
            {
                return _command;
            }
        }

        private void Execute()
        {
            // (NOTE: In a view model, you normally should not use MessageBox.Show()).
            //MessageBox.Show("Clicked at " + Header);
        }
    }

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

    public class PopMenuUtility
    {
        public static MenuItemViewModel CreateFSMConnectionDropMenu(FSMStateNode from, FSMStateNode to)
        {

            FSMMachineNode machine = null;
            if (to is FSMMetaStateNode)
                machine = (to as FSMMetaStateNode).SubMachine;
            else if (to is FSMUpperStateNode)
                machine = to.RootMachine;

            if (machine == null)
                return null;

            return _CreateMachineMenuItem(from, machine, to.OwnerMachine);
        }

        static MenuItemViewModel _CreateMachineMenuItem(FSMStateNode from, FSMMachineNode to, FSMMachineNode except)
        {
            MenuItemViewModel popMenu = new MenuItemViewModel(null) { Header = to.ForceGetRenderer.UITitle };
            popMenu.MenuItems = new List<MenuItemViewModel>();
            foreach (var state in to.States)
            {
                if (state.Type == FSMStateType.Special)
                    continue;

                MenuItemViewModel model;
                ///> The states
                model = new MenuItemViewModel(() =>
                    {
                        WorkBenchMgr.Instance.ConnectNodes(from.Conns.GetConnector(Connector.IdentifierChildren), state.Conns.ParentConnector);
                    })
                { Header = state.ForceGetRenderer.UITitle };

                popMenu.MenuItems.Add(model);

                ///> The substates
                if (state is FSMMetaStateNode)
                {
                    FSMMachineNode subMachine = (state as FSMMetaStateNode).SubMachine;
                    if (subMachine != except)
                    {
                        model = _CreateMachineMenuItem(from, subMachine, except);
                        popMenu.MenuItems.Add(model);
                    }
                }
            }

            return popMenu;

        }
    }
}
