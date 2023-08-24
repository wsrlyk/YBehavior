using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    class ToolBarViewModel
    {
        public class Tool
        {
            public interface ICanExecute
            {
                bool CanExecute { get; }
            }

            public Command Command { get; private set; }
            public Func<bool> CanExecute { get; private set; }
            public System.Windows.Input.ICommand CMD { get; private set; }

            public string Content
            {
                get
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Command);
                    var kb = Config.Instance.KeyBindings.GetKeyBinding(Command);
                    if (kb.key != Key.None)
                    {
                        sb.Append('\n');
                        if (kb.modifier != ModifierKeys.None)
                        {
                            if ((kb.modifier & ModifierKeys.Control) != 0)
                                sb.Append("Ctrl+");
                            if ((kb.modifier & ModifierKeys.Alt) != 0)
                                sb.Append("Alt+");
                            if ((kb.modifier & ModifierKeys.Shift) != 0)
                                sb.Append("Shift+");
                            if ((kb.modifier & ModifierKeys.Windows) != 0)
                                sb.Append("Win+");
                        }
                        sb.Append(kb.key);
                        if (Command == Command.Duplicate
                            || Command == Command.Copy
                            || Command == Command.Delete)
                            sb.Append("\n(").Append(Config.Instance.KeyBindings.MultiKey).Append(")");
                    }
                    return sb.ToString();
                }
            }

            public string Tips
            {
                get
                {
                    return DescriptionMgr.Instance.GetCommandDescription(Command.ToString()).tips;
                }
            }

            public Tool(Command command, Func<bool> canExecute = null, ICanExecuteChanged canExecuteChanged = null)
            {
                Command = command;
                if (canExecute != null)
                {
                    CanExecute = canExecute;
                    CMD = new RelayCommand(_Execute, _CanExecute, canExecuteChanged);
                }
                else
                {
                    CMD = new RelayCommand(_Execute);
                }
            }
            void _Execute(object o)
            {
                MainWindow.ProcessCommand(Command, Keyboard.Modifiers);
            }

            bool _CanExecute(object o)
            {
                if (CanExecute != null)
                    return CanExecute();
                return true;
            }
        }
        public DelayableNotificationCollection<Tool> Tools { get; } = new DelayableNotificationCollection<Tool>();

        public ToolBarViewModel()
        {
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);

            Tools.Add(new Tool(Command.Open));
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;
            using (Tools.Delay())
            {
                Tools.Clear();
                Tools.Add(new Tool(Command.Open));
                if (oArg.Bench != null)
                {
                    Tools.Add(new Tool(Command.Save));
                    Tools.Add(new Tool(Command.SaveAs));
                    Tools.Add(new Tool(
                        Command.Undo,
                        () => { return oArg.Bench.CommandMgr.HasDoneCommands; },
                        new CommandMgrCanExecuteChanged(oArg.Bench)
                        ));
                    Tools.Add(new Tool(
                        Command.Redo,
                        () => { return oArg.Bench.CommandMgr.HasUndoCommands; },
                        new CommandMgrCanExecuteChanged(oArg.Bench)
                        ));
                    Tools.Add(new Tool(Command.Duplicate));
                    Tools.Add(new Tool(Command.Copy));
                    Tools.Add(new Tool(Command.Paste));
                    Tools.Add(new Tool(Command.Delete));
                    Tools.Add(new Tool(Command.Search));
                    Tools.Add(new Tool(Command.Center));
                    Tools.Add(new Tool(Command.Clear));
                    if (oArg.Bench is TreeBench)
                    {
                        Tools.Add(new Tool(Command.Condition));
                        Tools.Add(new Tool(Command.Fold));
                    }
                    else
                    {
                        Tools.Add(new Tool(Command.Default));
                    }
                    Tools.Add(new Tool(Command.LogPoint));
                    Tools.Add(new Tool(Command.BreakPoint));
                    Tools.Add(new Tool(Command.Disable));
                }
            }
        }
        public class CommandMgrCanExecuteChanged : ICanExecuteChanged
        {
            public CommandMgrCanExecuteChanged(WorkBench wb)
            {
                bench = wb;
            }
            protected WorkBench bench;
            public event EventHandler CanExecuteChanged
            {
                add { bench.CommandMgr.OnCommandUpdate += value; }
                remove { bench.CommandMgr.OnCommandUpdate -= value; }
            }
        }
    }
}

