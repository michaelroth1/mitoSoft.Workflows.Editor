using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using System.Reactive.Linq;
using System.Linq;
using DynamicData.Alias;
using System.Collections.Generic;
using mitoSoft.Workflows.Editor.View;
using System.Collections;
using mitoSoft.Workflows.Editor.Helpers.HashSetComparer;
using System.IO;
using System.Diagnostics;
using System.Security.Policy;
using System.Threading;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System.Xml.Linq;
using mitoSoft.Graphs;
using System.Windows.Forms.VisualStyles;
using mitoSoft.Workflows.Editor.Helpers.Commands;
using mitoSoft.StateMachine.AdvancedStateMachines;
using mitoSoft.Workflows.Editor.Helpers.SchemeUpdater;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class MainWindowViewModel : ReactiveObject
    {
        public ObservableCollectionExtended<MessageViewModel> Messages { get; set; } = new ObservableCollectionExtended<MessageViewModel>();

        [Reactive] public NodesCanvasViewModel NodesCanvas { get; set; }

        public MainWindowViewModel ParentWindow { get; set; } = null;

        public DirectoryWatcher DirectoryWatcher { get; set; }
        [Reactive] public string DirectoryWatcherPath { get; set; }

        [Reactive] public bool CodeChanged { get; set; }
        public ObservableCollectionExtended<string> CodeFilesChanged { get; set; } = new ObservableCollectionExtended<string>();

        [Reactive] public TypeMessage DisplayMessageType { get; set; }
        [Reactive] public bool? DebugEnable { get; set; } = true;

        [Reactive] public int CountError { get; set; }
        [Reactive] public int CountWarning { get; set; }
        [Reactive] public int CountInformation { get; set; }
        [Reactive] public int CountDebug { get; set; }

        [Reactive] public object PropertyView { get; set; }
        public BaseNodeViewModel SelectedNodeViewModel;
        public ConnectorViewModel SelectedTransitionViewModel;

        private IDisposable ConnectToMessages;
        public double DefaultHeightMessagePanel = 150;
        public double DefaultWidthTransitionsTable = 350;
        public ObservableCollectionExtended<ConnectorViewModel> Transitions { get; set; } = new ObservableCollectionExtended<ConnectorViewModel>();



        [Reactive] public object SelectedItem { get; set; }


        public MainWindowViewModel(NodesCanvasViewModel viewModelNodesCanvas)
        {            
            NodesCanvas = viewModelNodesCanvas;
            NodesCanvas.Nodes.Connect().TransformMany(x => x.Transitions).AutoRefresh(x => x.Name).Filter(x => !string.IsNullOrEmpty(x.Name)).Bind(Transitions).Subscribe();
            DirectoryWatcher = new DirectoryWatcher(OnChanged);
            SetupCommands();
            SetupSubscriptions();
        }

        public MainWindowViewModel(NodesCanvasViewModel viewModelNodesCanvas, MainWindowViewModel _parentWindow):this(viewModelNodesCanvas)
        {
            ParentWindow = _parentWindow;                                  
        }


        #region Setup Subscriptions

        private void SetupSubscriptions()
        {
            this.WhenAnyValue(x => x.NodesCanvas.DisplayMessageType).Subscribe(_ => UpdateMessages());
            this.WhenAnyValue(x => x.NodesCanvas.Messages.Count).Subscribe(_ => UpdateCountMessages());
            this.WhenAnyValue(x => x.NodesCanvas.Messages.Count).Subscribe(_ => UpdateCountMessages());
            this.WhenAnyValue(x => x.NodesCanvas.SchemePath).Subscribe(x => UpdateWorkflowProjectDirectory(x,true));
            this.WhenAnyValue(x => x.NodesCanvas.CodePath).Subscribe(x => UpdateWorkflowProjectDirectory(x,false));
            this.WhenAnyValue(x => x.DirectoryWatcherPath).Subscribe(_ => UpdateDirectoryWatcherPath());
            this.WhenAnyValue(x => x.NodesCanvas.SelectedNode).Subscribe(x => UpdateConnectedTransitions(x));

        }

        public void UpdateConnectedTransitions(BaseNodeViewModel node)
        {
            if (node != null)
            {
                var transitions = node.Transitions.Items.Skip(1);
                node.ConnectedTransitionsForView.Clear();
                node.ConnectedTransitionsForView.AddRange(transitions);
            }
        }

        private void UpdateWorkflowProjectDirectory(string path,bool FromScheme)
        {
            var checkPath = FromScheme ? NodesCanvas.SchemePath : NodesCanvas.CodePath;
            if (DirectoryWatcher != null && !string.IsNullOrEmpty(checkPath))
            {
                DirectoryWatcherPath = PathHelper.GetWorkflowProjectDirectory(Path.GetDirectoryName(path)) + @"\bin";
            }
        }

        private void UpdateDirectoryWatcherPath()
        {
            if (!string.IsNullOrEmpty(DirectoryWatcherPath))
            {
                DirectoryWatcher?.PathChanged(DirectoryWatcherPath);
            }
        }

        private void UpdateMessages()
        {
            ConnectToMessages?.Dispose();

            Messages.Clear();

            bool debugEnable = DebugEnable.HasValue && DebugEnable.Value;
            bool displayAll = this.NodesCanvas.DisplayMessageType == TypeMessage.All;

            ConnectToMessages = this.NodesCanvas.Messages.ToObservableChangeSet().Filter(x => CheckForDisplay(x.TypeMessage)).ObserveOnDispatcher().Bind(Messages).DisposeMany().Subscribe();

            bool CheckForDisplay(TypeMessage typeMessage)
            {
                if (typeMessage == this.NodesCanvas.DisplayMessageType)
                {
                    return true;
                }
                else if (typeMessage == TypeMessage.Debug)
                {
                    return debugEnable && displayAll;
                }
                return displayAll;
            }
        }

        private void UpdateCountMessages()
        {
            var counts = NodesCanvas.Messages.GroupBy(x => x.TypeMessage).ToDictionary(x => x.Key, x => x.Count());
            CountError = counts.Keys.Contains(TypeMessage.Error) ? counts[TypeMessage.Error] : 0;
            CountWarning = counts.Keys.Contains(TypeMessage.Warning) ? counts[TypeMessage.Warning] : 0;
            CountInformation = counts.Keys.Contains(TypeMessage.Information) ? counts[TypeMessage.Information] : 0;
            CountDebug = counts.Keys.Contains(TypeMessage.Debug) ? counts[TypeMessage.Debug] : 0;
        }

        #endregion Setup Subscriptions


        public void OnChanged(string ChangedFile)
        {
            try
            {
                var WorkflowName = Path.GetFileNameWithoutExtension(NodesCanvas.SchemePath);

                var sm = WorkflowLoader.GetWorkflowInstance(WorkflowName, ChangedFile);
                
                if (sm == null)
                {
                    NodesCanvas.CommandLogDebug.ExecuteWithSubscribe($"Skip assembly {Path.GetFileName(ChangedFile)}. Does not contains {WorkflowName}");
                    return;
                }
                var CtS = new CodeToSchemeComparer(_codeWorkflow: sm,  nodesCanvas: NodesCanvas);

                if (!NodesCanvas.CanUpdateFromCode())
                {
                    return;
                }
                NodesCanvas.UpdateSchemeFromCode(CtS); 
                
            }
            catch (TargetInvocationException e)
            {
                NodesCanvas.CommandLogError.ExecuteWithSubscribe($"Load Workflow from Code failed: {e.InnerException?.Message}");
            }
        }

        //public void renamedetection()
        //{
        //    var AddRemoveEdge = edges.EdgesToAdd.Where(x => edges.EdgesToDelete.Select(x => (x.Connect?.FromConnector.Node.Name, x.Connect?.ToConnector.Node.Name)).Any(y => x.Item1 == y.Item1)).ToList();
        //    var AddRemoveEdgeDel = edges.EdgesToDelete.Select(x => (x.Connect?.FromConnector.Node.Name, x.Connect?.ToConnector.Node.Name)).Where(x => edges.EdgesToAdd.Any(y => x.Item1 == y.Item1)).ToList();

        //    if (AddRemoveEdge.Count() == AddRemoveEdgeDel.Count())
        //    {
        //        var newnames = AddRemoveEdge.Where(x => AddRemoveEdgeDel.Any(y => y.Item1 == x.Item1)).Select(n => n.Item2).ToList();
        //        var oldnames = AddRemoveEdgeDel.Where(x => AddRemoveEdge.Any(y => y.Item1 == x.Item1)).Select(n => n.Item2).ToList();


        //        nodes.nodesToAdd.Remove(newnames);
        //        var tempDelete = nodes.GetNodesToDelete.Where(x => oldnames.Any(y => x.Name == y)).ToList();
        //        nodes.GetNodesToDelete.Remove(tempDelete);
        //        edges.EdgesToAdd.Remove(AddRemoveEdge.ToList());
        //        var tempDelete2 = edges.EdgesToDelete.Where(x => AddRemoveEdgeDel.Any(y => x.Connect.FromConnector.Node.Name == y.Item1 && x.Connect.ToConnector.Node.Name == y.Item2)).ToList();
        //        edges.EdgesToDelete.Remove(tempDelete2);

        //        App.Current.Dispatcher.Invoke((Action)delegate
        //        {
        //            for (int i = 0; i < oldnames.Count; i++)
        //            {
        //                Debug.WriteLine($"Change Node Name | {oldnames[i]} -> {newnames[i]} ");
        //                var node = NodesCanvas.Nodes.Items.Where(x => x.Name == oldnames[i]).First();
        //                NodesCanvas.CommandChangeNodeName.Execute((node, newnames[i]));
        //            }
        //        });
        //    }
        //}
    }
}
