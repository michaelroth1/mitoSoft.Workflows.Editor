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
using Matrix = System.Windows.Media.Matrix;
using mitoSoft.Workflows.Editor.Helpers.CodeGenerator;
using mitoSoft.Workflows.Editor.Helpers.SchemeUpdater;
using System.Diagnostics;
using mitoSoft.Workflows;
using mitoSoft.Graphs;
using mitoSoft.Workflows.Editor.View;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class NodesCanvasViewModel
    {
        public ReactiveCommand<Unit, Unit> CommandNew { get; set; }

        public ReactiveCommand<Unit, Unit> CommandRedo { get; set; }

        public ReactiveCommand<Unit, Unit> CommandUndo { get; set; }

        public ReactiveCommand<Unit, Unit> CommandSelectAll { get; set; }

        public ReactiveCommand<Unit, Unit> CommandUnSelectAll { get; set; }

        public ReactiveCommand<Unit, Unit> CommandSelectorIntersect { get; set; }

        public ReactiveCommand<Unit, Unit> CommandCutterIntersect { get; set; }

        public ReactiveCommand<Unit, Unit> CommandZoomIn { get; set; }

        public ReactiveCommand<Unit, Unit> CommandZoomOut { get; set; }

        public ReactiveCommand<(Point point, double delta), Unit> CommandZoom { get; set; }

        public ReactiveCommand<Unit, Unit> CommandZoomOriginalSize { get; set; }

        public ReactiveCommand<Unit, Unit> CommandCollapseUpAll { get; set; }

        public ReactiveCommand<Unit, Unit> CommandExpandDownAll { get; set; }

        public ReactiveCommand<Unit, Unit> CommandExportToPNG { get; set; }

        public ReactiveCommand<Unit, Unit> CommandExportToJPEG { get; set; }

        public ReactiveCommand<Unit, Unit> CommandOpen { get; set; }

        public ReactiveCommand<string, Unit> CommandLoadScheme { get; set; }

        public ReactiveCommand<Unit, Unit> CommandSave { get; set; }

        public ReactiveCommand<Unit, Unit> CommandSaveAs { get; set; }

        public ReactiveCommand<Unit, Unit> CommandExit { get; set; }

        public ReactiveCommand<Unit, Unit> CommandSaveCode { get; set; }

        public ReactiveCommand<Unit, Unit> CommandChangeTheme { get; set; }



        #region Context Menu Commands
        public ReactiveCommand<Unit, Unit> CommandCollapseUpSelected { get; set; }

        public ReactiveCommand<Unit, Unit> CommandExpandDownSelected { get; set; }
        #endregion


        private void SetupMenuCommands()
        {
            CommandRedo = ReactiveCommand.Create(ICommandWithUndoRedo.Redo);

            CommandUndo = ReactiveCommand.Create(ICommandWithUndoRedo.Undo);

            CommandSelectAll = ReactiveCommand.Create(SelectedAll);

            CommandUnSelectAll = ReactiveCommand.Create(UnSelectedAll);

            CommandSelectorIntersect = ReactiveCommand.Create(SelectNodes);

            CommandCutterIntersect = ReactiveCommand.Create(SelectConnects);

            CommandZoomIn = ReactiveCommand.Create(ZoomIn);

            CommandZoomOut = ReactiveCommand.Create(ZoomOut);

            CommandZoomOriginalSize = ReactiveCommand.Create(ZoomOriginalSize);

            CommandCollapseUpAll = ReactiveCommand.Create(CollapseUpAll);

            CommandExpandDownAll = ReactiveCommand.Create(ExpandDownAll);

            CommandCollapseUpSelected = ReactiveCommand.Create(CollapseUpSelected);

            CommandExpandDownSelected = ReactiveCommand.Create(ExpandDownSelected);

            CommandExportToPNG = ReactiveCommand.Create(ExportToPNG);

            CommandExportToJPEG = ReactiveCommand.Create(ExportToJPEG);

            CommandNew = ReactiveCommand.Create(New);

            CommandOpen = ReactiveCommand.Create(Open);

            CommandLoadScheme = ReactiveCommand.Create<string>(LoadScheme);

            CommandSave = ReactiveCommand.Create(SaveProject);

            CommandSaveAs = ReactiveCommand.Create(SaveProjectAs);

            CommandSaveCode = ReactiveCommand.Create(ExportCode);

            CommandExit = ReactiveCommand.Create(Exit);

            CommandChangeTheme = ReactiveCommand.Create(ChangeTheme);

            CommandZoom = ReactiveCommand.Create<(Point point, double delta)>(Zoom);
        }


        #region SequenceHandler
        private List<BaseNodeViewModel> ItemResolveSequence(SequenceNodeViewModel parameter, List<BaseNodeViewModel> result)
        {
            if (parameter == null)
            {
                return null;
            }

            if (parameter.SequenceNodes.Count == 0)
            {
                SeqenceTransformError("Cannot Resolve Empty SequenceNode!");
                return null;
            }

            Nodes.AddRange(parameter.SequenceNodes);

            BaseNodeViewModel tempNode = null;
            foreach (var node in parameter.SequenceNodes)
            {
                if (tempNode != null)
                {
                    this.CommandAddConnectSourceTarget.ExecuteWithSubscribe((tempNode.Name,node.Name));
                }
                tempNode = node;
            }

            ChangeAllInputTransitions(parameter, parameter.SequenceNodes.First());

            ChangeOutputTransition(parameter, parameter.SequenceNodes.Last());

            result = parameter.SequenceNodes.ToList();

            parameter.SequenceNodes.Clear();

            DeleteSelectedNodes(new List<BaseNodeViewModel>() { parameter }, null);

            return result;
        }

        private List<BaseNodeViewModel> ItemResolveSequenceUndo(SequenceNodeViewModel parameter, List<BaseNodeViewModel> result)
        {
            ItemAddToSequence(result, parameter);
            return result;
        }
        
        private SequenceNodeViewModel ItemAddToSequenceUndo(List<BaseNodeViewModel> parameter, SequenceNodeViewModel result)
        {
            ItemResolveSequence(result, parameter);
            return result;
        }

        private SequenceNodeViewModel ItemAddToSequence(List<BaseNodeViewModel> parameter, SequenceNodeViewModel result)
        {
            if(parameter == null)
            {
                return null;
            }

            (var firstSelected, var lastSelected) = ValidateSequence(parameter);            

            if (firstSelected == null)
            {
                SeqenceTransformError("Cannot Parse Selected Node to Sequence!");
                return null;
            }
            parameter = GetOrderdSeqenceNodes(parameter);

            var selectedNodeNamesSet = parameter.Select(x => x.Name).ToHashSet();

            SequenceNodeViewModel sequenceNode;

            if (!SeqPattern.TryGetValue(selectedNodeNamesSet, out sequenceNode))
            {
                sequenceNode = new SequenceNodeViewModel(this, GetNameForNewNode(NodeType.SequenceNode), parameter.First().Point1);

                SeqPattern.Add(selectedNodeNamesSet, sequenceNode);
            }            

            ChangeAllInputTransitions(firstSelected, sequenceNode);

            ChangeOutputTransition(lastSelected, sequenceNode);

            AddNodesToSequenceNode(parameter, sequenceNode);

            Nodes.Add(sequenceNode);

            result = sequenceNode;

            DeleteSelectedNodes(parameter, null);

            return result;
        }

        private void AddNodesToSequenceNode(List<BaseNodeViewModel> parameter, SequenceNodeViewModel seqNode)
        {
            foreach (var node in parameter)
            {
                foreach (var transi in node.Transitions.Items)
                {
                    if (transi.Connect != null)
                    {
                        node.CommandDeleteConnectorWithConnect.ExecuteWithSubscribe(transi);
                    }
                }
            }
            seqNode.SequenceNodes.AddRange(parameter);
        }

        private List<BaseNodeViewModel> GetOrderdSeqenceNodes(List<BaseNodeViewModel> nodes)
        {
            List<BaseNodeViewModel> orderdSequence = new List<BaseNodeViewModel>();

            var firstNode = GetFirstNodeOfSequence(nodes);
            if (firstNode == null)
            {
                SeqenceTransformError("Cannot Find first Selected Node of Sequence!");
                return null;
            }
            orderdSequence.Add(firstNode);

            var nextNodes = firstNode.GetSuccessors();

            while (nextNodes.Count() == 1 && nodes.Contains(nextNodes.First()))
            {
                orderdSequence.Add(nextNodes.First());
                nextNodes = nextNodes.First().GetSuccessors();
            }
            return orderdSequence;
        }

        private void ChangeOutputTransition(BaseNodeViewModel fromNode, BaseNodeViewModel toNode)
        {
            var tempTransis = fromNode.Transitions.Items.Where((x => x.Connect != null));

            foreach (var temptransi in tempTransis)
            {
                if (temptransi != null)
                {
                    fromNode.Transitions.Remove(temptransi);

                    temptransi.Connect.FromConnector.Node = toNode;

                    toNode.Transitions.Add(temptransi);
                }
            }
        }

        private void ChangeAllInputTransitions(BaseNodeViewModel fromNode, BaseNodeViewModel toNode)
        {
            var pre = fromNode.GetPredecessors();

            var transis = pre.SelectMany(x => x.Transitions.Items.Where(x => x.Connect != null && x.Connect.ToConnector.Node == fromNode));

            transis.ToList().ForEach(x => x.Connect.ToConnector = toNode?.Input);
        }

        public (BaseNodeViewModel firstNode, BaseNodeViewModel lastNode) ValidateSequence(List<BaseNodeViewModel> selectedNodes)
        {            
            var firstNode = GetFirstNodeOfSequence(selectedNodes);            

            var lastNode = GetLastNodeOfSequence(selectedNodes);

            if (firstNode == null || lastNode == null || firstNode.Name == lastNode.Name)
            {
                return (null, null);
            }

            if (selectedNodes.Any(x => x.TransitionsForView.Any(y=>y.ItsLoop)))
            {
                return (null, null);
            }

            if (selectedNodes.Where(x => !x.Equals(lastNode)).Where(x => x.GetSuccessors().Count() > 1).Any())
            {
                return (null, null);
            }

            if (selectedNodes.Where(x => !x.Equals(firstNode)).Where(x => x.GetPredecessors().Count() > 1).Any())
            {
                return (null, null);
            }

            return (firstNode, lastNode);            
        }

        BaseNodeViewModel GetFirstNodeOfSequence(List<BaseNodeViewModel> selectedNodes)
        {
            Dictionary<string, bool> firstNode = selectedNodes.Where(x => x != StartState).ToDictionary(x => x.Name, x => false);

            var connects = selectedNodes.SelectMany(x => x.Transitions.Items.Where(y => y.Connect != null)).Select(c => c.Connect);

            var selectedConnects = connects.Where(x => selectedNodes.Contains(x.ToConnector.Node) && selectedNodes.Contains(x.FromConnector.Node));

            if (selectedConnects.Count() != selectedNodes.Count() - 1)
            {
                return null;
            }

            foreach (var connect in selectedConnects)
            {
                firstNode[connect.ToConnector.Node.Name] = true;
            }

            var firstNodeName = firstNode.Where(x => !x.Value).Select(x => x.Key).ToList();

            if (firstNodeName.Count() == 1)
            {
                return selectedNodes.Single(x => x.Name == firstNodeName.First());
            }

            return null;
        }

        BaseNodeViewModel GetLastNodeOfSequence(List<BaseNodeViewModel> selectedNodes)
        {
            Dictionary<string, bool> lastNode = selectedNodes.Where(x => x != StartState).ToDictionary(x => x.Name, x => false);

            var connects = selectedNodes.SelectMany(x => x.Transitions.Items.Where(y => y.Connect != null)).Select(c => c.Connect);

            var selectedConnects = connects.Where(x => selectedNodes.Contains(x.ToConnector.Node) && selectedNodes.Contains(x.FromConnector.Node));

            if (selectedConnects.Count() != selectedNodes.Count() - 1)
            {
                return null;
            }

            foreach (var connect in selectedConnects)
            {
                lastNode[connect.FromConnector.Node.Name] = true;
            }
            var lastNodeName = lastNode.Where(x => !x.Value).Select(x => x.Key).ToList();

            if (lastNodeName.Count() == 1)
            {
                return selectedNodes.Single(x => x.Name == lastNodeName.First());
            }

            return null;
        }
        #endregion


        private void New()
        {
            if (!WithoutSaving())
                return;

            ClearScheme();

            this.SetupStartState();
        }

        public void LoadScheme(string filePath)
        {
            ClearScheme();

            Mouse.OverrideCursor = Cursors.Wait;

            WithoutMessages = true;

            ParseSchemeFromXml(filePath);

            Mouse.OverrideCursor = null;

            WithoutMessages = false;

            LogDebug("Scheme was loaded from file \"{0}\"", SchemePath);
        }

        private void Open()
        {
            if (!WithoutSaving())
                return;
            var defaultPath = PathHelper.GetWorkflowProjectDirectory(PathHelper.GetDirectoryName(SchemePath)) + "\\WorkflowEditor";
            Dialog.ShowOpenFileDialog("XML-File | *.xml", SchemeName(), "Import scheme from xml file", defaultPath);

            if (Dialog.Result != DialogResult.Ok)
                return;

            Mouse.OverrideCursor = Cursors.Wait;

            ClearScheme();

            WithoutMessages = true;

            ParseSchemeFromXml(Dialog.FileName);

            Mouse.OverrideCursor = null;

            WithoutMessages = false;

            EdgeCount = MainWindowViewModel.Transitions.Count;

            LogDebug("Scheme was loaded from file \"{0}\"", SchemePath);
        }

        public void UpdateSchemeFromCode(CodeToSchemeComparer CtS)
        {
            CtS.Synchronize();

            ICommandWithUndoRedo.StackRedo.Clear();

            ICommandWithUndoRedo.StackUndo.Clear();
        }

        private List<BaseNodeViewModel> GetOrderdNodes()
        {
            List<BaseNodeViewModel> orderdSequence = new List<BaseNodeViewModel>();

            var stack = new Stack<BaseNodeViewModel>();

            stack.Push(StartState);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                if (orderdSequence.Contains(node))
                {
                    continue;
                }
                orderdSequence.Add(node);

                foreach (var successors in node.GetSuccessors())
                {
                    if (!orderdSequence.Contains(successors))
                    {
                        stack.Push(successors);
                    }
                }
            }
            return orderdSequence;
        }



        private void SaveProject()
        {
            if (string.IsNullOrEmpty(SchemePath))
            {
                SaveProjectAs();
            }
            else
            {
                WithValidateScheme(() =>
                {
                    SaveProject(SchemePath);
                });
            }
        }


        private void SaveProjectAs()
        {
            WithValidateScheme(() =>
            {
                var defaultPath = PathHelper.GetWorkflowProjectDirectory(PathHelper.GetDirectoryName(SchemePath)) + "\\WorkflowEditor";
                Dialog.ShowSaveFileDialog("XML-File | *.xml", SchemeName(), "SaveProject scheme as...", defaultPath);

                if (Dialog.Result != DialogResult.Ok)
                    return;

                SaveProject(Dialog.FileName);
            });
        }

        private void SaveProject(string fileName)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            ParseSchemeToXml(fileName);

            ProjectSaved = true;

            SchemePath = fileName;

            Mouse.OverrideCursor = null;

            LogDebug("Scheme was saved as \"{0}\"", SchemePath);
        }


        private void ExportCode()
        {
            if (string.IsNullOrEmpty(CodePath))
            {
                ExportCodeAs();
            }
            else
            {
                WithValidateScheme(() =>
                {
                    if (CanUpdateToCode())
                    {
                        ExportSchemeAsCode(CodePath);
                    }                    
                });
            }
        }

        private void ExportCodeAs()
        {
            WithValidateScheme(() =>
            {
                var defaultPath = PathHelper.GetWorkflowProjectDirectory(PathHelper.GetDirectoryName(SchemePath)) + "\\WorkflowFiles";
                Dialog.ShowSaveFileDialog("C#-File | *.cs", CodePathName(), "Save Scheme as Code to...", defaultPath);
                if (Dialog.Result != DialogResult.Ok)
                    return;
                ExportSchemeAsCode(Dialog.FileName);
            });
        }

        private void ExportSchemeAsCode(string filePath)
        {
            var fileName = PathHelper.GetFileNameWithoutExtension(filePath);

            var cG = new CodeGenerator(
            
                _Usings: new List<string>()
                {
                    "mitoSoft.StateMachine.AdvancedStateMachines",
                    "mitoSoft.Workflows"
                },
                _Namespace: "GeneratedWorkflow",
                _WorkflowName: fileName,
                _BaseStateMachine: "AdvancedStateMachine",
                _FilePath:filePath,
                _SchemeNodes: GetOrderdNodes()
            );
            cG.Generate();

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {
                cG.OutputFile.ForEach(x =>
                {
                    file.WriteLine(x);
                });
            }

            CodePath = filePath;

            CodeSaved = true;
        }

        private void WithValidateScheme(Action action)
        {
            var unReachable = ValidateScheme();

            if (unReachable.Count < 1)
            {
                action.Invoke();
            }
            else
            {
                LogError("Nodes without connects: {0}", string.Join(',', unReachable));
            }
        }

        private List<string> ValidateScheme()
        {
            Dictionary<string, bool> forValidate = Nodes.Items.Where(x => x != StartState).ToDictionary(x => x.Name, x => false);

            foreach (var connect in Connects)
            {
                forValidate[connect.ToConnector.Node.Name] = true;
            }
            return forValidate.Where(x => !x.Value).Select(x => x.Key).ToList();
        }


        private void ClearScheme()
        {
            this.Nodes.Clear();

            this.Connects.Clear();

            this.SeqPattern.Clear();

            this.NodesCount = 0;

            this.TransitionsCount = 0;

            this.EdgeCount = 0;

            this.SchemePath = "";

            this.CodePath = "";

            this.ImagePath = "";

            WithoutMessages = false;

            this.Messages.Clear();

            ProjectSaved = true;
        }

        bool WithoutSaving()
        {
            if (!ProjectSaved)
            {
                Dialog.ShowMessageBox("Exit without saving ?", "Exit without saving", MessageBoxButton.YesNo);

                return Dialog.Result == DialogResult.Yes;
            }
            return true;
        }

        public bool CanUpdateFromCode()
        {
             Dialog.ShowMessageBox("Want to update scheme from code ?", "Change in workflow code detetcted!", MessageBoxButton.YesNo);

             return Dialog.Result == DialogResult.Yes;
        }

        public bool CanUpdateToCode()
        {
            if (!CodeSaved)
            {
                Dialog.ShowMessageBox("Want to update code ?", "Code write", MessageBoxButton.YesNo);

                return Dialog.Result == DialogResult.Yes;
            }
            return false;
        }

        private void Exit()
        {
            if (!WithoutSaving())
                return;

            this.NeedExit = true;
        }

        public List<BaseNodeViewModel> GetAllNodesWithChild()
        {
            var allNodes = new List<BaseNodeViewModel>();

            foreach (var item in Nodes.Items.ToList())
            {
                item.GetAllNodes(allNodes);
            }
            return allNodes;
        }

        #region Select

        private void SelectedAll()
        {
            foreach (var node in Nodes.Items)
            {
                node.Selected = true;
            }
        }

        private void UnSelectedAll()
        {
            foreach (var node in Nodes.Items)
            {
                node.Selected = false;

                node.CommandUnSelectedAllConnectors.ExecuteWithSubscribe();
            }
        }

        private void SelectNodes()
        {
            Point selectorPoint1 = Selector.Point1WithScale;

            Point selectorPoint2 = Selector.Point2WithScale;

            foreach (BaseNodeViewModel node in Nodes.Items)
            {
                node.Selected = MyUtils.CheckIntersectTwoRectangles(node.Point1, node.Point2, selectorPoint1, selectorPoint2);
            }
        }

        private void SelectConnects()
        {
            Point cutterStartPoint = Cutter.StartPoint;

            Point cutterEndPoint = Cutter.EndPoint;

            var connects = Connects.Where(x => MyUtils.CheckIntersectTwoRectangles(MyUtils.GetStartPointDiagonal(x.StartPoint, x.EndPoint), MyUtils.GetEndPointDiagonal(x.StartPoint, x.EndPoint),
                                               MyUtils.GetStartPointDiagonal(cutterStartPoint, cutterEndPoint), MyUtils.GetEndPointDiagonal(cutterStartPoint, cutterEndPoint)));
            foreach (var connect in Connects)
            {
                connect.FromConnector.Selected = false;
            }

            foreach (var connect in connects)
            {
                connect.FromConnector.Selected = MyUtils.CheckIntersectCubicBezierCurveAndLine(connect.StartPoint, connect.Point1, connect.Point2, connect.EndPoint, cutterStartPoint, cutterEndPoint);
            }
        }

        #endregion

        #region Zoom

        private void ZoomIn()
        {
            Zoom((ScaleCenter, 1));
        }

        private void ZoomOut()
        {
            Zoom((ScaleCenter, -1));
        }

        private void Zoom((Point point, double delta) element)
        {
            ScaleCenter = element.point;
            double scaleValue = RenderTransformMatrix.M11;
            bool DeltaIsZero = (element.delta == 0);
            bool DeltaMax = ((element.delta > 0) && (scaleValue > ScaleMax));
            bool DeltaMin = ((element.delta < 0) && (scaleValue < ScaleMin));
            if (DeltaIsZero || DeltaMax || DeltaMin)
                return;

            double zoom = element.delta > 0 ? ScaleStep : 1 / ScaleStep;
            RenderTransformMatrix = MatrixExtension.ScaleAtPrepend(RenderTransformMatrix, zoom, zoom, element.point.X, element.point.Y);
        }

        private void ZoomOriginalSize()
        {
            RenderTransformMatrix = Matrix.Identity;
        }

        #endregion

        #region Collapse/Expand

        private void CollapseUpAll()
        {
            foreach (var node in Nodes.Items)
            {
                node.IsCollapse = true;
            }
        }

        private void ExpandDownAll()
        {
            foreach (var node in Nodes.Items)
            {
                node.IsCollapse = false;
            }
        }

        private void CollapseUpSelected()
        {
            foreach (var node in Nodes.Items.Where(x => x.Selected))
            {
                node.IsCollapse = true;
            }
        }

        private void ExpandDownSelected()
        {
            foreach (var node in Nodes.Items.Where(x => x.Selected))
            {
                node.IsCollapse = false;
            }
        }

        #endregion

        #region PictureExport
        private void ExportToPNG()
        {
            Dialog.ShowSaveFileDialog("PNG Image (.png)|*.png", SchemeName(), "Export scheme to PNG");
            if (Dialog.Result != DialogResult.Ok)
                return;
            ImageFormat = ImageFormats.PNG;
            ImagePath = Dialog.FileName;
        }

        private void ExportToJPEG()
        {
            Dialog.ShowSaveFileDialog("JPEG Image (.jpeg)|*.jpeg", SchemeName(), "Export scheme to JPEG");
            if (Dialog.Result != DialogResult.Ok)
                return;
            ImageFormat = ImageFormats.JPEG;
            ImagePath = Dialog.FileName;
        }
        #endregion

    }
}
