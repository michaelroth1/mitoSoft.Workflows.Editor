﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Controls.Primitives;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using ReactiveUI;

using mitoSoft.Workflows.Editor.Helpers;
using mitoSoft.Workflows.Editor.ViewModel;
using mitoSoft.Workflows.Editor.Helpers.Transformations;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System.Collections.Generic;

namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Interaction logic for ViewConnector.xaml
    /// </summary>
    public partial class RightConnector : UserControl, IViewFor<ConnectorViewModel>, CanBeMove
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ConnectorViewModel), typeof(RightConnector), new PropertyMetadata(null));

        public ConnectorViewModel ViewModel
        {
            get { return (ConnectorViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ConnectorViewModel)value; }
        }
        #endregion ViewModel
        public RightConnector()
        {
            InitializeComponent();
            SetupBinding();
            SetupEvents();
            SetupSubcriptions(); 
        }

        #region SetupBinding
        private void SetupBinding()
        {
            this.WhenActivated(disposable =>
            {

                Canvas.SetZIndex((UIElement)this.VisualParent, this.ViewModel.Node.Zindex+2);

                this.OneWayBind(this.ViewModel, x => x.Visible, x => x.RightConnectorElement.Visibility).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Name, x => x.TextBoxElement.Text).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.TextEnable, x => x.TextBoxElement.IsEnabled).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Foreground, x => x.TextBoxElement.Foreground).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormEnable, x => x.EllipseElement.IsEnabled).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormStroke, x => x.EllipseElement.Stroke).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormFill, x => x.EllipseElement.Fill).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormStrokeThickness, x => x.EllipseElement.StrokeThickness).DisposeWith(disposable);


            });
        }
        #endregion SetupBinding

        #region Setup Subcriptions
        private void SetupSubcriptions()
        {
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.EllipseElement.IsMouseOver).Subscribe(value => OnEventMouseOver(value)).DisposeWith(disposable);
            });
        }
        #endregion Setup Subcriptions

        #region SetupEvents

        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.EllipseElement.Events().MouseLeftButtonDown.Subscribe(e => ConnectDrag(e)).DisposeWith(disposable);
                this.TextBoxElement.Events().LostFocus.Subscribe(e => Validate(e)).DisposeWith(disposable);
                this.GridElement.Events().PreviewMouseLeftButtonDown.Subscribe(e => ConnectorDrag(e)).DisposeWith(disposable);
                this.GridElement.Events().PreviewDragEnter.Subscribe(e => ConnectorDragEnter(e)).DisposeWith(disposable);
                this.GridElement.Events().PreviewDrop.Subscribe(e => ConnectorDrop(e)).DisposeWith(disposable);
            });
        }
        private void OnEventMouseOver(bool value)
        {
                this.ViewModel.FormStroke = value ? Application.Current.Resources["ColorConnector"] as SolidColorBrush
                                                 : Application.Current.Resources["ColorNodesCanvasBackground"] as SolidColorBrush;
        }
        private void Validate(RoutedEventArgs e)
        {
            if (TextBoxElement.Text != ViewModel.Name)
                ViewModel.CommandValidateName.ExecuteWithSubscribe(TextBoxElement.Text);
            if (TextBoxElement.Text != ViewModel.Name)
                TextBoxElement.Text = ViewModel.Name;
        }

        private void ConnectDrag(MouseButtonEventArgs e)
        {
            //this.ViewModel.Selected = !this.ViewModel.Selected;
            if (this.ViewModel.NodesCanvas.ClickMode == NodeCanvasClickMode.Default)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    this.ViewModel.CommandSetAsLoop.ExecuteWithSubscribe();
                    this.ViewModel.NodesCanvas.CommandAddConnectorWithConnect.Execute(this.ViewModel);
                }
                else
                {
                    this.ViewModel.CommandConnectPointDrag.ExecuteWithSubscribe();
                    DataObject data = new DataObject();
                    data.SetData("Node", this.ViewModel.Node);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                    this.ViewModel.CommandCheckConnectPointDrop.ExecuteWithSubscribe();
                    e.Handled = true;
                }
            }
        }

        private void ConnectorDrag(MouseButtonEventArgs e)
        {
            if (this.ViewModel.NodesCanvas.ClickMode == NodeCanvasClickMode.Default)
            {
                if (!this.ViewModel.TextEnable)
                    return;
                if (Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    this.ViewModel.CommandConnectorDrag.ExecuteWithSubscribe();
                    DataObject data = new DataObject();
                    data.SetData("Connector", this.ViewModel);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }
                else if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    this.ViewModel.CommandSelect.ExecuteWithSubscribe(SelectMode.ClickWithShift);
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    this.ViewModel.CommandSelect.ExecuteWithSubscribe(SelectMode.ClickWithCtrl);
                }
                else
                {
                    this.ViewModel.CommandSelect.ExecuteWithSubscribe(SelectMode.Click);
                    return;
                }
            } 
            else if (this.ViewModel.NodesCanvas.ClickMode == NodeCanvasClickMode.Cut)
            {
                if (this.ViewModel != this.ViewModel.Node.CurrentConnector)
                    this.ViewModel.NodesCanvas.CommandDeleteSelectedConnectors.Execute(new List<ConnectorViewModel>() {this.ViewModel });
            }
            else if (this.ViewModel.NodesCanvas.ClickMode == NodeCanvasClickMode.Select)
            {
                this.ViewModel.CommandSelect.ExecuteWithSubscribe(SelectMode.ClickWithCtrl);
            }
                e.Handled = true;
           
        }

        private void ConnectorDragEnter(DragEventArgs e)
        {
            if (this.ViewModel.NodesCanvas.ConnectorPreviewForDrop == null)
                return;

            if (this.ViewModel.NodesCanvas.ConnectorPreviewForDrop == this.ViewModel)
                return;
            this.ViewModel.CommandConnectorDragEnter.ExecuteWithSubscribe();
            e.Handled = true;
        }
        private void ConnectorDrop(DragEventArgs e)
        {
            if (this.ViewModel.NodesCanvas.ConnectorPreviewForDrop == null)
                return;
           
            this.ViewModel.CommandConnectorDrop.ExecuteWithSubscribe();

            e.Handled = true;
        }

        #endregion SetupEvents


        private  void UpdatePosition()
        {
            Point positionConnectPoint;

            if((!ViewModel.Node.IsCollapse)||(ViewModel.Node.IsCollapse && this.ViewModel.Name == "Output"))
            {
                positionConnectPoint = EllipseElement.TranslatePoint(new Point(EllipseElement.Width/2, EllipseElement.Height / 2), this);

                NodesCanvas NodesCanvas = MyUtils.FindParent<NodesCanvas>(this);

                positionConnectPoint = this.TransformToAncestor(NodesCanvas).Transform(positionConnectPoint);

            }
            else
            {
                positionConnectPoint = this.ViewModel.Node.Output.PositionConnectPoint;

            }

            if (this.ViewModel.Name == "Output")
            {
                this.ViewModel.NodesCanvas.LogDebug(positionConnectPoint.ToString());
            }
            this.ViewModel.PositionConnectPoint = positionConnectPoint;
        }

    }
}
