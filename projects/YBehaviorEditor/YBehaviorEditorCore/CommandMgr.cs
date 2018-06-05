using System;
using System.Collections.Generic;

namespace YBehavior.Editor.Core
{
    public interface ICommand
    {
        void Redo();
        void Undo();
    }

    public class CommandMgr
    {
        LinkedList<ICommand> m_DoneCommands = new LinkedList<ICommand>();
        LinkedList<ICommand> m_UndoCommands = new LinkedList<ICommand>();

        bool m_bDoing = false;
        public void PushDoneCommand(ICommand command)
        {
            if (m_bDoing)
                return;

            if (m_DoneCommands.Count > 20)
                m_DoneCommands.RemoveFirst();
            m_DoneCommands.AddLast(command);

            m_UndoCommands.Clear();
        }

        public void Undo()
        {
            if (m_DoneCommands.Count > 0)
            {
                m_bDoing = true;
                ICommand last = m_DoneCommands.Last.Value;
                m_DoneCommands.RemoveLast();
                last.Undo();
                m_UndoCommands.AddLast(last);
                m_bDoing = false;
            }
        }

        public void Redo()
        {
            if (m_UndoCommands.Count > 0)
            {
                m_bDoing = true;
                ICommand last = m_UndoCommands.Last.Value;
                m_UndoCommands.RemoveLast();
                last.Redo();
                m_DoneCommands.AddLast(last);
                m_bDoing = false;
            }
        }
    }

    public class ConnectNodeCommand : ICommand
    {
        public ConnectionHolder Parent { get; set; }
        public ConnectionHolder Child { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.ConnectNodes(Parent, Child);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.DisconnectNodes(Child);
        }
    }

    public class DisconnectNodeCommand : ICommand
    {
        public ConnectionHolder Parent { get; set; }
        public ConnectionHolder Child { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.DisconnectNodes(Child);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.ConnectNodes(Parent, Child);
        }
    }

    public class AddNodeCommand : ICommand
    {
        public Node Node { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.AddNode(Node);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.RemoveNode(Node);
        }
    }

    public class RemoveNodeCommand : ICommand
    {
        public Node Node { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.RemoveNode(Node);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.AddNode(Node);
        }
    }

    public class AddSharedVariableCommand : ICommand
    {
        public Variable Variable { get; set; }

        public void Redo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench.MainTree != null)
            {
                WorkBenchMgr.Instance.ActiveWorkBench.MainTree.SharedData.AddVariable(Variable);
            }
        }
        public void Undo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench.MainTree != null)
            {
                WorkBenchMgr.Instance.ActiveWorkBench.MainTree.SharedData.RemoveVariable(Variable);
            }
        }
    }

    public class RemoveSharedVariableCommand : ICommand
    {
        public Variable Variable { get; set; }

        public void Redo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench.MainTree != null)
            {
                WorkBenchMgr.Instance.ActiveWorkBench.MainTree.SharedData.RemoveVariable(Variable);
            }
        }
        public void Undo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench.MainTree != null)
            {
                WorkBenchMgr.Instance.ActiveWorkBench.MainTree.SharedData.AddVariable(Variable);
            }
        }
    }

    public class MoveNodeCommand : ICommand
    {
        public Node Node { get; set; }
        public System.Windows.Point OriginPos { get; set; }
        public System.Windows.Point FinalPos { get; set; }

        public void Redo()
        {
            Node.Renderer.DragMain(FinalPos - OriginPos);
            Node.Renderer.FinishDrag(FinalPos - OriginPos, FinalPos);
        }
        public void Undo()
        {
            Node.Renderer.DragMain(OriginPos - FinalPos);
            Node.Renderer.FinishDrag(OriginPos - FinalPos, OriginPos);
        }
    }

    public class AddCommentCommand : ICommand
    {
        public Comment Comment { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.AddComment(Comment);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.RemoveComment(Comment);
        }
    }

    public class RemoveCommentCommand : ICommand
    {
        public Comment Comment { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.RemoveComment(Comment);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.AddComment(Comment);
        }
    }

}
