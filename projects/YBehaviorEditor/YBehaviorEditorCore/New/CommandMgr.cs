﻿using System;
using System.Collections.Generic;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Interface of a command
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Called when Redo
        /// </summary>
        void Redo();
        /// <summary>
        /// Called when Undo
        /// </summary>
        void Undo();
    }

    /// <summary>
    /// Undo/Redo commands management
    /// </summary>
    public class CommandMgr : System.ComponentModel.INotifyPropertyChanged
    {
        LinkedList<ICommand> m_DoneCommands = new LinkedList<ICommand>();
        LinkedList<ICommand> m_UndoCommands = new LinkedList<ICommand>();

        public event EventHandler OnCommandUpdate;

        public bool HasDoneCommands { get { return m_DoneCommands.Count > 0; } }
        public bool HasUndoCommands { get { return m_UndoCommands.Count > 0; } }

        public bool Blocked { get; set; } = false;

        public bool Dirty { get; set; }
        bool m_bDoing = false;
        /// <summary>
        /// Push a command to done list, and clear the undo list.
        /// </summary>
        /// <param name="command"></param>
        public void PushDoneCommand(ICommand command)
        {
            if (m_bDoing || Blocked)
                return;

            if (m_DoneCommands.Count > 20)
                m_DoneCommands.RemoveFirst();
            m_DoneCommands.AddLast(command);

            m_UndoCommands.Clear();

            //LogMgr.Instance.Log("Push command: " + command.ToString() + ", Total: " + m_DoneCommands.Count.ToString());

            OnCommandUpdate?.Invoke(this, null);
            //OnPropertyChanged("HasDoneCommands");
            //OnPropertyChanged("HasUndoCommands");

            Dirty = true;
        }
        /// <summary>
        /// Undo the last command from the done list, and push it to the back of the undo list
        /// </summary>
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

                //LogMgr.Instance.Log("Undo command: " + last.ToString());

                OnCommandUpdate.Invoke(this, null);
                //OnPropertyChanged("HasDoneCommands");
                //OnPropertyChanged("HasUndoCommands");
            }
        }
        /// <summary>
        /// Redo the last command from the undo list, and push it to the back of the done list
        /// </summary>
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

                //LogMgr.Instance.Log("Redo command: " + last.ToString());

                OnCommandUpdate.Invoke(this, null);
                //OnPropertyChanged("HasDoneCommands");
                //OnPropertyChanged("HasUndoCommands");
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
        public Connection.FromTo Conn { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.ConnectNodes(Conn.From, Conn.To);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.DisconnectNodes(Conn);
        }
    }

    /// <summary>
    /// Disconnect two nodes
    /// </summary>
    public class DisconnectNodeCommand : ICommand
    {
        public Connection.FromTo Conn { get; set; }

        public void Redo()
        {
            WorkBenchMgr.Instance.DisconnectNodes(Conn);
        }
        public void Undo()
        {
            WorkBenchMgr.Instance.ConnectNodes(Conn.From, Conn.To);
        }
    }
    /// <summary>
    /// Add a node to the current tree/fsm
    /// </summary>
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
    /// <summary>
    /// remove a node from the current tree/fsm
    /// </summary>
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
    /// <summary>
    /// Add a variable to the current tree
    /// </summary>
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
    /// <summary>
    /// Remove a variable from the current tree
    /// </summary>
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
    /// <summary>
    /// Add an input/output
    /// </summary>
    public class AddInOutVariableCommand : ICommand
    {
        public VariableHolder VariableHolder { get; set; }

        public void Redo()
        {
            VariableHolder.Variable.Container.DoInsertVariable(VariableHolder);
            //if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            //{
            //    VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.AddBackVariable(VariableHolder);
            //}
        }
        public void Undo()
        {
            VariableHolder.Variable.Container.DoRemove(VariableHolder);

            //if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            //{
            //    VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.RemoveVariable(VariableHolder.Variable);
            //}
        }
    }
    /// <summary>
    /// Remove an input/output
    /// </summary>
    public class RemoveInOutVariableCommand : ICommand
    {
        public VariableHolder VariableHolder { get; set; }

        public void Redo()
        {
            VariableHolder.Variable.Container.DoRemove(VariableHolder);
            //if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            //{
            //    VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.RemoveVariable(VariableHolder.Variable);
            //}
        }
        public void Undo()
        {
            VariableHolder.Variable.Container.DoInsertVariable(VariableHolder);
            //if (VariableHolder.Variable.SharedDataSource != null && VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData != null)
            //{
            //    VariableHolder.Variable.SharedDataSource.VariableDataSource.InOutData.AddBackVariable(VariableHolder);
            //}
        }
    }
    /// <summary>
    /// Move a node
    /// </summary>
    public class MoveNodeCommand : ICommand
    {
        public NodeBaseRenderer NodeRenderer { get; set; }
        public System.Windows.Point OriginPos { get; set; }
        public System.Windows.Point FinalPos { get; set; }
        public int DragParam { get; set; }
        public void Redo()
        {
            NodeRenderer.DragMain(FinalPos - OriginPos, DragParam);
            NodeRenderer.FinishDrag(FinalPos - OriginPos, FinalPos);
        }
        public void Undo()
        {
            NodeRenderer.DragMain(OriginPos - FinalPos, DragParam);
            NodeRenderer.FinishDrag(OriginPos - FinalPos, OriginPos);
        }
    }
    /// <summary>
    /// Change a node comment
    /// </summary>
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
    /// <summary>
    /// Change node disable state
    /// </summary>
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
    /// <summary>
    /// Change node nickname
    /// </summary>
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
    /// <summary>
    /// Add a comment
    /// </summary>
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
    /// <summary>
    /// Remove a comment
    /// </summary>

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
    /// <summary>
    /// Change the comment content
    /// </summary>
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
    /// <summary>
    /// Move a comment
    /// </summary>
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
    /// <summary>
    /// Change the value of a variable
    /// </summary>
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
    /// <summary>
    /// Change the VariableType of a variable
    /// </summary>
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
    /// <summary>
    /// Change the CountType of a variable
    /// </summary>
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
    /// <summary>
    /// Change the ValueType of a variable
    /// </summary>
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
    /// <summary>
    /// Change the ReturnType of a treenode
    /// </summary>
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
    /// <summary>
    /// Change the EnableType of a variable
    /// </summary>
    public class ChangeVariableETypeCommand : ICommand
    {
        public Variable Variable { get; set; }
        public Variable.EnableType OldType { get; set; }
        public Variable.EnableType NewType { get; set; }

        public void Redo()
        {
            Variable.eType = NewType;
        }
        public void Undo()
        {
            Variable.eType = OldType;
        }
    }
    /// <summary>
    /// Remove a transition of the active fsm
    /// </summary>
    public class RemoveTransCommand : ICommand
    {
        public Connection.FromTo Conn { get; set; }
        public Transition Trans { get; set; }
        public void Redo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).Disconnect(Conn, Trans);
        }
        public void Undo()
        {
            Trans = (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).MakeTrans(Conn.From, Conn.To, Trans);
        }
    }
    /// <summary>
    /// Make a transition of the active fsm
    /// </summary>
    public class MakeTransCommand : ICommand
    {
        public Connection.FromTo Conn { get; set; }
        public Transition Trans { get; set; }
        public void Redo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).MakeTrans(Conn.From, Conn.To, Trans);
        }
        public void Undo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).Disconnect(Conn, Trans);
        }
    }
    /// <summary>
    /// Add a condition to the active fsm
    /// </summary>
    public class AddCondCommand : ICommand
    {
        public TransitionMapValue Cond { get; set; }
        public Transition Trans { get; set; }
        public void Redo()
        {
            Trans.Value.Add(Cond);
        }
        public void Undo()
        {
            Trans.Value.Remove(Cond);
        }
    }
    /// <summary>
    /// Remove a condition from the active fsm
    /// </summary>
    public class RemoveCondCommand : ICommand
    {
        public TransitionMapValue Cond { get; set; }
        public Transition Trans { get; set; }
        public void Redo()
        {
            Trans.Value.Remove(Cond);
        }
        public void Undo()
        {
            Trans.Value.Add(Cond);
        }
    }
    /// <summary>
    /// Set default node of the active fsm
    /// </summary>
    public class SetDefaultStateCommand : ICommand
    {
        public FSMStateNode Origin { get; set; }
        public FSMStateNode Final { get; set; }
        public void Redo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).SetDefault(Final);
        }
        public void Undo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).SetDefault(Origin);
        }
    }
    /// <summary>
    /// Set current state machine of fsm
    /// </summary>
    public class SetCurMachineCommand : ICommand
    {
        public FSMMachineNode Origin { get; set; }
        public FSMMachineNode Final { get; set; }
        public void Redo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench).AddRenderers(Final, true);
        }
        public void Undo()
        {
            (WorkBenchMgr.Instance.ActiveWorkBench).AddRenderers(Origin, true);
        }
    }

}
