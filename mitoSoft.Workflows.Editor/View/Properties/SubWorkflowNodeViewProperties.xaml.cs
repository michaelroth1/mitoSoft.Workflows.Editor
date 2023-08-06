using DynamicData;
using mitoSoft.StateMachine.AdvancedStateMachines;
using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for SequenceNodeViewProperties.xaml
    /// </summary>
    public partial class SubWorkflowNodeProperties : UserControl,IViewFor<SubWorkflowNodeViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(SubWorkflowNodeViewModel), typeof(SubWorkflowNodeProperties), new PropertyMetadata(null));

        public SubWorkflowNodeViewModel ViewModel
        {
            get { return (SubWorkflowNodeViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (SubWorkflowNodeViewModel)value; }
        }

        public SubWorkflowNodeProperties()
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
                
                this.OneWayBind(this.ViewModel, x => x.SubStateMachine, x => x.lblStateMachine.Content).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.ConnectedTransitionsForView, x => x.lvTransitions.ItemsSource).DisposeWith(disposable);
            });
        }

        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.lblNodeName.Events().LostFocus.Subscribe(e => ValidateStateName(e)).DisposeWith(disposable);

                this.lblStateMachine.Events().MouseDoubleClick.Subscribe(e => DoubleClickSubState(e)).DisposeWith(disposable);
            });
        }

        private void DoubleClickSubState(MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(lblStateMachine.Content.ToString()) )
            {               
                ViewModel.NodesCanvas.MainWindowViewModel.CommandOpenChildWindow.ExecuteWithSubscribe(lblStateMachine.Content.ToString());
            }
        }

        private void SetupCommands() 
        {
            this.WhenActivated(disposable =>
            {                  
                this.BindCommand(this.ViewModel, x => x.CommandRemoveSubState, x => x.btnRemove).DisposeWith(disposable);
                
                this.BindCommand(this.ViewModel, x => x.CommandAddSubStateNewWindow, x => x.btnAdd).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandOpenSubState, x => x.btnOpen).DisposeWith(disposable);
            });
        }

        private void ValidateStateName(RoutedEventArgs e)
        {
            if (lblNodeName.Text != ViewModel.Name)
            {
                ViewModel.CommandValidateName.ExecuteWithSubscribe(lblNodeName.Text);
            }                
            if (lblNodeName.Text != ViewModel.Name)
            {
                lblNodeName.Text = ViewModel.Name;
            }                
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
