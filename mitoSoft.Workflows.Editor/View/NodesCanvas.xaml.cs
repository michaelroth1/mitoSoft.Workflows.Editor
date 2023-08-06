using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Reactive.Linq;
using System.Reactive.Disposables;

using ReactiveUI;

using mitoSoft.Workflows.Editor.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Transformations;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using ReactiveUI.Fody.Helpers;
using mitoSoft.Workflows.Editor.ViewModel;
using System.Windows.Shapes;
using System.Linq.Expressions;

namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Interaction logic for ViewNodesCanvas.xaml
    /// </summary>
    public partial class NodesCanvas : UserControl, IViewFor<NodesCanvasViewModel>, CanBeMove
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(NodesCanvasViewModel), typeof(NodesCanvas), new PropertyMetadata(null));

        public NodesCanvasViewModel ViewModel
        {
            get { return (NodesCanvasViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (NodesCanvasViewModel)value; }
        }
        #endregion ViewModel
        private Point PositionMove { get; set; }

        private Point SumMove { get; set; }
        private TypeMove Move { get; set; } = TypeMove.None;

        
        public NodesCanvas()
        {
            InitializeComponent();
            ViewModel = new NodesCanvasViewModel();
            SetupCommands();
            SetupSubscriptions();
            SetupBinding();
            SetupEvents();           
        }

        #region Setup Binding
        private void SetupBinding()
        {
            this.WhenActivated(disposable =>
            {               
                this.OneWayBind(this.ViewModel, x => x.NodesForView, x => x.Nodes.Collection).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Connects, x => x.Connects.Collection).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Selector, x => x.Selector.ViewModel).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Cutter, x => x.Cutter.ViewModel).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel.ShowAddSequenceControlItem).Subscribe(x => HandleShowContextMenu(x,ItemAddToSequence)).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel.ShowResolveSequenceControlItem).Subscribe(x => HandleShowContextMenu(x, ItemResolveSequence)).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel.ShowGoToDefinition).Subscribe(x => HandleShowContextMenu(x, ItemGoToCode)).DisposeWith(disposable);
            });
        }
        #endregion Setup Binding

        #region Setup Commands
        private void SetupCommands()
        {
            this.WhenActivated(disposable =>
            {
                this.BindCommand(this.ViewModel, x => x.CommandSelect,              x => x.BindingSelect, x => x.PositionLeft).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandCut,                 x => x.BindingCut, x => x.PositionLeft).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandAddBaseNodeWithUndoRedo, x => x.BindingAddNode, x=>x.AddNodeType).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandAddBaseNodeWithUndoRedo, x => x.ItemAddNode, x=> x.AddNodeType).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandAddBaseNodeWithUndoRedo, x => x.ItemAddParallelNode, x => x.AddParallelNodeType).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandAddBaseNodeWithUndoRedo, x => x.ItemAddSubworkflowNode, x => x.AddSubworkflowNodeType).DisposeWith(disposable);


                this.BindCommand(this.ViewModel, x => x.CommandRedo,                x => x.BindingRedo).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandUndo,                x => x.BindingUndo).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandSelectAll,           x => x.BindingSelectAll).DisposeWith(disposable);

               // this.BindCommand(this.ViewModel, x => x.CommandUndo,                x => x.ItemCollapsUp).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandSelectAll,           x => x.ItemExpandDown).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandDeleteSelectedElements, x => x.BindingDeleteSelectedElements).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandDeleteSelectedElements, x => x.ItemDelete).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandCollapseUpSelected,  x => x.ItemCollapsUp).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandExpandDownSelected,  x => x.ItemExpandDown).DisposeWith(disposable);

                //this.BindCommand(this.ViewModel, x => x.CommandItemAddToSequence, x => x.ItemAddToSequence).DisposeWith(disposable);
                //this.BindCommand(this.ViewModel, x=> x.CommandAddToSequenceWithUndoRedo, x => x.ItemResolveSequence).DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.CommandGoToCode, x => x.ItemGoToCode).DisposeWith(disposable);

            });
        }



        #endregion Setup Commands
        #region Setup Subscriptions

        private void SetupSubscriptions()
        {
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel.Selector.Size).WithoutParameter().InvokeCommand(ViewModel, x => x.CommandSelectorIntersect).DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.Cutter.EndPoint).WithoutParameter().InvokeCommand(ViewModel, x => x.CommandCutterIntersect).DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.ImagePath).Where(x => !string.IsNullOrEmpty(x)).Subscribe(value => SaveCanvasToImage(value, ImageFormats.JPEG)).DisposeWith(disposable);
                this.WhenAnyValue(x=>x.ViewModel.RenderTransformMatrix).Subscribe(value=>this.CanvasElement.RenderTransform = new MatrixTransform(value)).DisposeWith(disposable);
                //here need use ZoomIn and ZoomOut

                //this.WhenAnyValue(x=>x.ViewModel.Scale.Value).Subscribe(x=> {this.zoomBorder.ZoomDeltaTo })
            });
        }

        #endregion Setup Subscriptions
        #region Setup Events
        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.Events().MouseLeftButtonDown.Subscribe(e => OnEventMouseLeftDown(e)).DisposeWith(disposable);
                this.Events().MouseLeftButtonUp.Subscribe(e => OnEventMouseLeftUp(e));             

                this.Events().MouseUp.Subscribe(e => OnEventMouseUp(e)).DisposeWith(disposable);
                this.Events().MouseMove.Subscribe(e => OnEventMouseMove(e)).DisposeWith(disposable);
                this.BorderElement.Events().MouseWheel.Subscribe(e => OnEventMouseWheel(e)).DisposeWith(disposable);
                this.Events().DragOver.Subscribe(e => OnEventDragOver(e)).DisposeWith(disposable);               
                this.Events().PreviewMouseLeftButtonDown.Subscribe(e => OnEventPreviewMouseDown(e)).DisposeWith(disposable);
                this.Events().PreviewMouseRightButtonDown.Subscribe(e => OnEventPreviewMouseDown(e)).DisposeWith(disposable);
                this.ItemAddToSequence.Events().Click.Subscribe(e => OnItemAddToSequenceClicked(e)).DisposeWith(disposable);
                this.ItemResolveSequence.Events().Click.Subscribe(e => OnItemResolveSequenceClicked(e)).DisposeWith(disposable);
                

                this.Events().MouseRightButtonUp.Subscribe(_ => this.ViewModel.CommandHandleRightClick.ExecuteWithSubscribe()).DisposeWith(disposable);
                this.Cutter.Events().MouseLeftButtonUp.InvokeCommand(this.ViewModel.CommandDeleteSelectedConnectors).DisposeWith(disposable);
                this.Events().MouseRightButtonDown.Subscribe(_ => Keyboard.Focus(this)).DisposeWith(disposable);

            });
        }

        private void OnItemResolveSequenceClicked(RoutedEventArgs e)
        {
            var SeqNode = this.ViewModel.Nodes.Items.Where(x => x.Selected).FirstOrDefault();
            if(SeqNode != null)
            {
                this.ViewModel.CommandResolveSequenceWithUndoRedo.Execute(SeqNode);
            }            
        }

        private void OnItemAddToSequenceClicked(RoutedEventArgs e)
        {
            var SelectedNodes = this.ViewModel.Nodes.Items.Where(x => x.Selected);
            this.ViewModel.CommandAddToSequenceWithUndoRedo.Execute(SelectedNodes.ToList());
        }


        private void HandleShowContextMenu(bool show,MenuItem item)
        {
            if (show)
            {
                item.Visibility= Visibility.Visible;
            }
            else
            {
                item.Visibility= Visibility.Collapsed;
            }
        }

        private void OnEventMouseLeftDown(MouseButtonEventArgs e)
        {
            PositionMove = Mouse.GetPosition(this.CanvasElement);

            if (Mouse.Captured == null)
            {
                NodeCanvasClickMode clickMode = ViewModel.ClickMode;

                switch (clickMode)
                {
                    case NodeCanvasClickMode.Default:
                        Keyboard.ClearFocus();
                        this.CaptureMouse();
                        Keyboard.Focus(this);
                        this.ViewModel.CommandUnSelectAll.ExecuteWithSubscribe();
                        break;

                    case NodeCanvasClickMode.AddNode:
                        this.ViewModel.CommandAddBaseNodeWithUndoRedo.Execute((PositionMove, NodeType.Node));
                        break;

                    case NodeCanvasClickMode.AddParallelNode:
                        this.ViewModel.CommandAddBaseNodeWithUndoRedo.Execute((PositionMove, NodeType.ParallelNode));
                        break;

                    case NodeCanvasClickMode.AddSubWorkflowNode:
                        this.ViewModel.CommandAddBaseNodeWithUndoRedo.Execute((PositionMove, NodeType.SubWorkflowNode));
                        break;

                    case NodeCanvasClickMode.Select:
                        this.ViewModel.CommandSelect.ExecuteWithSubscribe(PositionMove);
                        break;

                    case NodeCanvasClickMode.Cut:
                        this.ViewModel.CommandCut.ExecuteWithSubscribe(PositionMove);
                        break;
                    
                    default:
                        break;
                }
            }
        }

        private void OnEventMouseLeftUp(MouseButtonEventArgs e)
        {
            if (Move == TypeMove.None)
                return;

            if (Move == TypeMove.MoveAll)
                this.ViewModel.CommandFullMoveAllNode.Execute(SumMove);
            else if (Move == TypeMove.MoveSelected)
                this.ViewModel.CommandFullMoveAllSelectedNode.Execute(SumMove);

            Move = TypeMove.None;
            SumMove = new Point();
        }

        
        private void OnEventMouseWheel(MouseWheelEventArgs e)
        {
            Point point = e.GetPosition(this.CanvasElement);
            this.ViewModel.CommandZoom.ExecuteWithSubscribe((point, e.Delta));
        }

        private void OnEventMouseUp(MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
            PositionMove = new Point();
            Keyboard.Focus(this);
        }

        private void OnEventMouseMove(MouseEventArgs e)
        {
            if (!(Mouse.Captured is CanBeMove))
                return;

            Point delta = GetDeltaMove();

            if (delta.IsClear())
                return;

            SumMove = SumMove.Addition(delta);
            if (this.IsMouseCaptured)
            {
                ViewModel.CommandPartMoveAllNode.ExecuteWithSubscribe(delta);
                Move = TypeMove.MoveAll;
            }
            else
            {
                ViewModel.CommandPartMoveAllSelectedNode.ExecuteWithSubscribe(delta);
                Move = TypeMove.MoveSelected;
            }
        }
        private void OnEventDragOver(DragEventArgs e)
        {
            Point point = e.GetPosition(this.CanvasElement);
            if (this.ViewModel.DraggedConnect != null)
            {
                point = point.Subtraction(2);
                //this.ViewModel.DraggedConnect.EndPoint = point.Division(this.ViewModel.Scale.Value);
                this.ViewModel.DraggedConnect.EndPoint = point;
            }
        }

        private void OnEventPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //this.ViewModel.PositionRight = e.GetPosition(this.CanvasElement);
        }

        private void OnEventPreviewMouseDown(MouseButtonEventArgs e)
        {
            var p = e.GetPosition(this.CanvasElement);
            this.ViewModel.PositionLeft = p;
            this.ViewModel.AddSubworkflowNodeType = (p, NodeType.SubWorkflowNode);
            this.ViewModel.AddNodeType = (p, NodeType.Node);
            this.ViewModel.AddParallelNodeType = (p, NodeType.ParallelNode);
        }

        #endregion Setup Events
        private Point GetDeltaMove()
        {
            Point CurrentPosition = Mouse.GetPosition(this.CanvasElement);
            Point result = new Point();

            if (!PositionMove.IsClear())
            {
                result = CurrentPosition.Subtraction(PositionMove);
            }

            PositionMove = CurrentPosition;
            return result;
        }

        private void SaveCanvasToImage(string filename, ImageFormats format)
        {
            //this.zoomBorder.Uniform();
            MyUtils.PanelToImage(this.CanvasElement, filename, format);
            ViewModel.CommandLogDebug.ExecuteWithSubscribe(String.Format("Scheme was exported to \"{0}\"", filename));
        }

    }
}
