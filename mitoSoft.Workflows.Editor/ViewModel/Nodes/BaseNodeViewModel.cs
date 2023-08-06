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
using System.CodeDom;
using mitoSoft.Workflows.Editor.View;
using System.Collections.Generic;
using static System.Windows.Forms.AxHost;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using mitoSoft.Workflows.Editor.Helpers.HashSetComparer;
using System.Threading.Channels;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public abstract partial  class BaseNodeViewModel : ReactiveValidationObject<NodeViewModel>
    {
        [Reactive] public virtual Point Point1 { get; set; }
        
        [Reactive] public virtual Point Point2 { get; set; }
        [Reactive] public virtual Size Size { get; set; }
        [Reactive] public virtual string Name { get; set; }
        [Reactive] public virtual bool NameEnable { get; set; } = true;
        [Reactive] public virtual bool Selected { get; set; }
        [Reactive] public virtual Brush BorderBrush { get; set; } = Application.Current.Resources["ColorNodeBorder"] as SolidColorBrush;
        [Reactive] public virtual bool? TransitionsVisible { get; set; } = true;
        [Reactive] public virtual bool? RollUpVisible { get; set; } = true;
        [Reactive] public virtual bool CanBeDelete { get; set; } = true;
        [Reactive] public virtual bool IsCollapse { get; set; }
        [Reactive] public virtual ConnectorViewModel Input { get; set; }
        [Reactive] public virtual ConnectorViewModel Output { get; set; }
        [Reactive] public virtual ConnectorViewModel CurrentConnector { get; set; }
        [Reactive] public virtual NodesCanvasViewModel NodesCanvas { get; set; }
        [Reactive] public virtual int IndexStartSelectConnectors { get; set; } = 0;

        [Reactive] public virtual string EnterAction { get; set; }
        [Reactive] public virtual string ExitAction { get; set; }

        [Reactive] public virtual double HeaderWidth { get; set; } = 80;
        public abstract int HEIGHT { get; set; }

        public SourceList<ConnectorViewModel> Transitions { get; set; } = new SourceList<ConnectorViewModel>();

        public ObservableCollectionExtended<ConnectorViewModel> TransitionsForView = new ObservableCollectionExtended<ConnectorViewModel>();
        
        public ObservableCollectionExtended<ConnectorViewModel> ConnectedTransitionsForView = new ObservableCollectionExtended<ConnectorViewModel>();


        public int Zindex { get; protected set; }


        public BaseNodeViewModel(NodesCanvasViewModel nodesCanvas, string name, Point point = default(Point))
        {
            NodesCanvas = nodesCanvas;
            
            Name = name;
            
            Zindex = nodesCanvas.Nodes.Count;
            
            Point1 = point;
            
            Transitions.Connect().ObserveOnDispatcher().Bind(TransitionsForView).Subscribe();
            //Transitions.Connect().Filter(x=>!string.IsNullOrEmpty(x.Name)).Bind(ConnectedTransitionsForView).Subscribe();      
            
            SetupConnectors();
            
            SetupCommands();
            
            SetupBinding();
            
            SetupSubscriptions();
        }

        public virtual void SetupConnectors() { }

        public virtual void SetupBinding() { }

        public virtual void SetupSubscriptions() 
        {
            this.WhenAnyValue(x => x.Selected).Subscribe(value => { this.BorderBrush = value ? Application.Current.Resources["ColorSelectedElement"] as SolidColorBrush : Brushes.LightGray; });
            
            this.WhenAnyValue(x => x.Selected).Subscribe(value => { this.NodesCanvas.SelectedNode = value ? this : null; });
            
            this.WhenAnyValue(x => x.TransitionsForView.Count).Buffer(2, 1).Select(x => (Previous: x[0], Current: x[1])).Subscribe(x => UpdateTransitionCount(x.Previous, x.Current));
            
            this.WhenAnyValue(x => x.Point1, x => x.Size).Subscribe(_ => UpdatePoint2());
            
            this.WhenAnyValue(x => x.IsCollapse).Subscribe(value => Collapse(value));

            this.WhenAnyValue(x => x.Name).Buffer(2, 1).Select(x => (Previous: x[0], Current: x[1])).Subscribe(x => NameChanged(x.Previous, x.Current));
        }

        public IEnumerable<BaseNodeViewModel> GetPredecessors()
        {
            var x = NodesCanvas.MainWindowViewModel.Transitions.Where(x => !x.ItsLoop && x.Connect != null && x.ToConnectorNodeName == this.Name).Select(n => n.Node);
            return x.Reverse();
        }

        public IEnumerable<BaseNodeViewModel> GetSuccessors()
        {
            return TransitionsForView.Where(x => !x.ItsLoop && x.Connect != null && x.FromConnectorNodeName == this.Name).Select(n => n.Connect.ToConnector.Node).Reverse();
        }

        

        public void UpdatePoint2()
        {
            Point2 = Point1.Addition(Size);
        }

        public void Collapse(bool value)
        {
            if (!value)
            {
                TransitionsVisible = true;
                
                Output.Visible = null;
            }
            else
            {
                TransitionsVisible = null;
               
                Output.Visible = true;
                
                UnSelectedAllConnectors();
            }
            NotSaved();
        }

        public virtual void AddEmptyConnector()
        {
            
            if (CurrentConnector != null)
            {
                CurrentConnector.TextEnable = true;
                
                CurrentConnector.FormEnable = false;

                var t = this.Transitions;

                if (string.IsNullOrEmpty(CurrentConnector.Name))
                {
                    CurrentConnector.Name = NodesCanvas.GetNameForTransition();
                }                
                NodesCanvas.LogDebug("Transition with name \"{0}\" was added", CurrentConnector.Name);
            }
            double width = Size.Width == 0 ? 80 : Size.Width;

            CurrentConnector = new ConnectorViewModel(NodesCanvas, this, "", Point1.Addition(width, this.HEIGHT))
            {
                TextEnable = false
            };
            Transitions.Insert(0, CurrentConnector);
        }

        public virtual void UpdateTransitionCount(int oldValue, int newValue)
        {
            if ((oldValue > 0) && (newValue > oldValue))
            {
                NodesCanvas.TransitionsCount++;
            }
        }
        private void NameChanged(string prev, string current)
        {
            List<(HashSet<string> oldKey, HashSet<string> newKey)> changes = new List<(HashSet<string>, HashSet<string>)>();

            foreach (var item in NodesCanvas.SeqPattern)
            {
                if (item.Key.Contains(prev))
                {
                    var newkey = new HashSet<string>(item.Key);

                    newkey.Remove(prev);

                    newkey.Add(current);

                    changes.Add((item.Key, newkey));
                }
            }
            changes.ForEach(x => NodesCanvas.SeqPattern.ChangeKey(x.oldKey, x.newKey));
        }

        public abstract List<BaseNodeViewModel> GetAllNodes(List<BaseNodeViewModel> allNodes);

        public abstract XElement ToXElement();

        public abstract XElement ToVisualizationXElement();

        public static BaseNodeViewModel FromXElement(NodesCanvasViewModel nodesCanvas, XElement state, out string errorMessage, Func<string, bool> nodeExits, bool addOnCanvas = true)
        {
            errorMessage = null;
            
            string name = state.Attribute("Name")?.Value;            

            if (string.IsNullOrEmpty(name))
            {
                errorMessage = "Node without name";

                return null;
            }

            if (nodeExits(name))
            {
                errorMessage = String.Format("Contains more than one state with name \"{0}\"", name);

                return null;
            }

            BaseNodeViewModel viewModelNode;

            switch (state.Name.ToString())
            {
                case "State":

                    viewModelNode = NodeViewModel.FromXElement(nodesCanvas, state, out errorMessage, nodeExits);
                    break;

                case "SequenceNode":

                    viewModelNode = SequenceNodeViewModel.FromXElement(nodesCanvas, state, out errorMessage, nodeExits);
                    break;

                case "ParallelNode":

                    viewModelNode = ParallelNodeViewModel.FromXElement(nodesCanvas, state, out errorMessage, nodeExits);
                    break;

                case "SubWorkflowNode":

                    viewModelNode = SubWorkflowNodeViewModel.FromXElement(nodesCanvas, state, out errorMessage, nodeExits);
                    break;

                default:
                    viewModelNode= null;
                    break;
            }

            if (addOnCanvas)
            {
                if (nodesCanvas.WithError(errorMessage, x => nodesCanvas.Nodes.Add(x), viewModelNode))
                    viewModelNode = null;
            }

            return viewModelNode;
        }

    }
}
