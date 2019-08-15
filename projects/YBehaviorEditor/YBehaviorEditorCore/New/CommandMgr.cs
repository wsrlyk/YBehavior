using System;
using System.Collections.Generic;

namespace YBehavior.Editor.Core.New
{
    public interface ICommand
    {
        void Redo();
        void Undo();
    }

    public class CommandMgr : System.ComponentModel.INotifyPropertyChanged
    {
        LinkedList<ICommand> m_DoneCommands = new LinkedList<ICommand>();
        LinkedList<ICommand> m_UndoCommands = new LinkedList<ICommand>();

        public bool HasDoneCommands { get { return m_DoneCommands.Count > 0; } }
        public bool HasUndoCommands { get { return m_UndoCommands.Count > 0; } }

        public bool Blocked { get; set; } = true;

        public bool Dirty { get; set; }
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

            OnPropertyChanged("HasDoneCommands");
            OnPropertyChanged("HasUndoCommands");

            Dirty = true;
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

                OnPropertyChanged("HasDoneCommands");
                OnPropertyChanged("HasUndoCommands");
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

                OnPropertyChanged("HasDoneCommands");
                OnPropertyChanged("HasUndoCommands");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ConnectNodeCommand : ICommand
    {
        public Connection Conn { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.ConnectNodes(Conn.From, Conn.To);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.DisconnectNodes(Conn);
        }
    }

    public class DisconnectNodeCommand : ICommand
    {
        public Connection Conn { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.DisconnectNodes(Conn);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.ConnectNodes(Conn.From, Conn.To);
        }
    }

    public class AddNodeCommand : ICommand
    {
        public NodeBase Node { get; set; }

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
        public NodeBase Node { get; set; }

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
        public VariableHolder VariableHolder { get; set; }

        public void Redo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench is TreeBench)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as TreeBench).Tree.TreeMemory.AddBackVariable(VariableHolder);
            }
        }
        public void Undo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench is TreeBench)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as TreeBench).Tree.TreeMemory.RemoveVariable(VariableHolder.Variable);
            }
        }
    }

    public class RemoveSharedVariableCommand : ICommand
    {
        public VariableHolder VariableHolder { get; set; }

        public void Redo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench is TreeBench)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as TreeBench).Tree.TreeMemory.RemoveVariable(VariableHolder.Variable);
            }
        }
        public void Undo()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench != null && WorkBenchMgr.Instance.ActiveWorkBench is TreeBench)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as TreeBench).Tree.TreeMemory.AddBackVariable(VariableHolder);
            }
        }
    }

    public class AddInOutVariableCommand : ICommand
    {
        public VariableHolder VariableHolder { get; set; }

        public void Redo()
        {
            if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            {
                VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.AddBackVariable(VariableHolder);
            }
        }
        public void Undo()
        {
            if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            {
                VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.RemoveVariable(VariableHolder.Variable);
            }
        }
    }

    public class RemoveInOutVariableCommand : ICommand
    {
        public VariableHolder VariableHolder { get; set; }

        public void Redo()
        {
            if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            {
                VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.RemoveVariable(VariableHolder.Variable);
            }
        }
        public void Undo()
        {
            if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            {
                VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.AddBackVariable(VariableHolder);
            }
        }
    }

    public class MoveNodeCommand : ICommand
    {
        public NodeBaseRenderer NodeRenderer { get; set; }
        public System.Windows.Point OriginPos { get; set; }
        public System.Windows.Point FinalPos { get; set; }

        public void Redo()
        {
            NodeRenderer.DragMain(FinalPos - OriginPos);
            NodeRenderer.FinishDrag(FinalPos - OriginPos, FinalPos);
        }
        public void Undo()
        {
            NodeRenderer.DragMain(OriginPos - FinalPos);
            NodeRenderer.FinishDrag(OriginPos - FinalPos, OriginPos);
        }
    }

    public class ChangeNodeCommentCommand : ICommand
    {
        public NodeBaseRenderer NodeRenderer { get; set; }
        public string OriginComment { get; set; }
        public string FinalComment { get; set; }

        public void Redo()
        {
            NodeRenderer.Comment = FinalComment;
        }
        public void Undo()
        {
            NodeRenderer.Comment = OriginComment;
        }
    }

    public class ChangeNodeDisableCommand : ICommand
    {
        public NodeBaseRenderer NodeRenderer { get; set; }
        public bool OriginState { get; set; }

        public void Redo()
        {
            NodeRenderer.Disabled = !OriginState;
        }
        public void Undo()
        {
            NodeRenderer.Disabled  = OriginState;
        }
    }

    public class ChangeNodeNickNameCommand : ICommand
    {
        public NodeBaseRenderer NodeRenderer { get; set; }
        public string OriginNickName { get; set; }
        public string FinalNickName { get; set; }

        public void Redo()
        {
            NodeRenderer.NickName = FinalNickName;
        }
        public void Undo()
        {
            NodeRenderer.NickName = OriginNickName;
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
        public bool OldIsLocal { get; set; }
        public bool NewIsLocal { get; set; }

        //public Variable OldVectorIndex { get; set; }

        public void Redo()
        {
            Variable.SetValue(NewValue, NewIsLocal);
        }
        public void Undo()
        {
            Variable.SetValue(OldValue, OldIsLocal);
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

    public class ChangeVariableCTypeCommand : ICommand
    {
        public Variable Variable { get; set; }
        public Variable.CountType OldType { get; set; }
        public Variable.CountType NewType { get; set; }

        public void Redo()
        {
            Variable.cType = NewType;
        }
        public void Undo()
        {
            Variable.cType = OldType;
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

    public class ChangeTreeNodeReturnTypeCommand : ICommand
    {
        public TreeNodeRenderer NodeRenderer { get; set; }
        public string OriginReturnType { get; set; }
        public string FinalReturnType { get; set; }

        public void Redo()
        {
            NodeRenderer.ReturnType = FinalReturnType;
        }
        public void Undo()
        {
            NodeRenderer.ReturnType = OriginReturnType;
        }
    }
}
