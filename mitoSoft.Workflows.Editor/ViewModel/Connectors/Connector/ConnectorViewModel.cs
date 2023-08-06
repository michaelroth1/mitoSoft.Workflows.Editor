﻿using System.Windows.Media;
using System.Windows;

using ReactiveUI.Fody.Helpers;
using ReactiveUI;

using mitoSoft.Workflows.Editor.Helpers;
using System;
using System.Xml.Linq;
using System.Linq;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System.Reactive.Linq;
using DynamicData;
using mitoSoft.Workflows.Editor.ViewModel;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class ConnectorViewModel : ReactiveObject
    {
        [Reactive] public Point PositionConnectPoint { get; set; } 
        [Reactive] public string Name { get; set; }
        [Reactive] public bool TextEnable { get; set; } = false;
        [Reactive] public bool? Visible { get; set; } = true;
        [Reactive] public bool FormEnable { get; set; } = true;
        [Reactive] public Brush FormStroke { get; set; }
        [Reactive] public Brush FormFill { get; set; }
        [Reactive] public Brush Foreground { get; set; }
        [Reactive] public double FormStrokeThickness { get; set; } = 1;
        [Reactive] public BaseNodeViewModel Node { get; set; }
        [Reactive] public ConnectViewModel Connect { get; set; }
        [Reactive] public bool ItsLoop { get; set; } = false;
        [Reactive] public NodesCanvasViewModel NodesCanvas { get; set; }
        [Reactive] public bool Selected { get; set; }
        [Reactive] public bool SharedTrigger { get; set; }
        [Reactive] public string TransiAction { get; set; }
        [Reactive] public string TransiCondition { get; set; }

        public string ToConnectorNodeName => ItsLoop ? Node.Name : Connect.ToConnector.Node.Name;

        public string FromConnectorNodeName => ItsLoop ? Node.Name : Connect.FromConnector.Node.Name;


        public ConnectorViewModel(NodesCanvasViewModel nodesCanvas, BaseNodeViewModel viewModelNode, string name, Point myPoint)
        {
            Node = viewModelNode;
            NodesCanvas = nodesCanvas;
            Name = name;
            PositionConnectPoint = myPoint;
            //TransiAction = $"Console.WriteLine(\"{Name}-Action\");";
            //TransiCondition = $"Console.WriteLine(\"{Name}-Condition\"); return true;";
            TransiAction = "";
            TransiCondition = "return true;";
            SetupCommands();
            SetupSubscriptions();
        }
        #region Setup Subscriptions
        private void SetupSubscriptions()
        {
            this.WhenAnyValue(x => x.Selected).Subscribe(value => Select(value));
            this.WhenAnyValue(x => x.NodesCanvas.Theme).Subscribe(_ => UpdateResources());
            this.WhenAnyValue(x => x.Selected).Subscribe(value => { this.NodesCanvas.SelectedTransition = value ? this : null; });

            if (this.Name!="Input")
            {
                this.WhenAnyValue(x => x.Node.HeaderWidth).Buffer(2, 1).Subscribe(x => UpdatePositionOnWidthChange(x[1] - x[0]));
                
                if (this.Name != "Output")
                {
                    this.WhenAnyValue(x => x.Node.TransitionsForView.Count).Subscribe(x => UpdatePositionOnTransitionCountChange());                   
                }                
            }

            this.WhenAnyValue(x => x.Node.Point1).Buffer(2, 1).Subscribe(value => PositionConnectPoint = PositionConnectPoint.Addition(value[1].Subtraction(value[0])));
        }

        private void UpdatePositionOnTransitionCountChange()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                int index = Node.Transitions.Items.IndexOf(this);
                this.PositionConnectPoint = Node.CurrentConnector.PositionConnectPoint.Addition(0, index*19);
            }
        }
        private void UpdatePositionOnWidthChange(double value)
        {
            this.PositionConnectPoint = this.PositionConnectPoint.Addition(value, 0);
        }
        private void UpdateResources()
        {
           Select(this.Selected);
            if (this.ItsLoop)
            {
                ToLoop();
            }
        }           
            
        #endregion Setup Subscriptions

        public BaseNodeViewModel GetTargetNode()
        {
            var TargetNode = Connect?.ToConnector?.Node;
            return TargetNode ?? Node;
        }

        public XElement ToXElement()
        {
            XElement element = new XElement("Transition");
            element.Add(new XAttribute("Name", Name));
            element.Add(new XAttribute("From", Node.Name));
            var ToConnectorName = this.Connect?.ToConnector?.Node.Name;
            element.Add(new XAttribute("To", ToConnectorName?? Node.Name));
            element.Add(new XAttribute("TransiAction", TransiAction));
            element.Add(new XAttribute("TransiCondition", TransiCondition));

            return element;
        }

        public static ConnectViewModel FromXElement(NodesCanvasViewModel nodesCanvas,XElement node, out string errorMessage, Func<string, bool> actionForCheck)
        {
            ConnectViewModel viewModelConnect = null;

            errorMessage = null;
            string name = node.Attribute("Name")?.Value;
            string from = node.Attribute("From")?.Value;
            string to = node.Attribute("To")?.Value;
            string transiAction = node.Attribute("TransiAction")?.Value;
            string transiCondition = node.Attribute("TransiCondition")?.Value;

            if (string.IsNullOrEmpty(name))
            {
                errorMessage = "Connect without name";
                return viewModelConnect;
            }
            if (string.IsNullOrEmpty(from))
            {
                errorMessage = "Connect without from point";
                return viewModelConnect;
            }
            if (string.IsNullOrEmpty(to))
            {
                errorMessage = "Connect without to point";
                return viewModelConnect;
            }
            if (actionForCheck(name))
            {
                errorMessage = String.Format("Contains more than one connect with name \"{0}\"", name);
                return viewModelConnect;
            }

            BaseNodeViewModel nodeFrom = nodesCanvas.Nodes.Items.Single(x => x.Name == from);
          
            BaseNodeViewModel nodeTo = nodesCanvas.Nodes.Items.Single(x => x.Name == to);

            nodeFrom.CurrentConnector.Name = name;

            if (nodeFrom == nodeTo)
            {
                nodeFrom.CurrentConnector.CommandSetAsLoop.ExecuteWithSubscribe();
            }
            else
            {
                viewModelConnect = new ConnectViewModel(nodeFrom.NodesCanvas, nodeFrom.CurrentConnector);
                
                viewModelConnect.ToConnector = nodeTo.Input;
                
                nodeFrom.CommandAddEmptyConnector.ExecuteWithSubscribe();
            }     

            return viewModelConnect;
        }
    }

}
