using DynamicData;
using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Commands;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Splat;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using Matrix = System.Windows.Media.Matrix;
using mitoSoft.Workflows.Editor.ViewModel;
using mitoSoft.Workflows.Editor.Helpers.CodeGenerator;
using mitoSoft.Workflows.Editor.View;
using System.Windows.Navigation;

using mitoSoft.Graphs;
using System.CodeDom;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class NodesCanvasViewModel
    {
        public ReactiveCommand<ConnectViewModel, Unit> CommandAddConnect { get; set; }
        public ReactiveCommand<(string Source, string Target), Unit> CommandAddConnectSourceTarget { get; set; }
        public ReactiveCommand<ConnectViewModel, Unit> CommandDeleteConnect { get; set; }
        public ReactiveCommand<ConnectorViewModel, Unit> CommandAddDraggedConnect { get; set; }
        public ReactiveCommand<Unit, Unit> CommandDeleteDraggedConnect { get; set; }
        public ReactiveCommand<(BaseNodeViewModel objectForValidate, string newValue), Unit> CommandValidateNodeName { get; set; }
        public ReactiveCommand<(ConnectorViewModel objectForValidate, string newValue), Unit> CommandValidateConnectName { get; set; }
        public ReactiveCommand<Point, Unit> CommandSelect { get; set; }
        public ReactiveCommand<Point, Unit> CommandCut { get; set; }
        public ReactiveCommand<Point, Unit> CommandPartMoveAllNode { get; set; }
        public ReactiveCommand<Point, Unit> CommandPartMoveAllSelectedNode { get; set; }
        public ReactiveCommand<string, Unit> CommandLogDebug { get; set; }
        public ReactiveCommand<string, Unit> CommandLogError { get; set; }
        public ReactiveCommand<string, Unit> CommandLogInformation { get; set; }
        public ReactiveCommand<string, Unit> CommandLogWarning { get; set; }
        public ReactiveCommand<Unit, Unit> CommandErrorListUpdate { get; set; }
        public ReactiveCommand<Unit, Unit> CommandHandleRightClick { get; set; }

        #region commands with undo-redo

        public Command<ConnectorViewModel, ConnectorViewModel> CommandAddConnectorWithConnect { get; set; }
        public Command<Point, List<BaseNodeViewModel>> CommandFullMoveAllNode { get; set; }
        public Command<Point, List<BaseNodeViewModel>> CommandFullMoveAllSelectedNode { get; set; }
        public Command<(Point point, NodeType nodeType), BaseNodeViewModel> CommandAddBaseNodeWithUndoRedo { get; set; }
        public Command<List<BaseNodeViewModel>, SequenceNodeViewModel> CommandAddToSequenceWithUndoRedo { get; set; }
        public Command<SequenceNodeViewModel, List<BaseNodeViewModel>> CommandResolveSequenceWithUndoRedo { get; set; }
        public ReactiveCommand<Unit, Unit> CommandGoToCode { get; set; }
        public Command<List<BaseNodeViewModel>, ElementsForDelete> CommandDeleteSelectedNodes { get; set; }
        public Command<List<ConnectorViewModel>, List<(int index, ConnectorViewModel element)>> CommandDeleteSelectedConnectors { get; set; }
        public Command<DeleteMode, DeleteMode> CommandDeleteSelectedElements { get; set; }
        public Command<(BaseNodeViewModel node, string newName), (BaseNodeViewModel node, string oldName)> CommandChangeNodeName { get; set; }
        public Command<(ConnectorViewModel connector, string newName), (ConnectorViewModel connector, string oldName)> CommandChangeConnectName { get; set; }

        #endregion commands with undo-redo

        private void SetupCommands()
        {
            SetupMenuCommands();

            CommandValidateNodeName = ReactiveCommand.Create<(BaseNodeViewModel objectForValidate, string newValue)>(ValidateNodeName);

            CommandValidateConnectName = ReactiveCommand.Create<(ConnectorViewModel objectForValidate, string newValue)>(ValidateConnectName);

            CommandAddConnect = ReactiveCommand.Create<ConnectViewModel>(AddConnect);

            CommandAddConnectSourceTarget = ReactiveCommand.Create<(string Source, string Target)>(AddConnect);

            CommandDeleteConnect = ReactiveCommand.Create<ConnectViewModel>(DeleteConnect);
            
            CommandLogDebug = ReactiveCommand.Create<string>((message)=>LogDebug(message));

            CommandLogError = ReactiveCommand.Create<string>((message) => LogError(message));

            CommandLogInformation = ReactiveCommand.Create<string>((message) => LogInformation(message));

            CommandLogWarning = ReactiveCommand.Create<string>((message) => LogWarning(message));

            CommandErrorListUpdate = ReactiveCommand.Create(ErrorsUpdate);

            CommandHandleRightClick = ReactiveCommand.Create(NodeCanvasContextMenuHandler);

            CommandSelect = ReactiveCommand.Create<Point>(StartSelect);

            CommandCut = ReactiveCommand.Create<Point>(StartCut);

            CommandAddDraggedConnect = ReactiveCommand.Create<ConnectorViewModel>(AddDraggedConnect);

            CommandDeleteDraggedConnect = ReactiveCommand.Create(DeleteDraggedConnect);

            CommandGoToCode = ReactiveCommand.Create(GoToCodeLine);


            CommandPartMoveAllNode = ReactiveCommand.Create<Point>(PartMoveAllNode);

            CommandPartMoveAllSelectedNode = ReactiveCommand.Create<Point>(PartMoveAllSelectedNode);

            CommandFullMoveAllNode = new Command<Point, List<BaseNodeViewModel>>(FullMoveAllNode, UnFullMoveAllNode, NotSaved);

            CommandFullMoveAllSelectedNode = new Command<Point, List<BaseNodeViewModel>>(FullMoveAllSelectedNode, UnFullMoveAllSelectedNode, NotSaved);

            CommandAddConnectorWithConnect = new Command<ConnectorViewModel, ConnectorViewModel>(AddConnectorWithConnect, DeleteConnectorWithConnect, NotSavedCode);

            CommandAddBaseNodeWithUndoRedo = new Command<(Point point,NodeType nodeType), BaseNodeViewModel>(AddNodeWithUndoRedo, DeleteNodetWithUndoRedo, NotSavedCode);

            CommandAddToSequenceWithUndoRedo = new Command<List<BaseNodeViewModel>, SequenceNodeViewModel>(ItemAddToSequence, ItemAddToSequenceUndo, NotSavedCode);
            
            CommandResolveSequenceWithUndoRedo = new Command<SequenceNodeViewModel, List<BaseNodeViewModel>>(ItemResolveSequence, ItemResolveSequenceUndo, NotSavedCode);

            CommandDeleteSelectedNodes = new Command<List<BaseNodeViewModel>, ElementsForDelete>(DeleteSelectedNodes, UnDeleteSelectedNodes, NotSavedCode);

            CommandDeleteSelectedConnectors = new Command<List<ConnectorViewModel>, List<(int index, ConnectorViewModel connector)>>(DeleteSelectedConnectors, UnDeleteSelectedConnectors, NotSavedCode);
            
            CommandDeleteSelectedElements = new Command<DeleteMode, DeleteMode>(DeleteSelectedElements, UnDeleteSelectedElements);

            CommandChangeNodeName = new Command<(BaseNodeViewModel node, string newName), (BaseNodeViewModel node, string oldName)>(ChangeNodeName, UnChangeNodeName);

            CommandChangeConnectName = new Command<(ConnectorViewModel connector, string newName), (ConnectorViewModel connector, string oldName)>(ChangeConnectName, UnChangeConnectName);

            NotSavedSubscrube();
        }

        private void GoToCodeLine()
        {                
            int lineNr = -1;
            if (SelectedNode != null)
            {
                lineNr = GoToCode.FindInCode(SelectedNode, CodePath);
            }
            else if(SelectedTransition != null)
            {
                if(SelectedTransition.Node.TransitionsForView.Where(y => !string.IsNullOrEmpty(y.Name)).Count() > 1)
                {
                    lineNr = GoToCode.FindInCode(SelectedTransition, CodePath);
                }
                else
                {
                    lineNr = GoToCode.FindInCode(SelectedTransition.Node, CodePath);
                }                
            }
            if (lineNr != -1)
            {
                MessageBox.Show((lineNr+1).ToString());
                Clipboard.SetText((lineNr+1).ToString());
            }
            else
            {
                MessageBox.Show((lineNr + 1).ToString());
            }
            if (CodeSaved == false)
            {
                CommandLogWarning.ExecuteWithSubscribe("Save Code Before Searching in Code");
            }
        }

        private void NodeCanvasContextMenuHandler()
        {
            var selectedNodes = Nodes.Items.Where(x => x.Selected == true);

            ShowResolveSequenceControlItem = (selectedNodes.Count() == 1 && selectedNodes.First() is SequenceNodeViewModel);
            
            ShowAddSequenceControlItem = selectedNodes.Count() > 1 && selectedNodes.Any(x=>x is NodeViewModel);

            ShowGoToDefinition = (SelectedNode != null && SelectedTransition == null) ||
                (SelectedNode == null && SelectedTransition != null);
        }

        private void NotSaved() => ProjectSaved = false;

        private void NotSavedCode()
        {
            ProjectSaved = false;
            CodeSaved= false;
        }

        private void NotSavedSubscrube()
        {
            CommandRedo.Subscribe(_ => NotSavedCode());

            CommandUndo.Subscribe(_ => NotSavedCode());

            CommandAddConnect.Subscribe(_ => NotSavedCode());

            CommandDeleteConnect.Subscribe(_ => NotSavedCode());
        }

        private void ErrorsUpdate()
        {
            Messages.RemoveMany(Messages.Where(x => x.TypeMessage == DisplayMessageType || DisplayMessageType == TypeMessage.All));
        }        

        private void StartSelect(Point point)
        {
            Selector.CommandStartSelect.ExecuteWithSubscribe(point);
        }

        private void StartCut(Point point)
        {
            Cutter.CommandStartCut.ExecuteWithSubscribe(point);
        }

        private void PartMoveAllNode(Point delta)
        {
            foreach (var node in Nodes.Items)
            {
                node.CommandMove.ExecuteWithSubscribe(delta);
            }
        }

        private void PartMoveAllSelectedNode(Point delta)
        {
            foreach (var node in Nodes.Items.Where(x => x.Selected))
            {
                node.CommandMove.ExecuteWithSubscribe(delta);
            }
        }

        private void AddDraggedConnect(ConnectorViewModel fromConnector)
        {
            DraggedConnect = new ConnectViewModel(this, fromConnector);

            AddConnect(DraggedConnect);
        }

        private void DeleteDraggedConnect()
        {
            Connects.Remove(DraggedConnect);
            
            DraggedConnect.FromConnector.Connect = null;
        }

        private void AddConnect(ConnectViewModel ViewModelConnect)
        {
            Connects.Add(ViewModelConnect);
        }

        private void AddConnect((string Source, string Target) con)
        {
            BaseNodeViewModel nodeFrom = Nodes.Items.Single(x => x.Name == con.Source);

            BaseNodeViewModel nodeTo = Nodes.Items.Single(x => x.Name == con.Target);

            nodeFrom.CurrentConnector.Name = GetNameForTransition();

            if (nodeFrom == nodeTo)
            {
                nodeFrom.CurrentConnector.CommandSetAsLoop.ExecuteWithSubscribe();
            }
            else
            {
                var connect = new ConnectViewModel(nodeFrom.NodesCanvas, nodeFrom.CurrentConnector);

                connect.ToConnector = nodeTo.Input;

                nodeFrom.CommandAddEmptyConnector.ExecuteWithSubscribe();

                Connects.Add(connect);
            }
        }

        private void DeleteConnect(ConnectViewModel ViewModelConnect)
        {
            Connects.Remove(ViewModelConnect);
        }

        private void ValidateNodeName((BaseNodeViewModel objectForValidate, string newValue) obj)
        {
            if (!String.IsNullOrWhiteSpace(obj.newValue))
            {
                if (!NodeExists(obj.newValue))
                {
                    LogDebug("Node \"{0}\"  has been renamed . New name is \"{1}\"", obj.objectForValidate.Name, obj.newValue);

                    CommandChangeNodeName.Execute((obj.objectForValidate, obj.newValue));
                }
                else
                {
                    LogError("Name for node doesn't set, because node with name \"{0}\" already exist", obj.newValue);
                }
            }
            else
            {
                LogError("Name for node doesn't set, name off node should not be empty", obj.newValue);
            }
        }

        private void ValidateConnectName((ConnectorViewModel objectForValidate, string newValue) obj)
        {
            if (!String.IsNullOrWhiteSpace(obj.newValue))
            {
                if (!ConnectsExist(obj.newValue))
                {
                    LogDebug("Transition \"{0}\"  has been renamed . New name is \"{1}\"", obj.objectForValidate.Name, obj.newValue);

                    CommandChangeConnectName.Execute((obj.objectForValidate, obj.newValue));
                }
                else
                {
                    LogError("Name for transition doesn't set, because transition with name \"{0}\" already exist", obj.newValue);
                }
            }
            else
            {
                LogError("Name for transition doesn't set, name off transition should not be empty", obj.newValue);
            }
        }

        private List<BaseNodeViewModel> FullMoveAllNode(Point delta, List<BaseNodeViewModel> nodes = null)
        {
            if (nodes == null)
            {
                nodes = Nodes.Items.ToList();

                delta = new Point();
            }           
            nodes.ForEach(node => node.CommandMove.ExecuteWithSubscribe(delta));
            
            return nodes;
        }

        private List<BaseNodeViewModel> UnFullMoveAllNode(Point delta, List<BaseNodeViewModel> nodes = null)
        {
            Point myPoint = delta.Copy();

            myPoint = myPoint.Mirror();

            nodes.ForEach(node => node.CommandMove.ExecuteWithSubscribe(myPoint));

            return nodes;
        }

        private List<BaseNodeViewModel> FullMoveAllSelectedNode(Point delta, List<BaseNodeViewModel> nodes = null)
        {
            Point myPoint = delta.Copy();
            if (nodes == null)
            {
                nodes = Nodes.Items.Where(x => x.Selected).ToList();             

                myPoint = new Point();
            }            
            nodes.ForEach(node => node.CommandMove.ExecuteWithSubscribe(myPoint));
            
            return nodes;
        }

        private List<BaseNodeViewModel> UnFullMoveAllSelectedNode(Point delta, List<BaseNodeViewModel> nodes = null)
        {
            Point myPoint = delta.Copy();

            myPoint = myPoint.Mirror();

            nodes.ForEach(node => node.CommandMove.ExecuteWithSubscribe(myPoint));

            return nodes;
        }

        private BaseNodeViewModel AddNodeWithUndoRedo((Point point, NodeType NodeType) param , BaseNodeViewModel result)
        {
            BaseNodeViewModel newNode = result;            

            if (result == null)
            {
                if(param.NodeType == NodeType.Node)
                {
                    newNode = new NodeViewModel(this, GetNameForNewNode(param.NodeType), param.point);
                }
                else if (param.NodeType == NodeType.ParallelNode)
                {
                    newNode = new ParallelNodeViewModel(this, GetNameForNewNode(param.NodeType), param.point);
                }
                else if (param.NodeType == NodeType.SequenceNode)
                {
                    newNode = new SequenceNodeViewModel(this, GetNameForNewNode(param.NodeType), param.point);
                }
                else if(param.NodeType == NodeType.SubWorkflowNode)
                {
                    newNode = new SubWorkflowNodeViewModel(this, GetNameForNewNode(param.NodeType), param.point);
                }                  
            }
            else
            {
                NodesCount--;
            }
            Nodes.Add(newNode);

            LogDebug("Node with name \"{0}\" was added", newNode.Name);

            return newNode;
        }

        private BaseNodeViewModel DeleteNodetWithUndoRedo((Point point, NodeType NodeType) param, BaseNodeViewModel result)
        {
            Nodes.Remove(result);

            LogDebug("Node with name \"{0}\" was removed", result.Name);

            return result;
        }        

        private ConnectorViewModel AddConnectorWithConnect(ConnectorViewModel parameter, ConnectorViewModel result)
        {
            if (result == null)
            {
                return parameter;
                
            }
            else
            {
                TransitionsCount--;
            }             
            result.Node.CommandAddConnectorWithConnect.ExecuteWithSubscribe((1, result));
            
            LogDebug("Transition with name \"{0}\" was added", result.Name);
            
            return result;
        }

        private ConnectorViewModel DeleteConnectorWithConnect(ConnectorViewModel parameter, ConnectorViewModel result)
        {            
            result.Node.CommandDeleteConnectorWithConnect.ExecuteWithSubscribe(result);
           
            LogDebug("Transition with name \"{0}\" was removed", result.Name);
            
            return parameter;
        }

        private DeleteMode DeleteSelectedElements(DeleteMode parameter, DeleteMode result)
        {
            if (result == DeleteMode.noCorrect)
            {
                bool keyN = Keyboard.IsKeyDown(Key.N);
                
                bool keyC = Keyboard.IsKeyDown(Key.C);

                if (keyN == keyC)
                {
                    result = DeleteMode.DeleteAllSelected;
                }
                else if (keyN)
                {
                    result = DeleteMode.DeleteNodes;
                }
                else if (keyC)
                {
                    result = DeleteMode.DeleteConnects;
                }
            }
            if ((result == DeleteMode.DeleteConnects) || (result == DeleteMode.DeleteAllSelected))
            {
                CommandDeleteSelectedConnectors.Execute();
            }
            if ((result == DeleteMode.DeleteNodes) || (result == DeleteMode.DeleteAllSelected))
            {
                CommandDeleteSelectedNodes.Execute();
            }
            return result;
        }

        private DeleteMode UnDeleteSelectedElements(DeleteMode parameter, DeleteMode result)
        {
            int count = 0;

            if ((result == DeleteMode.DeleteNodes) || (result == DeleteMode.DeleteConnects))
            {
                count = 1;
            }
            else if (result == DeleteMode.DeleteAllSelected)
            {
                count = 2;
            }
            for (int i = 0; i < count; i++)
            {
                CommandUndo.ExecuteWithSubscribe();
            }
            return result;
        }

        private List<(int index, ConnectorViewModel connector)> DeleteSelectedConnectors(List<ConnectorViewModel> parameter, List<(int index, ConnectorViewModel connector)> result)
        {
            if (result == null)
            {               
                result = new List<(int index, ConnectorViewModel element)>();
                
                foreach (var connector in parameter?? GetAllConnectors().Where(x => x.Selected))
                {
                    result.Add((connector.Node.GetConnectorIndex(connector), connector));
                }
            }
            foreach (var element in result)
            {
                element.connector.Node.CommandDeleteConnectorWithConnect.ExecuteWithSubscribe(element.connector);
                
                LogDebug("Transition with name \"{0}\" was removed", element.connector.Name);
            }
            return result;
        }

        private List<(int index, ConnectorViewModel connector)> UnDeleteSelectedConnectors(List<ConnectorViewModel>  parameter, List<(int index, ConnectorViewModel connector)> result)
        {
            foreach (var element in result)
            {
                TransitionsCount--;
               
                element.connector.Node.CommandAddConnectorWithConnect.ExecuteWithSubscribe((element.index, element.connector));
                
                LogDebug("Transition with name \"{0}\" was added", element.connector.Name);
            }

            return result;
        }

        private (ConnectorViewModel connector, string oldName) ChangeConnectName((ConnectorViewModel connector, string newName) parameter, (ConnectorViewModel connector, string oldName) result)
        {
            string oldName = parameter.connector.Name;
           
            parameter.connector.Name = parameter.newName;
            
            return (parameter.connector, oldName);
        }

        private (ConnectorViewModel connector, string oldName) UnChangeConnectName((ConnectorViewModel connector, string newName) parameter, (ConnectorViewModel connector, string oldName) result)
        {            
            result.connector.Name = result.oldName;
            
            return result;
        }

        private (BaseNodeViewModel node, string oldName) ChangeNodeName((BaseNodeViewModel node, string newName) parameter, (BaseNodeViewModel node, string oldName) result)
        {
            string oldName = parameter.node.Name;
            
            parameter.node.Name = parameter.newName;
            
            return (parameter.node, oldName);
        }

        private (BaseNodeViewModel node, string oldName) UnChangeNodeName((BaseNodeViewModel node, string newName) parameter, (BaseNodeViewModel node, string oldName) result)
        {           
            result.node.Name = result.oldName;
            
            return result;
        }

        private ElementsForDelete DeleteSelectedNodes(List<BaseNodeViewModel> parameter, ElementsForDelete result)
        {
            if (result == null)
            {
                result = new ElementsForDelete();
             
                result.NodesToDelete = (parameter?.Where(x=>x.CanBeDelete)?? Nodes.Items.Where(x => x.Selected && x.CanBeDelete)).ToList();
               
                result.ConnectsToDelete = new List<ConnectViewModel>();
              
                result.ConnectsToDeleteWithConnectors = new List<(int connectorIndex, ConnectViewModel connect)>();

                foreach (var connect in Connects)
                {
                    if (result.NodesToDelete.Contains(connect.FromConnector.Node))
                    {
                        result.ConnectsToDelete.Add(connect);
                    }
                    else if (result.NodesToDelete.Contains(connect.ToConnector.Node))
                    {
                        result.ConnectsToDeleteWithConnectors.Add((connect.FromConnector.Node.GetConnectorIndex(connect.FromConnector), connect));
                    }
                }
            }
            foreach (var element in result.ConnectsToDeleteWithConnectors)
            {
                element.connect.FromConnector.Node.CommandDeleteConnectorWithConnect.ExecuteWithSubscribe(element.connect.FromConnector);
              
                LogDebug("Transition with name \"{0}\" was removed", element.connect.FromConnector.Name);
            }
            Connects.RemoveMany(result.ConnectsToDelete);
           
            Nodes.RemoveMany(result.NodesToDelete);
           
            foreach(var node in result.NodesToDelete)
            {
                LogDebug("Node with name \"{0}\" was removed", node.Name);
            }
            return result;
        }

        private ElementsForDelete UnDeleteSelectedNodes(List<BaseNodeViewModel> parameter, ElementsForDelete result)
        {
            NodesCount -= result.NodesToDelete.Count;
            
            Nodes.AddRange(result.NodesToDelete);
           
            foreach (var node in result.NodesToDelete)
            {
                LogDebug("Node with name \"{0}\" was added", node.Name);
            }
           
            Connects.AddRange(result.ConnectsToDelete);
           
            result.ConnectsToDeleteWithConnectors.Sort(ElementsForDelete.Sort);
            
            foreach (var element in result.ConnectsToDeleteWithConnectors)
            {
                TransitionsCount--;
                
                element.connect.FromConnector.Node.CommandAddConnectorWithConnect.ExecuteWithSubscribe((element.connectorIndex, element.connect.FromConnector));
               
                LogDebug("Transition with name \"{0}\" was added", element.connect.FromConnector.Name);
            }
            return result;
        }

        private IEnumerable<ConnectorViewModel> GetAllConnectors()
        {
            return this.Nodes.Items.SelectMany(x => x.Transitions.Items);
        }

        private bool ConnectsExist(string nameConnect)
        {
            return GetAllConnectors().Any(x => x.Name == nameConnect);
        }

        private bool NodeExists(string NodeName)
        {
            return Nodes.Items.Any(x => x.Name == NodeName);
        }

        public class ElementsForDelete
        {
            public List<BaseNodeViewModel> NodesToDelete;
            
            public List<ConnectViewModel> ConnectsToDelete;
           
            public List<(int connectorIndex, ConnectViewModel connect)> ConnectsToDeleteWithConnectors;

            public static int Sort((int connectorIndex, ConnectViewModel connect) A, (int connectorIndex, ConnectViewModel connect) B)
            {
                return A.connectorIndex.CompareTo(B.connectorIndex);
            }
        }
    }
}
