using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.ViewModel;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Логика взаимодействия для DoubleClickParaStates.xaml
    /// </summary>
    public partial class TableOfTransitionsItem : UserControl, IViewFor<ConnectorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ConnectorViewModel), typeof(TableOfTransitionsItem), new PropertyMetadata(null));

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
        public TableOfTransitionsItem()
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
                this.OneWayBind(this.ViewModel, x => x.Node.Name, x => x.TextBoxElementStateFrom.Text).DisposeWith(disposable);
                this.OneWayBind(this.ViewModel, x => x.Name, x => x.TextBoxElementTransitionName.Text).DisposeWith(disposable);
                this.OneWayBind(this.ViewModel, x => x.ToConnectorNodeName, x => x.TextBoxElementStateTo.Text).DisposeWith(disposable);
            });

        }
        #endregion SetupBinding

        #region SetupEvents

        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.TextBoxElementTransitionName.Events().LostFocus.Subscribe(e => ValidateTransitionName(e)).DisposeWith(disposable);
                this.TextBoxElementStateFrom.Events().LostFocus.Subscribe(e => ValidateStateFrom(e)).DisposeWith(disposable);
                this.TextBoxElementStateTo.Events().LostFocus.Subscribe(e => ValidateStateTo(e)).DisposeWith(disposable);
            });
        }
        private void ValidateTransitionName(RoutedEventArgs e)
        {
            if (TextBoxElementTransitionName.Text != ViewModel.Name)
                ViewModel.CommandValidateName.ExecuteWithSubscribe(TextBoxElementTransitionName.Text);
            if (TextBoxElementTransitionName.Text != ViewModel.Name)
                TextBoxElementTransitionName.Text = ViewModel.Name;
        }
        private void ValidateStateFrom(RoutedEventArgs e)
        {
            if (TextBoxElementStateFrom.Text != ViewModel.Node.Name)
                ViewModel.Node.CommandValidateName.ExecuteWithSubscribe(TextBoxElementStateFrom.Text);
            if (TextBoxElementStateFrom.Text != ViewModel.Node.Name)
                TextBoxElementStateFrom.Text = ViewModel.Node.Name;
        }
        private void ValidateStateTo(RoutedEventArgs e)
        {
            if (this.ViewModel.ItsLoop)
                ValidateStateToLoop();
            else
                ValidateStateTo();
        }
        private void ValidateStateToLoop()
        {
            if (TextBoxElementStateTo.Text != ViewModel.Node.Name)
                this.ViewModel.Node.CommandValidateName.ExecuteWithSubscribe(TextBoxElementStateTo.Text);
            if (TextBoxElementStateTo.Text != ViewModel.Node.Name)
                TextBoxElementStateTo.Text = ViewModel.Node.Name;
        }
        private void ValidateStateTo()
        {
            if (TextBoxElementStateTo.Text != this.ViewModel.ToConnectorNodeName)
                this.ViewModel.Connect.ToConnector.Node.CommandValidateName.ExecuteWithSubscribe(TextBoxElementStateTo.Text);
            if (TextBoxElementStateTo.Text != this.ViewModel.ToConnectorNodeName)
                TextBoxElementStateTo.Text = this.ViewModel.ToConnectorNodeName;
        }
        #endregion SetupEvents
    }
}
