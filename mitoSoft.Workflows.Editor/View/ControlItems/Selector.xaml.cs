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


namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Interaction logic for ViewSelector.xaml
    /// </summary>
    public partial class Selector : UserControl, IViewFor<SelectorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel),
            typeof(SelectorViewModel), typeof(Selector), new PropertyMetadata(null));

        public SelectorViewModel ViewModel
        {
            get { return (SelectorViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (SelectorViewModel)value; }
        }
        #endregion ViewModel
        public Selector()
        {
            InitializeComponent();
            SetupBinding();
            SetupEvents();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

        }
        #region Setup Binding 
        private void SetupBinding()
        {
            this.WhenActivated(disposable =>
            {
                this.OneWayBind(this.ViewModel, x => x.Visible, x => x.Visibility).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Size.Width, x => x.Rectangle.Width).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Size.Height, x => x.Rectangle.Height).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Point1.X, x => x.TranslateTransformElement.X).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Point1.Y, x => x.TranslateTransformElement.Y).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Scale.Scales.X, x => x.ScaleTransformElement.ScaleX).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Scale.Scales.Y, x => x.ScaleTransformElement.ScaleY).DisposeWith(disposable);

                this.WhenAnyValue(x => x.Visibility).Where(IsVisible => IsVisible==Visibility.Visible).Subscribe(_ => Update()).DisposeWith(disposable);
            });
        }

        #endregion Setup Binding 

        #region Setup Events

        private void Update()
        {
            Mouse.Capture(this);
            Keyboard.Focus(this);
        }
        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.Events().MouseMove.Subscribe(e => OnMouseMoves(e));
                this.Events().MouseLeftButtonUp.Subscribe(e => OnMouseLeftButtonUp(e));

            });
        }

        private void OnMouseMoves(MouseEventArgs e)
        {
            //find canvas
            NodesCanvas NodesCanvas = MyUtils.FindParent<NodesCanvas>(this);
            ViewModel.Point2 = e.GetPosition(NodesCanvas.CanvasElement);

            e.Handled = true;
        }
        private void OnMouseLeftButtonUp(MouseEventArgs e)
        {
            this.ViewModel.Visible = null;
        }

        #endregion Setup Events


    }
}
