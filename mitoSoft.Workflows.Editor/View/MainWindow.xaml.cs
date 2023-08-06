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
using System.Windows.Markup;
using mitoSoft.Workflows.Editor.ViewModel;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using System.IO;
using System.Linq;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using DynamicData.Binding;
using DynamicData;
using System.Diagnostics;

using System.Net.WebSockets;
using System.Reactive;

namespace mitoSoft.Workflows.Editor.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<MainWindowViewModel>
    {
        #region ViewModel
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(MainWindowViewModel), typeof(MainWindow), new PropertyMetadata(null));

        public MainWindowViewModel ViewModel
        {
            get { return (MainWindowViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MainWindowViewModel)value; }
        }
        #endregion ViewModel

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(this.NodesCanvas.ViewModel);
            this.NodesCanvas.ViewModel.MainWindowViewModel = this.ViewModel;
            Init();
        }

        public MainWindow(MainWindowViewModel parent)
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(this.NodesCanvas.ViewModel, parent);
            this.NodesCanvas.ViewModel.MainWindowViewModel = this.ViewModel;
            Init();
        }

        public MainWindow(MainWindowViewModel parent, string openFile)
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(this.NodesCanvas.ViewModel, parent);
            this.NodesCanvas.ViewModel.MainWindowViewModel = this.ViewModel;
            Init();
            ViewModel.NodesCanvas.CommandLoadScheme.ExecuteWithSubscribe(openFile);
        }

        private void Init()
        {
            this.DataContext = ViewModel;
            SetupSubscriptions();
            SetupBinding();
            SetupEvents();
            SetupCommands();
        }

        #region Setup Binding

        private void SetupBinding()
        {

            this.WhenActivated(disposable =>
            {              
                this.OneWayBind(this.ViewModel, x => x.NodesCanvas.Dialog, x => x.Dialog.ViewModel).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Transitions, x => x.TableOfTransitions.ItemsSource).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.Messages, x => x.MessageList.ItemsSource).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.DebugEnable, x => x.LabelDebug.Visibility).DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.CountError, x => x.LabelError.Content, value => value.ToString() + " Error").DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.CountWarning, x => x.LabelWarning.Content, value => value.ToString() + " Warning").DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.CountInformation, x => x.LabelInformation.Content, value => value.ToString() + " Information").DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.CountDebug, x => x.LabelDebug.Content, value => value.ToString() + " Debug").DisposeWith(disposable);

            });
        }
        #endregion Setup Binding

        #region Setup Subscriptions

        private void SetupSubscriptions()
        {
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel.NodesCanvas.SchemePath).Subscribe(value => UpdateSchemeName(value)).DisposeWith(disposable);

                //this.WhenAnyValue(x => x.NodesCanvas.ViewModel.NeedExit).Where(x => x).Subscribe(_ => this.Close()).DisposeWith(disposable);

                this.WhenAnyValue(x => x.NodesCanvas.ViewModel.NeedExit).Where(x => x).Subscribe(_ => CloseChildWindow()).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel.CountError).Buffer(2, 1).Where(x => x[1] > x[0]).Subscribe(_ => ShowError()).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ViewModel.NodesCanvas.Theme).Subscribe(_ => UpdateButton()).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ActualWidth).Subscribe(value => TableOfTransitionsColumn.MaxWidth = value - 50).DisposeWith(disposable);

                this.WhenAnyValue(x => x.ActualHeight).Subscribe(value => Fotter.MaxHeight = value - 150).DisposeWith(disposable);

                this.WhenAnyValue(x => x.NodesCanvas.ViewModel.SelectedNode).Subscribe(x => SelectedNodeChanged(x)).DisposeWith(disposable);

                this.WhenAnyValue(x => x.NodesCanvas.ViewModel.SelectedTransition).Subscribe(x => SelectedTransitionChanged(x)).DisposeWith(disposable);

            });
        }

        private void CloseChildWindow()
        {
            var node = this.ViewModel.ParentWindow?.NodesCanvas.SelectedNode;
            if (node is ParallelNodeViewModel)
            {
                (node as ParallelNodeViewModel).CommandAddParallelState.ExecuteWithSubscribe(ViewModel.NodesCanvas.SchemePath);
            }
            else if (node is SubWorkflowNodeViewModel)
            {
                (node as SubWorkflowNodeViewModel).SubStateMachine = ViewModel.NodesCanvas.SchemePath;
            }
            this.Close();
        }


        private void SelectedNodeChanged(BaseNodeViewModel x)
        {
            if (x == null)
            {
                ViewModel.PropertyView = null;
            }
            else if (x is NodeViewModel)
            {
                ViewModel.SelectedNodeViewModel = (NodeViewModel)x;

                ViewModel.PropertyView = ViewModel.SelectedNodeViewModel;
            }
            else if (x is ParallelNodeViewModel)
            {
                ViewModel.SelectedNodeViewModel = (ParallelNodeViewModel)x;

                ViewModel.PropertyView = ViewModel.SelectedNodeViewModel;
            }
            else if (x is SequenceNodeViewModel)
            {
                ViewModel.SelectedNodeViewModel = (SequenceNodeViewModel)x;

                ViewModel.PropertyView = ViewModel.SelectedNodeViewModel;
            }
            else if (x is SubWorkflowNodeViewModel)
            {
                ViewModel.SelectedNodeViewModel = (SubWorkflowNodeViewModel)x;

                ViewModel.PropertyView = ViewModel.SelectedNodeViewModel;
            }
        }

        private void SelectedTransitionChanged(ConnectorViewModel x)
        {
            if (x == null)
            {
                ViewModel.PropertyView = null;
            }
            else
            {
                ViewModel.SelectedTransitionViewModel = x;

                ViewModel.PropertyView = ViewModel.SelectedTransitionViewModel;
            }
        }

        private void UpdateSchemeName(string newName)
        {
            this.LabelSchemeName.Visibility = string.IsNullOrEmpty(newName) ? Visibility.Hidden : Visibility.Visible;
            if (!string.IsNullOrEmpty(newName))
            {
                this.LabelSchemeName.Content = Path.GetFileNameWithoutExtension(newName);
                this.LabelSchemeName.ToolTip = newName;
            }
        }
        #endregion Setup Subscriptions

        #region SetupEvents
        private void SetupEvents()
        {
            this.WhenActivated(disposable =>
            {
                this.MessageList.Events().MouseDoubleClick.Subscribe(_ => ViewModel.CommandCopyError.ExecuteWithSubscribe((MessageList.SelectedItem as MessageViewModel)?.Text)).DisposeWith(disposable);

                this.LabelSchemeName.Events().MouseDoubleClick.WithoutParameter().InvokeCommand(ViewModel.CommandCopySchemeName).DisposeWith(disposable);


                this.Header.Events().PreviewMouseLeftButtonDown.Subscribe(e => HeaderClick(e)).DisposeWith(disposable);

                this.ButtonMin.Events().Click.Subscribe(e => ButtonMinClick(e)).DisposeWith(disposable);

                this.ButtonMax.Events().Click.Subscribe(e => ButtonMaxClick(e)).DisposeWith(disposable);


                this.ErrorListExpander.Events().Collapsed.Subscribe(_ => ErrorListCollapse()).DisposeWith(disposable);

                this.ErrorListExpander.Events().Expanded.Subscribe(_ => ErrorListExpanded()).DisposeWith(disposable);

                this.TableOfTransitionsExpander.Events().Collapsed.Subscribe(_ => TableOfTransitionsCollapse()).DisposeWith(disposable);

                this.TableOfTransitionsExpander.Events().Expanded.Subscribe(_ => TableOfTransitionsExpanded()).DisposeWith(disposable);


                this.LabelError.Events().PreviewMouseLeftButtonDown.Subscribe(e => SetDisplayMessageType(e, TypeMessage.Error)).DisposeWith(disposable);

                this.LabelWarning.Events().PreviewMouseLeftButtonDown.Subscribe(e => SetDisplayMessageType(e, TypeMessage.Warning)).DisposeWith(disposable);

                this.LabelInformation.Events().PreviewMouseLeftButtonDown.Subscribe(e => SetDisplayMessageType(e, TypeMessage.Information)).DisposeWith(disposable);

                this.LabelDebug.Events().PreviewMouseLeftButtonDown.Subscribe(e => SetDisplayMessageType(e, TypeMessage.Debug)).DisposeWith(disposable);

                this.LabelErrorList.Events().PreviewMouseLeftButtonDown.Subscribe(e => SetDisplayMessageType(e, TypeMessage.All)).DisposeWith(disposable);

                this.LabelErrorListUpdate.Events().MouseLeftButtonDown.WithoutParameter().InvokeCommand(NodesCanvas.ViewModel.CommandErrorListUpdate).DisposeWith(disposable);


                this.ButtonAddNode.Events().PreviewMouseLeftButtonDown.Subscribe(e => RadioButtonUnChecked(ButtonAddNode, NodeCanvasClickMode.AddNode, e)).DisposeWith(disposable);

                this.ButtonDeleteNode.Events().PreviewMouseLeftButtonDown.Subscribe(e => RadioButtonUnChecked(ButtonDeleteNode, NodeCanvasClickMode.Delete, e)).DisposeWith(disposable);

                this.ButtonStartSelect.Events().PreviewMouseLeftButtonDown.Subscribe(e => RadioButtonUnChecked(ButtonStartSelect, NodeCanvasClickMode.Select, e)).DisposeWith(disposable);

                this.ButtonStartCut.Events().PreviewMouseLeftButtonDown.Subscribe(e => RadioButtonUnChecked(ButtonStartCut, NodeCanvasClickMode.Cut, e)).DisposeWith(disposable);

                this.ButtonAddParallelNode.Events().PreviewMouseLeftButtonDown.Subscribe(e => RadioButtonUnChecked(ButtonAddParallelNode, NodeCanvasClickMode.AddParallelNode, e)).DisposeWith(disposable);

                this.ButtonAddSubWorkflowNode.Events().PreviewMouseLeftButtonDown.Subscribe(e => RadioButtonUnChecked(ButtonAddSubWorkflowNode, NodeCanvasClickMode.AddSubWorkflowNode, e)).DisposeWith(disposable);
            });
        }

        private void SetDisplayMessageType(MouseButtonEventArgs e, TypeMessage typeMessage)
        {
            if ((ErrorListExpander.IsExpanded) && (this.ViewModel.NodesCanvas.DisplayMessageType != typeMessage))
                e.Handled = true;

            this.ViewModel.NodesCanvas.DisplayMessageType = typeMessage;
        }
        private void RadioButtonUnChecked(RadioButton radioButton, NodeCanvasClickMode clickMode, MouseButtonEventArgs e)
        {
            if (radioButton.IsChecked == true)
            {
                radioButton.IsChecked = false;
                e.Handled = true;

                ViewModel.NodesCanvas.ClickMode = NodeCanvasClickMode.Default;
            }
            else
            {
                ViewModel.NodesCanvas.ClickMode = clickMode;
            }
        }
        private void ErrorListCollapse()
        {
            this.ErrorListSplitter.IsEnabled = false;
            this.Fotter.Height = new GridLength();
            this.Fotter.MinHeight = 18;

        }
        private void ErrorListExpanded()
        {
            this.ErrorListSplitter.IsEnabled = true;
            this.Fotter.Height = new GridLength(this.ViewModel.DefaultHeightMessagePanel);
            this.Fotter.MinHeight = 52;
        }

        private void TableOfTransitionsCollapse()
        {
            this.TableOfTransitionsSplitter.IsEnabled = false;
            this.TableOfTransitionsColumn.Width = new GridLength();
            this.TableOfTransitionsColumn.MinWidth = 18;
        }

        private void TableOfTransitionsExpanded()
        {
            this.TableOfTransitionsSplitter.IsEnabled = true;
            //this.TableOfTransitionsColumn.Width = new GridLength(this.ViewModel.DefaultWidthTransitionsTable);
            this.TableOfTransitionsColumn.MinWidth = 52;
        }
        private void ShowError()
        {
            if (!this.ErrorListExpander.IsExpanded)
            {
                this.ErrorListExpander.IsExpanded = true;
                ErrorListExpanded();
            }
        }

        private void ButtonMinClick(RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void ButtonMaxClick(RoutedEventArgs e)
        {
            StateNormalMaximaze();
        }
        private void StateNormalMaximaze()
        {
            this.WindowState = (this.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
            UpdateButton();
        }
        private void UpdateButton()
        {
            if (this.WindowState == WindowState.Normal)
            {
                StateNormal();
            }
            else
            {
                StateMaximize();
            }
        }
        private void StateMaximize()
        {
            this.ButtonMaxRectangle.Fill = Application.Current.Resources["IconRestore"] as DrawingBrush;
            this.ButtonMaxRectangle.ToolTip = "Maximize";
        }
        private void StateNormal()
        {
            this.ButtonMaxRectangle.Fill = Application.Current.Resources["IconMaximize"] as DrawingBrush;
            this.ButtonMaxRectangle.ToolTip = "Restore down";
        }
        private void HeaderClick(MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DockPanel)
            {
                if (e.ClickCount == 1)
                {

                    if (this.WindowState == WindowState.Maximized)
                    {
                        var point = PointToScreen(e.MouseDevice.GetPosition(this));

                        if (point.X <= RestoreBounds.Width / 2)
                            Left = 0;

                        else if (point.X >= RestoreBounds.Width)
                            Left = point.X - (RestoreBounds.Width - (this.ActualWidth - point.X));

                        else
                            Left = point.X - (RestoreBounds.Width / 2);

                        Top = point.Y - (this.Header.ActualHeight / 2);

                        StateNormal();
                        this.WindowState = WindowState.Normal;
                    }

                    this.DragMove();
                }
                else
                {
                    StateNormalMaximaze();
                }
                e.Handled = true;
            }
        }


        #endregion SetupEvents


        private void SetupCommands()
        {
            this.WhenActivated(disposable =>
            {
                var SelectedItem = this.ObservableForProperty(x => x.MessageList.SelectedItem).Select(x => (x.Value as MessageViewModel)?.Text);

                this.BindCommand(this.ViewModel, x => x.CommandCopyError, x => x.BindingCopyError, SelectedItem).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandCopyError, x => x.ItemCopyError, SelectedItem).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandChangeTheme, x => x.ButtonChangeTheme).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.CommandCopySchemeName, x => x.ItemCopySchemeName).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSelectAll, x => x.ItemSelectAll).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandZoomIn, x => x.ButtonZoomIn).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandZoomOut, x => x.ButtonZoomOut).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandZoomOriginalSize, x => x.ButtonZoomOriginalSize).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandCollapseUpAll, x => x.ButtonCollapseUpAll).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExpandDownAll, x => x.ButtonExpandDownAll).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandUndo, x => x.ItemUndo).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandUndo, x => x.ButtonUndo).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandRedo, x => x.ItemRedo).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandRedo, x => x.ButtonRedo).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExportToPNG, x => x.BindingExportToPNG).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExportToPNG, x => x.ItemExportToPNG).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExportToPNG, x => x.ButtonExportToPNG).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExportToJPEG, x => x.BindingExportToJPEG).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExportToJPEG, x => x.ItemExportToJPEG).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandNew, x => x.BindingNew).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandNew, x => x.ItemNew).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandNew, x => x.ButtonNew).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandOpen, x => x.BindingOpen).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandOpen, x => x.ItemOpen).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandOpen, x => x.ButtonOpen).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSave, x => x.BindingSave).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSave, x => x.ItemSave).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSave, x => x.ButtonSave).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSaveAs, x => x.BindingSaveAs).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSaveAs, x => x.ItemSaveAs).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSaveAs, x => x.ButtonSaveAs).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExit, x => x.BindingExit).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExit, x => x.ItemExit).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandExit, x => x.ButtonClose).DisposeWith(disposable);

                this.BindCommand(this.ViewModel, x => x.NodesCanvas.CommandSaveCode, x => x.ButtonSaveCode).DisposeWith(disposable);
            });
        }
    }
}
