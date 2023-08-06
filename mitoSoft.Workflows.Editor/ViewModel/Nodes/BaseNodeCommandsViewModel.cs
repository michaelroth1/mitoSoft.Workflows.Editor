using DynamicData;
using ReactiveUI;
using mitoSoft.Workflows.Editor.Helpers.Enums;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Windows;
using mitoSoft.Workflows.Editor.Helpers.Enums;

namespace mitoSoft.Workflows.Editor.ViewModel
{
    public abstract partial class BaseNodeViewModel
    {
        public ReactiveCommand<Unit, Unit> CommandUnSelectedAllConnectors { get; set; }

        public ReactiveCommand<Unit, Unit> CommandAddEmptyConnector { get; set; }

        public ReactiveCommand<SelectMode, Unit> CommandSelect { get; set; }

        public ReactiveCommand<Point, Unit> CommandMove { get; set; }

        public ReactiveCommand<(int index, ConnectorViewModel connector), Unit> CommandAddConnectorWithConnect { get; set; }

        public ReactiveCommand<ConnectorViewModel, Unit> CommandDeleteConnectorWithConnect { get; set; }

        public ReactiveCommand<string, Unit> CommandValidateName { get; set; }

        public ReactiveCommand<ConnectorViewModel, Unit> CommandSelectWithShiftForConnectors { get; set; }

        public ReactiveCommand<ConnectorViewModel, Unit> CommandSetConnectorAsStartSelect { get; set; }

        public ReactiveCommand<ConnectorViewModel, Unit> CommandMoveTransitionUp { get; set; }

        public ReactiveCommand<ConnectorViewModel, Unit> CommandMoveTransitionDown { get; set; }


        public virtual void SetupCommands()
        {
            CommandSelect = ReactiveCommand.Create<SelectMode>(Select);

            CommandMove = ReactiveCommand.Create<Point>(Move);

            CommandAddEmptyConnector = ReactiveCommand.Create(AddEmptyConnector);

            CommandSelectWithShiftForConnectors = ReactiveCommand.Create<ConnectorViewModel>(SelectWithShiftForConnectors);

            CommandSetConnectorAsStartSelect = ReactiveCommand.Create<ConnectorViewModel>(SetConnectorAsStartSelect);

            CommandUnSelectedAllConnectors = ReactiveCommand.Create(UnSelectedAllConnectors);

            CommandAddConnectorWithConnect = ReactiveCommand.Create<(int index, ConnectorViewModel connector)>(AddConnectorWithConnect);

            CommandDeleteConnectorWithConnect = ReactiveCommand.Create<ConnectorViewModel>(DeleteConnectorWithConnec);

            CommandValidateName = ReactiveCommand.Create<string>(ValidateName);

            CommandMoveTransitionUp = ReactiveCommand.Create<ConnectorViewModel>((x) => MoveTransitionUp(x));

            CommandMoveTransitionDown = ReactiveCommand.Create<ConnectorViewModel>((x) => MoveTransitionDown(x));

            NotSavedSubscrube();
        }

        protected void NotSavedSubscrube()
        {
            CommandMove.Subscribe(_ => NotSaved());

            CommandAddConnectorWithConnect.Subscribe(_ => NotSavedCode());

            CommandDeleteConnectorWithConnect.Subscribe(_ => NotSavedCode());

            CommandValidateName.Subscribe(_ => NotSavedCode());

            CommandMoveTransitionUp.Subscribe(_ => NotSavedCode());

            CommandMoveTransitionDown.Subscribe(_ => NotSavedCode());
        }

        public virtual void ValidateName(string newName) { }

        protected void NotSaved()
        {
            NodesCanvas.ProjectSaved = false;
        }
        protected void NotSavedCode()
        {
            NodesCanvas.ProjectSaved = false;
            NodesCanvas.CodeSaved = false;
        }

        public int GetConnectorIndex(ConnectorViewModel connector)
        {
            return Transitions.Items.IndexOf(connector);
        }

        public virtual void AddConnectorWithConnect((int index, ConnectorViewModel connector) element)
        {
            Transitions.Insert(element.index, element.connector);

            if (element.connector.Connect != null)
            {
                NodesCanvas.CommandAddConnect.ExecuteWithSubscribe(element.connector.Connect);
            }
        }
        public void DeleteConnectorWithConnec(ConnectorViewModel connector)
        {
            if (connector.Connect != null)
            {
                NodesCanvas.CommandDeleteConnect.ExecuteWithSubscribe(connector.Connect);
            }
            Transitions.Remove(connector);
        }
        protected void Select(SelectMode selectMode)
        {
            if (selectMode == SelectMode.ClickWithCtrl)
            {
                this.Selected = !this.Selected;

                return;
            }
            else if ((selectMode == SelectMode.Click) && (!Selected))
            {
                NodesCanvas.CommandUnSelectAll.ExecuteWithSubscribe();

                this.Selected = true;
            }
        }

        protected void Move(Point delta)
        {
            //Point moveValue = delta.Division(NodesCanvas.Scale.Value);
            Point1 = Point1.Addition(delta);
        }

        public void UnSelectedAllConnectors()
        {
            foreach (var transition in Transitions.Items)
            {
                transition.Selected = false;
            }

            IndexStartSelectConnectors = 0;
        }

        protected void SetConnectorAsStartSelect(ConnectorViewModel viewModelConnector)
        {
            IndexStartSelectConnectors = Transitions.Items.IndexOf(viewModelConnector) - 1;
        }

        protected void SelectWithShiftForConnectors(ConnectorViewModel viewModelConnector)
        {
            if (viewModelConnector == null)

                return;
            var transitions = this.Transitions.Items.Skip(1);

            int indexCurrent = transitions.IndexOf(viewModelConnector);

            int indexStart = IndexStartSelectConnectors;

            UnSelectedAllConnectors();

            IndexStartSelectConnectors = indexStart;

            transitions = transitions.Skip(Math.Min(indexCurrent, indexStart)).SkipLast(Transitions.Count - Math.Max(indexCurrent, indexStart) - 2);

            foreach (var transition in transitions)
            {
                transition.Selected = true;
            }
        }

        private void MoveTransitionUp(ConnectorViewModel connector)
        {
            var selectedIndex = Transitions.Items.IndexOf(connector);

            if (selectedIndex > 1)
            {                         
                Transitions.RemoveAt(selectedIndex);

                Transitions.Insert(selectedIndex - 1, connector);

                NodesCanvas.MainWindowViewModel.UpdateConnectedTransitions(this);
            }
        }

        private void MoveTransitionDown(ConnectorViewModel connector)
        {
            var selectedIndex = Transitions.Items.IndexOf(connector);

            if (selectedIndex < Transitions.Count - 1)
            {               
                Transitions.RemoveAt(selectedIndex);

                Transitions.Insert(selectedIndex + 1, connector);

                NodesCanvas.MainWindowViewModel.UpdateConnectedTransitions(this);
            }
        }
    }
}

