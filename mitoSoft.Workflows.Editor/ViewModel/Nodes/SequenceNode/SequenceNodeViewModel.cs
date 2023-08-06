using DynamicData;
using DynamicData.Binding;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public class SequenceNodeViewModel : BaseNodeViewModel
    {
        public override int HEIGHT { get; set; } = 105;

        //public ObservableCollectionExtended<string> SequenceStates { get; set; } = new ObservableCollectionExtended<string>();

        public ObservableCollectionExtended<BaseNodeViewModel> SequenceNodes { get; set; } = new ObservableCollectionExtended<BaseNodeViewModel>();


        public SequenceNodeViewModel(NodesCanvasViewModel nodesCanvas, string name, Point point = default) : base(nodesCanvas, name, point) { }

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

        public IEnumerable<ConnectorViewModel> GetInternTransitions()
        {
            return SequenceNodes.SelectMany(x => x.Transitions.Items.Where(y => !string.IsNullOrEmpty(y.Name)));
        }

        public override List<BaseNodeViewModel> GetAllNodes(List<BaseNodeViewModel> allNodes)
        {
            allNodes.Add(this);

            foreach (var item in SequenceNodes)
            {
                item.GetAllNodes(allNodes);
            }

            return allNodes;
        }

        public override XElement ToXElement()
        {
            XElement element = new XElement("SequenceNode");

            element.Add(new XAttribute("Name", Name));

           // element.Add(new XAttribute("EnterAction", EnterAction));

           // element.Add(new XAttribute("ExitAction", ExitAction));

            foreach (var x in SequenceNodes)
            {
                element.Add(x.ToXElement());
            }

            return element;
        }

        private XElement ToVisuXElement()
        {
            XElement element = new XElement("SequenceNode");

            element.Add(new XAttribute("Name", Name));

           // element.Add(new XAttribute("EnterAction", EnterAction));

           // element.Add(new XAttribute("ExitAction", ExitAction));

            foreach (var x in SequenceNodes)
            {
                element.Add(x.ToVisualizationXElement());
            }

            return element;
        }

        public override XElement ToVisualizationXElement()
        {
            XElement element = ToVisuXElement();

            element.Add(new XAttribute("Position", PointExtensition.PointToString(Point1)));

            element.Add(new XAttribute("IsCollapse", IsCollapse.ToString()));

            return element;
        }

        public static new SequenceNodeViewModel FromXElement(NodesCanvasViewModel nodesCanvas, XElement node, out string errorMessage, Func<string, bool> nodeExits, bool addOnCanvas = true)
        {
            errorMessage = null;

            string name = node.Attribute("Name")?.Value;

            //string enterAction = node.Attribute("EnterAction")?.Value;

            //string exitAction = node.Attribute("ExitAction")?.Value;          
              
            var viewModelNode = new SequenceNodeViewModel(nodesCanvas, name);

            var sequenceNodes = node?.Elements()?.ToList() ?? new List<XElement>();


            foreach (var seqNodeX in sequenceNodes)
            {
                var n = BaseNodeViewModel.FromXElement(nodesCanvas, seqNodeX, out string errorMesage, nodeExits,false);
                
                viewModelNode.SequenceNodes.Add(n);
            }            

           // viewModelNode.EnterAction = enterAction;

           // viewModelNode.ExitAction = exitAction;

            return viewModelNode;
        }
    }
}
