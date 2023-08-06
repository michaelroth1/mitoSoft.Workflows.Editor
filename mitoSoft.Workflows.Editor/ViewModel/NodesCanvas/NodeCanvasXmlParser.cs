using DynamicData;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.View;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Xml.Linq;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class NodesCanvasViewModel
    {
        //Elements
        const string STATEMACHINE = "StateMachine";
        const string STATES = "States";
        const string STARTSTATE = "StartState";
        const string TRANSITIONS = "Transitions";
        const string VISUAL = "Visualization";
        const string PATTERN = "TransformPattern";

        //Attributes
        const string NAME = "Name";


        public void ParseSchemeToXml(string fileName)
        {

            XDocument xDocument = new XDocument();

            XElement stateMachineXElement = new XElement(STATEMACHINE);

            xDocument.Add(stateMachineXElement);


            //States
            XElement states = new XElement(STATES);

            stateMachineXElement.Add(states);

            foreach (var state in Nodes.Items)
            {
                states.Add(state.ToXElement());
            }


            //StartState
            XElement startState = new XElement(STARTSTATE);

            stateMachineXElement.Add(startState);

            startState.Add(new XAttribute(NAME, StartState.Name));


            //Transitions
            XElement transitions = new XElement(TRANSITIONS);

            stateMachineXElement.Add(transitions);

            foreach (var transition in Nodes.Items.SelectMany(x => x.TransitionsForView.Where(y => !string.IsNullOrEmpty(y.Name))))
            {
                transitions.Add(transition.ToXElement());
            }

            //Visualization
            XElement visualizationXElement = new XElement(VISUAL);

            stateMachineXElement.Add(visualizationXElement);

            foreach (var state in Nodes.Items)
            {
                visualizationXElement.Add(state.ToVisualizationXElement());
            }

            //Pattern
            XElement patternXElement = new XElement(PATTERN);

            stateMachineXElement.Add(patternXElement);

            foreach (var seq in SeqPattern)
            {
                patternXElement.Add(GetSeqPatternXML(seq));
            }

            xDocument.Save(fileName);
        }

        public XElement GetSeqPatternXML(KeyValuePair<HashSet<string>, SequenceNodeViewModel> seq)
        {
            XElement seqPatternXElement = new XElement("SeqPattern");

            XElement seqNodeXElement = new XElement("SequenceNode");

            seqNodeXElement.Add(new XAttribute("Position", PointExtensition.PointToString(seq.Value.Point1)));

            seqNodeXElement.Add(new XAttribute("Name", seq.Value.Name));

            seqPatternXElement.Add(seqNodeXElement);

            XElement NodesXElement = new XElement("Nodes");

            seq.Key.ToList().ForEach((x) =>
            {
                XElement NodeXElement = new XElement("Node");

                NodeXElement.Add(new XAttribute("Name", x));

                NodesXElement.Add(NodeXElement);
            });

            XElement TransitionsXElement = new XElement("InternTransitions");

            var internTransis = seq.Value.GetInternTransitions();

            internTransis.ToList().ForEach(x => TransitionsXElement.Add(x.ToXElement()));

            seqPatternXElement.Add(NodesXElement);

            seqPatternXElement.Add(TransitionsXElement);

            return seqPatternXElement;
        }

        public void ParseSchemeFromXml(string fileName)
        {
            XDocument xDocument = XDocument.Load(fileName);

            XElement stateMachineXElement = xDocument.Element(STATEMACHINE);

            if (stateMachineXElement == null)
            {
                Error("not contanins StateMachine");
                return;
            }

            if (!ParseStates(stateMachineXElement))
                return;

            if (!ParseStartState(stateMachineXElement))
                return;

            if (!ParseTransitions(stateMachineXElement))
                return;

            if (!ParsePatterns(stateMachineXElement))
                return;

            SchemePath = fileName;

            ParseVisualization(stateMachineXElement);
        }

        bool ParseStates(XElement stateMachineXElement)
        {
            var States = stateMachineXElement.Element(STATES)?.Elements()?.ToList() ?? new List<XElement>();

            BaseNodeViewModel viewModelNode = null;

            foreach (var state in States)
            {
                viewModelNode = BaseNodeViewModel.FromXElement(this, state, out string StateErrorMesage, NodeExists, true);
            }

            return viewModelNode == null ? false : true;
        }

        bool ParseStartState(XElement stateMachineXElement)
        {
            var startStateElement = stateMachineXElement.Element("StartState");
            if (startStateElement == null)
            {
                this.SetupStartState();
                return true;
            }
            else
            {
                var startStateAttribute = startStateElement.Attribute("Name");
                if (startStateAttribute == null)
                {
                    Error("Start state element don't has name attribute");
                    return false;
                }
                else
                {
                    string startStateName = startStateAttribute.Value;
                    if (string.IsNullOrEmpty(startStateName))
                    {
                        Error(string.Format("Name attribute of start state is empty.", startStateName));
                        return false;
                    }

                    var startNode = this.Nodes.Items.SingleOrDefault(x => x.Name == startStateName);
                    if (startNode == null)
                    {
                        Error(string.Format("Unable to set start state. Node with name \"{0}\" don't exists", startStateName));
                        return false;
                    }
                    else
                    {
                        this.SetAsStart(startNode);
                        return true;
                    }
                }
            }
        }

        bool ParseTransitions(XElement stateMachineXElement)
        {
            var Transitions = stateMachineXElement.Element("Transitions")?.Elements()?.ToList() ?? new List<XElement>();

            ConnectViewModel viewModelConnect;

            Transitions?.Reverse();

            foreach (var transition in Transitions)
            {
                viewModelConnect = ConnectorViewModel.FromXElement(this, transition, out string errorMesage, ConnectsExist);

                if (WithError(errorMesage, x => Connects.Add(x), viewModelConnect))
                    return false;
            }
            return true;
        }

        bool ParsePatterns(XElement stateMachineXElement)
        {
            var patterns = stateMachineXElement.Element("TransformPattern")?.Elements()?.ToList() ?? new List<XElement>();

            bool ret = true;

            foreach (var pattern in patterns)
            {
                switch (pattern.Name.ToString())
                {
                    case "SeqPattern":

                        ret = ParseSequencePatternFromXElement(pattern);
                        break;

                    default:
                        ret = false;
                        break;
                }
            }
            return ret;
        }

        private bool ParseSequencePatternFromXElement(XElement pattern)
        {
            var sequenceXNode = pattern.Element("SequenceNode");

            string seqName = sequenceXNode.Attribute("Name")?.Value;

            var allnodes = GetAllNodesWithChild();

            SequenceNodeViewModel seqNode;

            var exists = allnodes.Where(x => x.Name == seqName);

            if (exists.Any())
            {
                seqNode = (SequenceNodeViewModel)allnodes.Single(x => x.Name == seqName);
            }
            else
            {
                seqNode = new SequenceNodeViewModel(this, seqName);
            }

            ParseInternTransition(pattern, seqNode);

            var nodes = pattern?.Element("Nodes")?.Elements()?.ToList() ?? new List<XElement>();

            var nodesTable = new HashSet<string>();

            nodes.ForEach(x => nodesTable.Add(x.Attribute("Name")?.Value));

            System.Windows.Point point;

            var pointAttribute = sequenceXNode.Attribute("Position")?.Value;

            if (PointExtensition.TryParseFromString(pointAttribute ?? "0, 0", out point))
            {
                seqNode.Point1 = point;
            }

            SeqPattern.Add(nodesTable, seqNode);

            return true;
        }

        void ParseInternTransition(XElement pattern, SequenceNodeViewModel seqNode)
        {
            var internTransis = pattern?.Element("InternTransitions")?.Elements()?.ToList() ?? new List<XElement>();

            internTransis?.Reverse();

            foreach (var transi in internTransis)
            {
                ConnectViewModel viewModelConnect;

                string name = transi.Attribute("Name")?.Value;

                string from = transi.Attribute("From")?.Value;

                string to = transi.Attribute("To")?.Value;

                string transiAction = transi.Attribute("TransiAction")?.Value;

                string transiCondition = transi.Attribute("TransiCondition")?.Value;

                BaseNodeViewModel nodeFrom = seqNode.SequenceNodes.Single(x => x.Name == from);

                BaseNodeViewModel nodeTo = seqNode.SequenceNodes.Single(x => x.Name == to);

                nodeFrom.CurrentConnector.Name = name;

                viewModelConnect = new ConnectViewModel(nodeFrom.NodesCanvas, nodeFrom.CurrentConnector);

                viewModelConnect.ToConnector = nodeTo.Input;

                nodeFrom.CommandAddEmptyConnector.ExecuteWithSubscribe();
            }
        }


        void ParseVisualization(XElement stateMachineXElement)
        {
            var visualizationStates = stateMachineXElement.Element("Visualization")?.Elements()?.ToList() ?? new List<XElement>();

            var nodes = this.GetAllNodesWithChild().ToDictionary(x => x.Name, x => x);

            foreach (var element in visualizationStates)
            {
                ParseVisu(element, nodes);
            }
        }


        public void ParseVisu(XElement element, Dictionary<string, BaseNodeViewModel> nodes)
        {
            List<XElement> childs;
            
            if (element.Name == "ParallelNode" || element.Name == "SubWorkflowNode")
            {
                childs = null;
            }
            else
            {
                childs = element.Elements()?.ToList();
            }            
            
            System.Windows.Point point;

            bool isCollapse;

            string name;

            string pointAttribute;

            string isCollapseAttribute;

            //var x = element.Elements()?.ToList();

            name = element.Attribute("Name")?.Value;

            if (nodes.TryGetValue(name, out BaseNodeViewModel node))
            {
                pointAttribute = element.Attribute("Position")?.Value;

                if (!PointExtensition.TryParseFromString(pointAttribute ?? "0, 0", out point))
                {
                    Error(String.Format("Error parse attribute \'position\' for state with name \'{0}\'", name));
                    return;
                }

                isCollapseAttribute = element.Attribute("IsCollapse")?.Value;

                if (!bool.TryParse(isCollapseAttribute, out isCollapse))
                {
                    Error(String.Format("Error parse attribute \'isCollapse\' for state with name \'{0}\'", name));
                    return;
                }

                node.Point1 = point;

                node.IsCollapse = isCollapse;
            }
            else
            {
                Error(String.Format("Visualization for state with name \'{0}\' that not exist", name));
                return;
            }

            childs?.ForEach(x => ParseVisu(x, nodes));
        }

        public bool WithError<T>(string errorMessage, Action<T> action, T obj)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                if (!object.Equals(obj, default(T)))
                    action.Invoke(obj);
            }
            else
            {
                Error(errorMessage);
                return true;
            }
            return false;
        }

        void Error(string errorMessage)
        {
            ClearScheme();

            LogError("File is not valid. " + errorMessage);

            this.SetupStartState();

            Mouse.OverrideCursor = null;
        }

        void SeqenceTransformError(string errorMessage)
        {
            
            LogError("Transform invalid: " + errorMessage);

            Mouse.OverrideCursor = null;
        }
    }
}
