using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// WorkBenchFrame.xaml 的交互逻辑
    /// </summary>
    public abstract class WorkBenchFrame : UserControl
    {
        public PageData CurPageData { get; } = new PageData();
        public abstract FrameworkElement GetCanvasBoard { get; }
        public abstract FrameworkElement GetCanvas { get; }
        Operation m_Operation;

        protected void _Init()
        {

            m_Operation = new Operation(this.GetCanvasBoard);
            m_Operation.RegisterMiddleDrag(_OnDrag, null, null);
            m_Operation.RegisterLeftClick(_OnClick);

            GetCanvas.RenderTransform = CurPageData.TransGroup;
        }

        public void Enable()
        {
            EventMgr.Instance.Register(EventType.NewNodeAdded, _OnNewNodeAdded);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.CommentCreated, _OnCommentCreated);
            EventMgr.Instance.Register(EventType.MakeCenter, _OnMakeCenter);
            Focus();
        }

        public void Disable()
        {
            EventMgr.Instance.Unregister(EventType.NewNodeAdded, _OnNewNodeAdded);
            EventMgr.Instance.Unregister(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Unregister(EventType.CommentCreated, _OnCommentCreated);
            EventMgr.Instance.Unregister(EventType.MakeCenter, _OnMakeCenter);
        }

        public virtual void OnWorkBenchLoaded(WorkBench bench)
        {
            ClearCanvas();
        }

        public virtual void OnWorkBenchSelected()
        {

        }

        protected virtual void _OnTickResult(EventArg arg)
        {
        }

        protected virtual void _OnDebugTargetChanged(EventArg arg)
        {

        }

        private void _OnNewNodeAdded(EventArg arg)
        {
            NewNodeAddedArg oArg = arg as NewNodeAddedArg;
            if (oArg.Node == null)
                return;

            ///> move the node to the topleft of the canvas
            if (oArg.From != NewNodeAddedArg.AddMethod.Duplicate)
                oArg.Node.Renderer.SetPos(new Point(
                    -CurPageData.TranslateTransform.X / CurPageData.ScaleTransform.ScaleX,
                    -CurPageData.TranslateTransform.Y / CurPageData.ScaleTransform.ScaleY));

            //_CreateNode(oArg.Node);
            //this.Canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action<Node>(_ThreadRefreshConnection), oArg.Node);
        }

        private void _OnCommentCreated(EventArg arg)
        {
            CommentCreatedArg oArg = arg as CommentCreatedArg;
            if (oArg.Comment == null)
                return;

            ///> move the comment to the topleft of the canvas
            oArg.Comment.Geo.Pos = new Point(
                -CurPageData.TranslateTransform.X / CurPageData.ScaleTransform.ScaleX,
                -CurPageData.TranslateTransform.Y / CurPageData.ScaleTransform.ScaleY);
            oArg.Comment.OnGeometryChanged();
        }

        public void ClearCanvas()
        {
            //RenderMgr.Instance.ClearNodes();
        }

        private void _MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CurPageData == null || e.Delta == 0)
                return;

            Point pos = e.GetPosition(this.GetCanvasBoard);
            Point oldPos = new Point(CurPageData.TranslateTransform.X, CurPageData.TranslateTransform.Y);

            double width = this.GetCanvas.ActualWidth;
            double height = this.GetCanvas.ActualHeight;

            double oldWidth = width * CurPageData.ScaleTransform.ScaleX;
            double oldHeight = height * CurPageData.ScaleTransform.ScaleY;

            double rateX = (pos.X - oldPos.X) / oldWidth;
            double rateY = (pos.Y - oldPos.Y) / oldHeight;

            double delta = (e.Delta / Math.Abs(e.Delta) * 0.1);
            CurPageData.ScaleTransform.ScaleX *= (1.0 + delta);
            CurPageData.ScaleTransform.ScaleY *= (1.0 + delta);

            double deltaX = (width * CurPageData.ScaleTransform.ScaleX - oldWidth) * rateX;
            double deltaY = (height * CurPageData.ScaleTransform.ScaleY - oldHeight) * rateY;

            CurPageData.TranslateTransform.X -= deltaX;
            CurPageData.TranslateTransform.Y -= deltaY;
        }


        void _OnDrag(Vector delta, Point pos)
        {
            if (CurPageData == null)
                return;

            CurPageData.TranslateTransform.X += delta.X;
            CurPageData.TranslateTransform.Y += delta.Y;
        }
        void _OnClick()
        {
            Focus();
            SelectionMgr.Instance.Clear();
        }
        public void ResetTransform()
        {
            if (CurPageData == null)
                return;
            CurPageData.ScaleTransform.ScaleX = 0;
            CurPageData.ScaleTransform.ScaleY = 0;
            CurPageData.TranslateTransform.X = 0;
            CurPageData.TranslateTransform.Y = 0;
        }

        private void _OnMakeCenter(EventArg arg)
        {
            if (CurPageData == null)
                return;

            MakeCenterArg oArg = arg as MakeCenterArg;
            if (oArg.Target == null)
            {
                m_MakingCenterDes.X = 0;
                m_MakingCenterDes.Y = 0;

                CompositionTarget.Rendering -= AutoMakingCenter;
                CompositionTarget.Rendering += AutoMakingCenter;
            }
            else
            {
                Vector halfcanvas = new Vector(this.GetCanvasBoard.ActualWidth / 2, this.GetCanvasBoard.ActualHeight / 2);
                Point curPos = new Point(0, 0) + halfcanvas;
                double nodesScale = CurPageData.ScaleTransform.ScaleX;
                Point pos = new Vector(-oArg.Target.Owner.Geo.Pos.X * nodesScale, -oArg.Target.Owner.Geo.Pos.Y * nodesScale) + curPos;
                m_MakingCenterDes = pos;

                CompositionTarget.Rendering -= ManualMakingCenter;
                CompositionTarget.Rendering += ManualMakingCenter;
            }
        }

        Point m_MakingCenterDes;
        private void ManualMakingCenter(object sender, EventArgs e)
        {
            if (CurPageData == null)
                return;

            if (_MakingCenter())
            {
                CompositionTarget.Rendering -= ManualMakingCenter;
            }
        }

        private void AutoMakingCenter(object sender, EventArgs e)
        {
            if (CurPageData == null)
                return;

            Point newDes = new Point();
            if (_FindCenterPos(ref newDes))
            {
                m_MakingCenterDes = newDes;
            }

            if (_MakingCenter())
            {
                CompositionTarget.Rendering -= AutoMakingCenter;
            }
        }

        bool _MakingCenter()
        {
            Point curPos = new Point(CurPageData.TranslateTransform.X, CurPageData.TranslateTransform.Y);
            if ((curPos - m_MakingCenterDes).LengthSquared < 1)
            {
                return true;
            }

            Vector delta = m_MakingCenterDes - curPos;
            double sqrLength = delta.LengthSquared;
            double speed = 30.0;
            if (sqrLength > speed * speed)
            {
                delta = delta / 4;
            }
            CurPageData.TranslateTransform.X += delta.X;
            CurPageData.TranslateTransform.Y += delta.Y;

            return false;
        }

        bool _FindCenterPos(ref Point newDes)
        {
            Vector halfcanvas = new Vector(this.GetCanvasBoard.ActualWidth / 2, this.GetCanvasBoard.ActualHeight / 2);
            Point curPos = new Point(0, 0) + halfcanvas;
            Point nodesPos = new Point(CurPageData.TranslateTransform.X, CurPageData.TranslateTransform.Y);
            double nodesScale = CurPageData.ScaleTransform.ScaleX;
            double sqrradius = Math.Max(halfcanvas.X, halfcanvas.Y);
            sqrradius *= sqrradius;

            Point nextPos = new Point(0, 0);
            int count = 0;

            foreach (NodeBaseRenderer renderer in WorkBenchMgr.Instance.ActiveWorkBench.NodeList.Collection)
            {
                Point pos = new Vector(renderer.Owner.Geo.Pos.X * nodesScale, renderer.Owner.Geo.Pos.Y * nodesScale) + nodesPos;
                if ((pos - curPos).LengthSquared < sqrradius)
                {
                    ///> Much Larger Weight
                    count += 50;
                    nextPos.X += (pos.X * 50);
                    nextPos.Y += (pos.Y * 50);
                }
                else
                {
                    ///> Normal Weight
                    ++count;
                    nextPos.X += (pos.X);
                    nextPos.Y += (pos.Y);
                }
            }

            if (count > 0)
            {
                nextPos.X /= count;
                nextPos.Y /= count;

                Vector delta = nextPos - curPos;

                newDes.X = CurPageData.TranslateTransform.X - delta.X;
                newDes.Y = CurPageData.TranslateTransform.Y - delta.Y;
                return true;
            }
            return false;
        }
    }
}
