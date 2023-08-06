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
    public class SubWorkflowNodeViewModel : BaseNodeViewModel
    {
        public override int HEIGHT { get; set; } = 105;

        [Reactive] public string SubStateMachine { get; set; }

        public ReactiveCommand<Unit, Unit> CommandRemoveSubState { get; set; }

        public ReactiveCommand<string, Unit> CommandAddSubState { get; set; }

        public ReactiveCommand<Unit, Unit> CommandAddSubStateNewWindow { get; set; }

        public ReactiveCommand<Unit, Unit> CommandOpenSubState { get; set; }


        public SubWorkflowNodeViewModel(NodesCanvasViewModel nodesCanvas, string name, Point point = default(Point)) : base(nodesCanvas, name, point)
        {
            CommandRemoveSubState = ReactiveCommand.Create(RemoveSubState);

            CommandAddSubState = ReactiveCommand.Create<string>(AddSubState);

            CommandAddSubStateNewWindow = ReactiveCommand.Create(AddSubStateNewWindow);

            CommandOpenSubState = ReactiveCommand.Create(OpenSubState);
        }

        private void OpenSubState()
        {
            var defaultPath = PathHelper.GetWorkflowProjectDirectory(PathHelper.GetDirectoryName(NodesCanvas.SchemePath)) + "\\WorkflowEditor";

            NodesCanvas.Dialog.ShowOpenFileDialog("XML-File | *.xml", NodesCanvas.SchemeName(), "Import scheme from xml file", defaultPath);

            if (NodesCanvas.Dialog.Result == DialogResult.Ok)
            {
                CommandAddSubState.ExecuteWithSubscribe(NodesCanvas.Dialog.FileName);
            }
        }

        private void AddSubStateNewWindow()
        {
            var detail = new MainWindow(NodesCanvas.MainWindowViewModel);

            detail.ShowDialog();
        }

        private void AddSubState(string SubState)
        {
            SubStateMachine = SubState;
        }

        private void RemoveSubState()
        {
            SubStateMachine = String.Empty;
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
            XElement element = new XElement("SubWorkflowNode");

            element.Add(new XAttribute("Name", Name));

            XElement SubStateMachine = new XElement("SubStateMachine");

            element.Add(SubStateMachine);

            SubStateMachine.Add(new XAttribute("FilePath", this.SubStateMachine ?? ""));
            

            return element;
        }

        public override XElement ToVisualizationXElement()
        {
            XElement element = ToXElement();

            element.Add(new XAttribute("Position", PointExtensition.PointToString(Point1)));

            element.Add(new XAttribute("IsCollapse", IsCollapse.ToString()));

            return element;
        }

        public static new SubWorkflowNodeViewModel FromXElement(NodesCanvasViewModel nodesCanvas, XElement node, out string errorMessage, Func<string, bool> nodeExits, bool addOnCanvas = true)
        {
            errorMessage = null;

            string name = node.Attribute("Name")?.Value;      

            var subsequenceState = node?.Element("SubStateMachine");

            string subsequenceStateName = subsequenceState.Attribute("FilePath")?.Value;

            var viewModelNode = new SubWorkflowNodeViewModel(nodesCanvas, name);

            viewModelNode.SubStateMachine = subsequenceStateName;

            return viewModelNode;
        }

        public override List<BaseNodeViewModel> GetAllNodes(List<BaseNodeViewModel> allNodes)
        {
            allNodes.Add(this);
            return allNodes;
        }
    }
}
