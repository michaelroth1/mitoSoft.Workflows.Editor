﻿using System;
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
using mitoSoft.Workflows.Editor.ViewModel;
using System.Windows.Shapes;
using mitoSoft.Workflows.Editor.Helpers.Extensions;

namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Interaction logic for ViewLeftConnector.xaml
    /// </summary>
    public partial class LeftConnector : UserControl, IViewFor<ConnectorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ConnectorViewModel), typeof(LeftConnector), new PropertyMetadata(null));

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
        public LeftConnector()
        {
            InitializeComponent();
            SetupBinding();
            SetupEvents();
        }

        #region SetupBinding
        private void SetupBinding()
        {
            this.WhenActivated(disposable =>
            {

                this.OneWayBind(this.ViewModel, x => x.Name, x => x.TextBoxElement.Text).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.TextEnable, x => x.TextBoxElement.IsEnabled).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormEnable, x => x.EllipseElement.IsEnabled).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Foreground, x => x.TextBoxElement.Foreground).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormStroke, x => x.EllipseElement.Stroke).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.FormFill, x => x.EllipseElement.Fill).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Visible, x => x.LeftConnectorElement.Visibility).DisposeWith(disposable);

            });
        }
        #endregion SetupBinding

        #region SetupEvents
        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {               
                this.EllipseElement.Events().Drop.Subscribe(e => OnEventDrop(e)).DisposeWith(disposable);
                this.EllipseElement.Events().DragEnter.Subscribe(e => OnEventDragEnter(e)).DisposeWith(disposable);
                this.EllipseElement.Events().DragLeave.Subscribe(e => OnEventDragLeave(e)).DisposeWith(disposable);
            });
        }

        #endregion SetupEvents
        private void OnEventDragEnter(DragEventArgs e)
        {
            this.ViewModel.FormStroke = Application.Current.Resources["ColorConnector"] as SolidColorBrush;
            e.Handled = true;
        }
        private void OnEventDragLeave(DragEventArgs e)
        {
            this.ViewModel.FormStroke = Application.Current.Resources["ColorNodesCanvasBackground"] as SolidColorBrush;
            e.Handled = true;
        }
        private void OnEventDrop(DragEventArgs e)
        {
            this.ViewModel.FormStroke = Application.Current.Resources["ColorNodesCanvasBackground"] as SolidColorBrush;
            this.ViewModel.CommandConnectPointDrop.ExecuteWithSubscribe();
            e.Handled = true;
        }
        void UpdatePosition()
        {
            Point positionConnectPoint = EllipseElement.TranslatePoint(new Point(EllipseElement.Width/2, EllipseElement.Height / 2), this);

            NodesCanvas NodesCanvas = MyUtils.FindParent<NodesCanvas>(this);
            if (NodesCanvas == null)
                return;

            positionConnectPoint = this.TransformToAncestor(NodesCanvas).Transform(positionConnectPoint);

            //this.ViewModel.PositionConnectPoint = positionConnectPoint.Division(this.ViewModel.NodesCanvas.Scale.Value);
            this.ViewModel.PositionConnectPoint = positionConnectPoint;
        }
    }
}
