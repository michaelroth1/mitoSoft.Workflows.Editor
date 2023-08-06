using DynamicData;
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
    public partial class SequenceNodeViewProperties : UserControl,IViewFor<SequenceNodeViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(SequenceNodeViewModel), typeof(SequenceNodeViewProperties), new PropertyMetadata(null));

        public SequenceNodeViewModel ViewModel
        {
            get { return (SequenceNodeViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (SequenceNodeViewModel)value; }
        }

        public SequenceNodeViewProperties()
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
                this.OneWayBind(this.ViewModel, x => x.SequenceNodes, x => x.SequenceStates.ItemsSource).DisposeWith(disposable);
                this.OneWayBind(this.ViewModel, x => x.ConnectedTransitionsForView, x => x.lvTransitions.ItemsSource).DisposeWith(disposable);
            });
        }

        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.lblNodeName.Events().LostFocus.Subscribe(e => ValidateStateName(e)).DisposeWith(disposable);
            });
        }

        private void SetupCommands() { }

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
