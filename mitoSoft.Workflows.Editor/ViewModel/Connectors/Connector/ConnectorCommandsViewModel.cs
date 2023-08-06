using DynamicData;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public partial class ConnectorViewModel
    {
        public ReactiveCommand<Unit, Unit> CommandConnectPointDrag { get; set; }
        public ReactiveCommand<Unit, Unit> CommandConnectPointDrop { get; set; }
        public ReactiveCommand<Unit, Unit> CommandCheckConnectPointDrop { get; set; }
        public ReactiveCommand<Unit, Unit> CommandConnectorDrag { get; set; }
        public ReactiveCommand<Unit, Unit> CommandConnectorDragEnter { get; set; }
        public ReactiveCommand<Unit, Unit> CommandConnectorDrop { get; set; }
        public ReactiveCommand<Unit, Unit> CommandSetAsLoop { get; set; }
        public ReactiveCommand<SelectMode, Unit> CommandSelect { get; set; }
        public ReactiveCommand<string, Unit> CommandValidateName { get; set; }

        private void SetupCommands()
        {

            CommandConnectPointDrag = ReactiveCommand.Create(ConnectPointDrag);
            CommandConnectPointDrop = ReactiveCommand.Create(ConnectPointDrop);
            CommandSetAsLoop = ReactiveCommand.Create(SetAsLoop);
            CommandCheckConnectPointDrop = ReactiveCommand.Create(CheckConnectPointDrop);

            CommandConnectorDrag = ReactiveCommand.Create(ConnectorDrag);
            CommandConnectorDragEnter = ReactiveCommand.Create(ConnectorDragEnter);
            CommandConnectorDrop = ReactiveCommand.Create(ConnectorDrop);

            CommandValidateName = ReactiveCommand.Create<string>(ValidateName);

            CommandSelect = ReactiveCommand.Create<SelectMode>(Select);


            NotSavedSubscribe();
        }

        private void NotSavedSubscribe()
        {
            CommandSetAsLoop.Subscribe(_ => NotSaved());
            CommandValidateName.Subscribe(_ => NotSaved());
            CommandConnectorDragEnter.Subscribe(_ => NotSavedCode());
        }

        private void NotSaved()
        {
            NodesCanvas.ProjectSaved = false;
        }

        private void NotSavedCode()
        {
            NodesCanvas.ProjectSaved = false;
            NodesCanvas.CodeSaved = false;
        }

        private void SetAsLoop()
        {
            if(Node is SequenceNodeViewModel)
            {
                NodesCanvas.LogError("Cant Add SelfLoop to Sequence Node");
                return;
            }
            if (this == Node.Output)
                return;
            ToLoop();
            ItsLoop = true;

            Node.CommandAddEmptyConnector.ExecuteWithSubscribe();
        }

        private void ToLoop()
        {
            this.FormStrokeThickness = 0;
            this.FormFill = Application.Current.Resources["IconLoop"] as DrawingBrush;
        }

        private void ConnectPointDrag()
        {
            NodesCanvas.CommandAddDraggedConnect.ExecuteWithSubscribe(Node.CurrentConnector);
        }

        private void ConnectPointDrop()
        {
            var connect = NodesCanvas.DraggedConnect;
            if (connect.FromConnector.Node != this.Node)
            {              
                connect.ToConnector = this;
            }
            else
            {
                connect.FromConnector.SetAsLoop();
            }
        }

        private void CheckConnectPointDrop()
        {
            if (NodesCanvas.DraggedConnect.ToConnector == null)
            {
                NodesCanvas.CommandDeleteDraggedConnect.ExecuteWithSubscribe();
            }
            else
            {
                NodesCanvas.CommandAddConnectorWithConnect.Execute(Node.CurrentConnector);
                Node.CommandAddEmptyConnector.ExecuteWithSubscribe();
                CommandSelect.ExecuteWithSubscribe(SelectMode.Click);
                NodesCanvas.DraggedConnect = null;
            }
        }

        private void ConnectorDrag()
        {
            NodesCanvas.ConnectorPreviewForDrop = this;
        }

        private void ConnectorDragEnter()
        {
            if (Node != NodesCanvas.ConnectorPreviewForDrop.Node)
                return;

            int indexTo = Node.Transitions.Items.IndexOf(this);
            if (indexTo == 0)
                return;
            int count = this.Node.Transitions.Count;
            int indexFrom = this.Node.Transitions.Items.IndexOf(this.NodesCanvas.ConnectorPreviewForDrop);

            if ((indexFrom > -1) && (indexTo > -1) && (indexFrom < count) && (indexTo < count))
            {
                Point positionTo = this.Node.Transitions.Items.ElementAt(indexTo).PositionConnectPoint;
                Point position;
                //shift down
                if (indexTo > indexFrom)
                {
                    for (int i = indexTo; i >= indexFrom + 1; i--)
                    {
                        position = this.Node.Transitions.Items.ElementAt(i - 1).PositionConnectPoint;
                        this.Node.Transitions.Items.ElementAt(i).PositionConnectPoint = position;
                    }
                }
                //shift up
                else if (indexFrom > indexTo)
                {
                    for (int i = indexTo; i <= indexFrom - 1; i++)
                    {
                        position = this.Node.Transitions.Items.ElementAt(i + 1).PositionConnectPoint;
                        this.Node.Transitions.Items.ElementAt(i).PositionConnectPoint = position;
                    }
                }
                this.Node.Transitions.Items.ElementAt(indexFrom).PositionConnectPoint = positionTo;
                this.Node.Transitions.Move(indexFrom, indexTo);
            }
        }

        private void ConnectorDrop()
        {
            this.NodesCanvas.ConnectorPreviewForDrop = null;
        }
        private void ValidateName(string newName)
        {
            NodesCanvas.CommandValidateConnectName.ExecuteWithSubscribe((this, newName));
        }

        private void Select(bool value)
        {
            this.FormStroke = Application.Current.Resources["ColorNodesCanvasBackground"] as SolidColorBrush;
            this.Foreground = Application.Current.Resources[this.Selected ? "ColorSelectedElement" : "ColorConnectorForeground"] as SolidColorBrush;
            if (!this.ItsLoop)
                this.FormFill = Application.Current.Resources[this.Selected ? "ColorSelectedElement" : "ColorConnector"] as SolidColorBrush;
            else
                this.FormFill = Application.Current.Resources[this.Selected ? "IconSelectedLoop" : "IconLoop"] as DrawingBrush;           

        }

        private void Select(SelectMode selectMode)
        {
            switch (selectMode)
            {
                case SelectMode.Click:
                    {
                        if (!this.Selected)
                        {
                           this.Node.CommandSetConnectorAsStartSelect.ExecuteWithSubscribe(this);
                        }

                        var tempselect = this.Selected;
                        
                        foreach (var node in NodesCanvas.Nodes.Items)
                        {
                            node.CommandUnSelectedAllConnectors.ExecuteWithSubscribe();

                            this.Selected = !tempselect;
                        }                        
                        break;
                    }
                case SelectMode.ClickWithCtrl:
                    {
                        this.Selected = !this.Selected;

                        break;
                    }
                case SelectMode.ClickWithShift:
                    {
                        this.Node.CommandSelectWithShiftForConnectors.ExecuteWithSubscribe(this);

                        break;
                    }
            }
        }
    }
}
