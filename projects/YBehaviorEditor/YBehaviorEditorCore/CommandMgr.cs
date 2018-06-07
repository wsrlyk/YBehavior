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

        public bool Blocked { get; set; } = true;

        bool m_bDoing = false;
        public void PushDoneCommand(ICommand command)
        {
            if (m_bDoing || Blocked)
                return;

            if (m_DoneCommands.Count > 20)
                m_DoneCommands.RemoveFirst();
            m_DoneCommands.AddLast(command);

            m_UndoCommands.Clear();

            LogMgr.Instance.Log("Push command: " + command.ToString() + ", Total: " + m_DoneCommands.Count.ToString());
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

    public class ChangeNodeCommentCommand : ICommand
    {
        public Node Node { get; set; }
        public string OriginComment { get; set; }
        public string FinalComment { get; set; }

        public void Redo()
        {
            Node.Comment = FinalComment;
        }
        public void Undo()
        {
            Node.Comment = OriginComment;
        }
    }

    public class ChangeNodeDisableCommand : ICommand
    {
        public Node Node { get; set; }
        public bool OriginState { get; set; }

        public void Redo()
        {
            Node.Disabled = !OriginState;
        }
        public void Undo()
        {
            Node.Disabled  = OriginState;
        }
    }

    public class ChangeNodeNickNameCommand : ICommand
    {
        public Node Node { get; set; }
        public string OriginNickName { get; set; }
        public string FinalNickName { get; set; }

        public void Redo()
        {
            Node.NickName = FinalNickName;
        }
        public void Undo()
        {
            Node.NickName = OriginNickName;
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

    public class ChangeCommentCommand : ICommand
    {
        public Comment Comment { get; set; }
        public string OriginContent { get; set; }
        public string FinalContent { get; set; }

        public void Redo()
        {
            Comment.Content = FinalContent;
        }
        public void Undo()
        {
            Comment.Content = OriginContent;
        }
    }

    public class MoveCommentCommand : ICommand
    {
        public Comment Comment { get; set; }
        public System.Windows.Rect OriginRec { get; set; }
        public System.Windows.Rect FinalRec { get; set; }

        public void Redo()
        {
            Comment.Geo.Rec = FinalRec;
            Comment.OnGeometryChangedWithoutCommand();
        }
        public void Undo()
        {
            Comment.Geo.Rec = OriginRec;
            Comment.OnGeometryChangedWithoutCommand();
        }
    }

    public class ChangeVariableValueCommand : ICommand
    {
        public Variable Variable { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        //public Variable OldVectorIndex { get; set; }

        public void Redo()
        {
            Variable.Value = NewValue;
        }
        public void Undo()
        {
            Variable.Value = OldValue;
            //if (OldVectorIndex != null)
            //{
            //    Variable.VectorIndex = OldVectorIndex;
            //}
        }
    }

    public class ChangeVariableVBTypeCommand : ICommand
    {
        public Variable Variable { get; set; }
        public Variable.VariableType OldType { get; set; }
        public Variable.VariableType NewType { get; set; }

        public void Redo()
        {
            Variable.vbType = NewType;
        }
        public void Undo()
        {
            Variable.vbType = OldType;
        }
    }

    public class ChangeVariableVTypeCommand : ICommand
    {
        public Variable Variable { get; set; }
        public Variable.ValueType OldType { get; set; }
        public Variable.ValueType NewType { get; set; }

        public void Redo()
        {
            Variable.vType = NewType;
        }
        public void Undo()
        {
            Variable.vType = OldType;
        }
    }
}
