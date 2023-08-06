using System;
using System.Windows;
using System.Windows.Media;
using System.Reactive.Linq;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;

using DynamicData.Binding;
using System.Linq;
using System.Xml.Linq;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using DynamicData;
using System.Collections.ObjectModel;
using mitoSoft.Workflows.Editor.ViewModel;
using System.Collections.Generic;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public class NodeViewModel : BaseNodeViewModel
    {
        public override int HEIGHT { get; set; } = 54;
        
        public NodeViewModel(NodesCanvasViewModel nodesCanvas, string name, Point point = default(Point)):base(nodesCanvas, name, point) { }

        public override void SetupSubscriptions()
        {
            base.SetupSubscriptions();
        }

        public override void SetupConnectors()
        {
            Input = new ConnectorViewModel(NodesCanvas, this, "Input", Point1.Addition(0, 30));

            Output = new ConnectorViewModel(NodesCanvas, this, "Output", Point1.Addition(80, 54))
            {
                Visible = null
            };
            AddEmptyConnector();
        }

        public override void ValidateName(string newName)
        {
            NodesCanvas.CommandValidateNodeName.ExecuteWithSubscribe((this, newName));
        }

        public override List<BaseNodeViewModel> GetAllNodes(List<BaseNodeViewModel> allNodes)
        {
            allNodes.Add(this);

            return allNodes;
        }

        public override XElement ToXElement()
        {
            XElement element = new XElement("State");

            element.Add(new XAttribute("Name", Name));
            
            element.Add(new XAttribute("NodeType", "Node"));

            //element.Add(new XAttribute("EnterAction", EnterAction));

            //element.Add(new XAttribute("ExitAction", ExitAction));

            return element;
        }

        public override XElement ToVisualizationXElement()
        {
            XElement element = ToXElement();

            element.Add(new XAttribute("Position", PointExtensition.PointToString(Point1)));

            element.Add(new XAttribute("IsCollapse", IsCollapse.ToString()));

            return element;
        }

        public new static NodeViewModel FromXElement(NodesCanvasViewModel nodesCanvas, XElement node, out string errorMessage, Func<string, bool> nodeExits, bool addOnCanvas = true)
        {
            errorMessage = null;

            string name = node.Attribute("Name")?.Value;

            //string enterAction = node.Attribute("EnterAction")?.Value;

            //string exitAction = node.Attribute("ExitAction")?.Value;

            var viewModelNode = new NodeViewModel(nodesCanvas, name);           

            //viewModelNode.EnterAction = enterAction;

            //viewModelNode.ExitAction = exitAction;

            return viewModelNode;
        }
    }
}
