using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using mitoSoft.Workflows.Editor.View;
using System.Reflection;
using System.Xml.Linq;
using DynamicData.Binding;
using DynamicData;
using System.Windows.Media;
using System.Reactive.Linq;
using ReactiveUI.Validation.Helpers;
using System.Linq;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System.Collections.ObjectModel;
using mitoSoft.Workflows.Editor.ViewModel;

using System.Reactive;
using System.Globalization;
using System.Windows.Data;
using System.IO;
using mitoSoft.Workflows.Editor.Helpers.SchemeUpdater;
using mitoSoft.Workflows.Editor.Helpers.Enums;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public class ParallelNodeViewModel : BaseNodeViewModel
    {
        public override int HEIGHT { get; set; } = 105;

        public ObservableCollectionExtended<string> ParallelStates { get; set; } = new ObservableCollectionExtended<string>();

        public ReactiveCommand<string, Unit> CommandRemoveParallelState { get; set; }

        public ReactiveCommand<string, Unit> CommandAddParallelState { get; set; }

        public ReactiveCommand<Unit, Unit> CommandAddParallelStateNewWindow { get; set; }

        public ReactiveCommand<Unit, Unit> CommandOpenParallelState { get; set; }


        public ParallelNodeViewModel(NodesCanvasViewModel nodesCanvas, string name, Point point = default(Point)) : base(nodesCanvas, name, point)
        {
            CommandRemoveParallelState = ReactiveCommand.Create<string>(RemoveParallelState);
            
            CommandAddParallelState = ReactiveCommand.Create<string>(AddParallelState);

            CommandAddParallelStateNewWindow = ReactiveCommand.Create(AddParallelStateNewWindow);

            CommandOpenParallelState = ReactiveCommand.Create(OpenParallelState);
        }

        private void OpenParallelState()
        {
            var defaultPath = PathHelper.GetWorkflowProjectDirectory(PathHelper.GetDirectoryName(NodesCanvas.SchemePath)) + "\\WorkflowEditor";
            
            NodesCanvas.Dialog.ShowOpenFileDialog("XML-File | *.xml", NodesCanvas.SchemeName(), "Import scheme from xml file", defaultPath);
            
            if (NodesCanvas.Dialog.Result == DialogResult.Ok)
            {
                CommandAddParallelState.ExecuteWithSubscribe(NodesCanvas.Dialog.FileName);
            }              
        }

        private void RemoveParallelState(string parallelState)
        {
            ParallelStates.Remove(parallelState);
        }

        private void AddParallelStateNewWindow()
        {
            var ParallelWindow = new MainWindow(NodesCanvas.MainWindowViewModel);

            ParallelWindow.ShowDialog();
        }

        private void AddParallelState(string parallelState)
        {
            if (!ParallelStates.Contains(parallelState) && !string.IsNullOrEmpty(parallelState))
            {
                ParallelStates.Add(parallelState);
            }
        }

        public override void SetupConnectors()
        {
            Input = new ConnectorViewModel(NodesCanvas, this, "Input", Point1.Addition(0, 80));

            Output = new ConnectorViewModel(NodesCanvas, this, "Output", Point1.Addition(80, 54))
            {
                Visible = null
            };
            AddEmptyConnector();
        }

        public override void SetupSubscriptions()
        {
            base.SetupSubscriptions();
        }


        public override void ValidateName(string newName)
        {
            NodesCanvas.CommandValidateNodeName.ExecuteWithSubscribe((this, newName));
        }

        public override XElement ToXElement()
        {
            XElement element = new XElement("ParallelNode");

            element.Add(new XAttribute("Name", Name));

            foreach (var x in ParallelStates)
            {
                XElement parallelState = new XElement("ParallelState");

                element.Add(parallelState);

                parallelState.Add(new XAttribute("FilePath", x));
            }

            return element;
        }

        public override XElement ToVisualizationXElement()
        {
            XElement element = ToXElement();

            element.Add(new XAttribute("Position", PointExtensition.PointToString(Point1)));

            element.Add(new XAttribute("IsCollapse", IsCollapse.ToString()));

            return element;
        }

        public static new ParallelNodeViewModel FromXElement(NodesCanvasViewModel nodesCanvas, XElement node, out string errorMessage, Func<string, bool> nodeExits, bool addOnCanvas = true)
        {
            errorMessage = null;

            string name = node.Attribute("Name")?.Value;

            var viewModelNode = new ParallelNodeViewModel(nodesCanvas, name);

            var parallelNodes = node?.Elements()?.ToList() ?? new List<XElement>();

            foreach (var paraState in parallelNodes)
            {               
                viewModelNode.ParallelStates.Add(paraState.Attribute("FilePath")?.Value);
            }

            return viewModelNode;
        }

        public override List<BaseNodeViewModel> GetAllNodes(List<BaseNodeViewModel> allNodes)
        {
            allNodes.Add(this);

            return allNodes;
        }
    }
}
