using DynamicData;
using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.ViewModel;

using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Interaction logic for ParallelNodeViewProperties.xaml
    /// </summary>
    public partial class ParallelNodeViewProperties : UserControl, IViewFor<ParallelNodeViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ParallelNodeViewModel), typeof(ParallelNodeViewProperties), new PropertyMetadata(null));

        public ParallelNodeViewModel ViewModel
        {
            get { return (ParallelNodeViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ParallelNodeViewModel)value; }
        }

        public ParallelNodeViewProperties()
        {
            InitializeComponent();
            SetupBinding();
            SetupEvents();
            SetupCommands();
        }

        private void SetupBinding()
        {
            this.WhenActivated(disposable =>
            {
                this.OneWayBind(this.ViewModel, x => x.Name, x => x.lblNodeName.Text).DisposeWith(disposable);
               
                this.OneWayBind(this.ViewModel, x=> x.ParallelStates, x=>x.ParalellStates.ItemsSource).DisposeWith(disposable);
               
                this.OneWayBind(this.ViewModel, x => x.ConnectedTransitionsForView, x => x.lvTransitions.ItemsSource).DisposeWith(disposable);

            });
        }

        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.lblNodeName.Events().LostFocus.Subscribe(e => ValidateStateName(e)).DisposeWith(disposable);

                this.ParalellStates.Events().MouseDoubleClick.Subscribe(e => DoubleClickParaStates(e)).DisposeWith(disposable);
            });
        }

        private void DoubleClickParaStates(MouseButtonEventArgs e)
        {
            if (ParalellStates.SelectedItem != null)
            {
                var ParallelWorkflow = ParalellStates.SelectedItem.ToString();

                ViewModel.NodesCanvas.MainWindowViewModel.CommandOpenChildWindow.ExecuteWithSubscribe(ParallelWorkflow);
            }
        }

        private void SetupCommands() 
        {
            this.WhenActivated(disposable =>
            {
                var SelectedItem = this.ObservableForProperty(x => x.ParalellStates.SelectedItem).Select(x => (x.Value as string));
                
                this.BindCommand(this.ViewModel, x => x.CommandRemoveParallelState, x => x.btnRemove, SelectedItem).DisposeWith(disposable);
                
                this.BindCommand(this.ViewModel, x => x.CommandAddParallelStateNewWindow, x => x.btnAdd).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandOpenParallelState, x => x.btnOpen).DisposeWith(disposable);
            });
        }

        private void ValidateStateName(RoutedEventArgs e)
        {
            if (lblNodeName.Text != ViewModel.Name)
                ViewModel.CommandValidateName.ExecuteWithSubscribe(lblNodeName.Text);
            if (lblNodeName.Text != ViewModel.Name)
                lblNodeName.Text = ViewModel.Name;
        }

        private void ButtonUpClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            var connector = btn.DataContext as ConnectorViewModel;

            ViewModel.CommandMoveTransitionUp.ExecuteWithSubscribe(connector);
        }

        private void ButtonDownClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            var connector = btn.DataContext as ConnectorViewModel;

            ViewModel.CommandMoveTransitionDown.ExecuteWithSubscribe(connector);
        }
    }
}
