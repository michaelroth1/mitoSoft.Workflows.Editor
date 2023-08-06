using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
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
    /// Interaction logic for TransitionProp.xaml
    /// </summary>
    public partial class TransitionProp : UserControl, IViewFor<ConnectorViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ConnectorViewModel), typeof(TransitionProp), new PropertyMetadata(null));

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

        public TransitionProp()
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
                this.OneWayBind(this.ViewModel, x => x.Name, x => x.tbTransiName.Text).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Node.Name, x => x.lblFromStateName.Content).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.ToConnectorNodeName, x => x.lblToStateName.Content).DisposeWith(disposable);                                 
            });
        }

        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.tbTransiName.Events().LostFocus.Subscribe(e => ValidateTransiName(e)).DisposeWith(disposable);             
            });
        }

        private void SetupCommands() { }

        private void ValidateTransiName(RoutedEventArgs e)
        {
            if (tbTransiName.Text != ViewModel.Name)
            {
                ViewModel.CommandValidateName.ExecuteWithSubscribe(tbTransiName.Text);
            }                
            if (tbTransiName.Text != ViewModel.Name)
            {
                tbTransiName.Text = ViewModel.Name;
            }            
        }
    }
}

