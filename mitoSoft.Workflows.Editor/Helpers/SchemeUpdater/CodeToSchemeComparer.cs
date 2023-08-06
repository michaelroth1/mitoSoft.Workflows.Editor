using System.Collections.Generic;
using System.Linq;
using DynamicData;
using mitoSoft.StateMachine.AdvancedStateMachines;
using mitoSoft.Workflows;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using mitoSoft.Workflows.Editor.States;
using mitoSoft.Workflows.Editor.ViewModel;

namespace mitoSoft.Workflows.Editor.Helpers.SchemeUpdater
{
    public class CodeToSchemeComparer
    {
        List<State> WorkflowStates;

        List<State> SingleWorkflowStates;

        List<ParallelState> ParallelWorkflowStates;

        List<SequenceState> SequenceWorkflowStates;

        List<SubWorkflowState> SubWorkflowStates;

        List<TransitionState> TransitionDummys;

        List<mitoSoft.Workflows.Transition> WorkflowEdges;

        List<BaseNodeViewModel> SchemeNodes;

        List<NodeViewModel> SingleSchemeNodes;

        List<ParallelNodeViewModel> ParallelSchemeNodes;

        List<SequenceNodeViewModel> SequenceSchemeNodes;

        List<SubWorkflowNodeViewModel> SubWorkflowSchemeNodes;

        List<ConnectorViewModel> SchemeTransitions;

        NodesCanvasViewModel NodesCanvas;



        public CodeToSchemeComparer(StateMachine _codeWorkflow, NodesCanvasViewModel nodesCanvas)
        {
            NodesCanvas = nodesCanvas;
            ParseWorkflow(_codeWorkflow);
            ParseScheme(nodesCanvas.Nodes.Items.ToList());
        }

        private void ParseWorkflow(StateMachine _codeWorkflow)
        {
            SingleWorkflowStates = _codeWorkflow.Nodes.Where(x => x.GetType() != typeof(ParallelState) && x.GetType() != typeof(SequenceState) && x.GetType() != typeof(TransitionState) && x.GetType() != typeof(SubWorkflowState)).ToList();

            ParallelWorkflowStates = _codeWorkflow.Nodes.Where(x => x.GetType() == typeof(ParallelState)).Cast<ParallelState>().ToList();

            SequenceWorkflowStates = _codeWorkflow.Nodes.Where(x => x.GetType() == typeof(SequenceState)).Cast<SequenceState>().ToList();

            SubWorkflowStates = _codeWorkflow.Nodes.Where(x => x.GetType() == typeof(SubWorkflowState)).Cast<SubWorkflowState>().ToList();

            WorkflowStates = new List<State> { SingleWorkflowStates, ParallelWorkflowStates, SequenceWorkflowStates, SubWorkflowStates };

            TransitionDummys = _codeWorkflow.Nodes.Where(x => x.GetType() == typeof(TransitionState)).Cast<TransitionState>().ToList();

            WorkflowEdges = _codeWorkflow.Edges.ToList();

            foreach (var TransiNode in TransitionDummys)
            {
                var TransiNodeParent = TransiNode.Predecessors.First();

                var TransiNodeChildWithoutLoop = TransiNode.Successors.Where(x => x != TransiNode);
                foreach (var TransiNodeChild in TransiNodeChildWithoutLoop)
                {
                    WorkflowEdges.Add(new mitoSoft.Workflows.Transition(TransiNodeParent, TransiNodeChild, () => true));
                }
                WorkflowEdges.Remove(TransiNode.Edges);
            }
        }

        private void ParseScheme(List<BaseNodeViewModel> _schemeNodes)
        {
            SingleSchemeNodes = _schemeNodes.Where(x => x.GetType() == typeof(NodeViewModel)).Cast<NodeViewModel>().ToList();

            ParallelSchemeNodes = _schemeNodes.Where(x => x.GetType() == typeof(ParallelNodeViewModel)).Cast<ParallelNodeViewModel>().ToList();

            SequenceSchemeNodes = _schemeNodes.Where(x => x.GetType() == typeof(SequenceNodeViewModel)).Cast<SequenceNodeViewModel>().ToList();

            SubWorkflowSchemeNodes = _schemeNodes.Where(x => x.GetType() == typeof(SubWorkflowNodeViewModel)).Cast<SubWorkflowNodeViewModel>().ToList();

            SchemeNodes = new List<BaseNodeViewModel> { SingleSchemeNodes, ParallelSchemeNodes, SequenceSchemeNodes, SubWorkflowSchemeNodes };

            SchemeTransitions = SchemeNodes.SelectMany(x => x.Transitions.Items.Where(y => !string.IsNullOrEmpty(y.Name))).ToList();
        }

        public void Synchronize()
        {
            AddNewSingleNodes();

            AddNewParallelNodes();
            UpdateInternParaStates();

            AddNewSequenceNodes();
            UpdateInternSequenceStates();

            AddNewSubworkflowNodes();
            UpdateInternSubworkflowStates();

            AddEdges();

            DeleteAbsentNodes();
            DeleteAbsentEdges();
        }


        #region SingleWorkflowStates
        private void AddNewSingleNodes()
        {
            GetSingleNodeNamesToAdd().ForEach(x => NodesCanvas.Nodes.Add(new NodeViewModel(NodesCanvas, x)));
        }

        public List<string> GetSingleNodeNamesToAdd()
        {
            return SingleWorkflowStates.Select(x => x.Name).Except(SchemeNodes.Select(x => x.Name)).ToList();
        }
        #endregion

        #region ParallelWorkflowStates
        private void AddNewParallelNodes()
        {
            GetParaNodeNamesToAdd().ForEach(x => NodesCanvas.Nodes.Add(new ParallelNodeViewModel(NodesCanvas, x)));
        }

        private void UpdateInternParaStates()
        {
            foreach (var item in GetInternParallelStates())
            {
                var node = NodesCanvas.Nodes.Items.SingleOrDefault(x => x.Name == item.Key) as ParallelNodeViewModel;
                node.ParallelStates.Clear();
                node?.ParallelStates.Add(item.Value.Select(x => PathHelper.GetFilePathFromProjectDirectory(NodesCanvas.SchemePath, x)));
            }
        }

        public List<string> GetParaNodeNamesToAdd()
        {
            return ParallelWorkflowStates.Select(x => x.Name).Except(SchemeNodes.Select(x => x.Name)).ToList();
        }

        public Dictionary<string, List<string>> GetInternParallelStates()
        {
            var InternWorkflowStates = ParallelWorkflowStates.SelectMany(x => x.stateMachines.Select(y => (ParaStateName: x.Name, StateMachineName: y.GetType().Name))).ToList();

            var InternStatesToAdd = InternWorkflowStates
                .GroupBy(x => x.ParaStateName).Select(x => new { Name = x.Key, Values = x.Select(item => item.StateMachineName).ToList() }).ToDictionary(x => x.Name, x => x.Values);

            return InternStatesToAdd;
        }

        //public Dictionary<string, List<string>> InternParallelStatesToAdd()
        //{
        //    var InternWorkflowStates = ParallelWorkflowStates.SelectMany(x => x.stateMachines.Select(y => (x.Name, y.GetType().Name))).ToList();

        //    var InternSchemeStates = ParallelSchemeNodes.SelectMany(x => x.ParallelStates.Select(y => (x.Name, PathHelper.GetFileNameWithoutExtension(y)))).ToList();

        //    InternSchemeStates.ForEach(x => InternWorkflowStates.Remove(x));

        //    var InternStatesToAdd = InternWorkflowStates
        //        .GroupBy(x => x.Item1).Select(x => new { Name = x.Key, Values = x.Select(item => item.Item2).ToList() }).ToDictionary(x => x.Name, x => x.Values);

        //    return InternStatesToAdd;
        //}

        //public Dictionary<string, List<string>> InternParallelStatesToDelete()
        //{
        //    var InternWorkflowStates = ParallelWorkflowStates.SelectMany(x => x.stateMachines.Select(y => (x.Name, y.GetType().Name))).ToList();

        //    var InternSchemeStates = ParallelSchemeNodes.SelectMany(x => x.ParallelStates.Select(y => (x.Name, PathHelper.GetFileNameWithoutExtension(y)))).ToList();

        //    InternWorkflowStates.ForEach(x => InternSchemeStates.Remove(x));

        //    var InternStatesToRemove = InternSchemeStates
        //        .GroupBy(x => x.Item1).Select(x => new { Name = x.Key, Values = x.Select(item => item.Item2).ToList() }).ToDictionary(x => x.Name, x => x.Values);

        //    return InternStatesToRemove;
        //}

        #endregion

        #region SequenceWorkflowStates
        private void AddNewSequenceNodes()
        {
            GetSequenceNodeNamesToAdd().ForEach(x => NodesCanvas.Nodes.Add(new SequenceNodeViewModel(NodesCanvas, x)));
        }

        private void UpdateInternSequenceStates()
        {
            var SequenceNodesWF = SequenceWorkflowStates.Select(x => (SeqStateName: x.Name, SeqStates: x.internStates)).ToList();

            var SeqNodeList = new List<BaseNodeViewModel>();
            foreach (var item in SequenceNodesWF)
            {
                var SequenceState = NodesCanvas.Nodes.Items.SingleOrDefault(x => x.Name == item.SeqStateName) as SequenceNodeViewModel;

                foreach (var state in item.SeqStates)
                {
                    var seqNode = SequenceState.SequenceNodes.SingleOrDefault(x => x.Name == state.Name);
                    if (seqNode != null)
                    {
                        SeqNodeList.Add(seqNode);
                    }
                    else
                    {
                        SeqNodeList.Add(new NodeViewModel(NodesCanvas, state.Name));
                    }
                }
                SequenceState.SequenceNodes.Clear();
                SequenceState.SequenceNodes.Add(SeqNodeList);
            }
        }
        public List<string> GetSequenceNodeNamesToAdd()
        {
            return SequenceWorkflowStates.Select(x => x.Name).Except(SchemeNodes.Select(x => x.Name)).ToList();
        }

        //public Dictionary<string, List<string>> InternSequenceStatesToAdd()
        //{
        //    var SeqNodeDic = new Dictionary<string, List<string>>();
        //    foreach (var SeqNode in SequenceWorkflowStates)
        //    {
        //        SeqNodeDic.Add(SeqNode.Name, SeqNode.internStates.Select(x => x.Name).ToList());
        //    }
        //    return SeqNodeDic;

        //}

        //ToDo: Intern States Change Detection
        //adden von  interener Sequence State von StateMachines
        //löschen von hinterlegtem Pfad in SequenceNodeViewModel 
        #endregion

        #region SubWorkflowStates

        private void AddNewSubworkflowNodes()
        {
            SubWorkflowStateToAdd().ForEach(x => NodesCanvas.Nodes.Add(new SubWorkflowNodeViewModel(NodesCanvas, x)));
        }

        private void UpdateInternSubworkflowStates()
        {
            foreach (var state in SubWorkflowsToAdd())
            {
                var node = NodesCanvas.Nodes.Items.SingleOrDefault(x => x.Name == state.StateName) as SubWorkflowNodeViewModel;
                if (node != null)
                {
                    node.SubStateMachine = PathHelper.GetFilePathFromProjectDirectory(NodesCanvas.SchemePath, state.SubWorkflow);
                }
            }
        }

        public List<string> SubWorkflowStateToAdd()
        {
            return SubWorkflowStates.Select(x => x.Name).Except(SchemeNodes.Select(x => x.Name)).ToList();
        }

        public List<(string StateName, string SubWorkflow)> SubWorkflowsToAdd()
        {
            var InternSubWorkflows = SubWorkflowStates.Select(x => (x.Name, x.SubWorkflowStateMachine.GetType().Name)).ToList();

            var InternSchemeSubWorkflows = SubWorkflowSchemeNodes.Select(x => (x.Name, PathHelper.GetFileNameWithoutExtension(x.SubStateMachine))).ToList();

            var x = InternSubWorkflows.Except(InternSchemeSubWorkflows).ToList();

            return x;
        }

        #endregion

        #region Edges

        private void AddEdges()
        {
            foreach (var edge in EdgeNamesToAdd())
            {
                NodesCanvas.CommandAddConnectSourceTarget.ExecuteWithSubscribe(edge);
            }
        }

        public List<(string Source, string Target)> EdgeNamesToAdd()
        {
            return WorkflowEdges.Select(x => (x.Source.Name, x.Target.Name)).Except(SchemeTransitions.Select(x => (x.FromConnectorNodeName, x.ToConnectorNodeName))).ToList();
        }

        #endregion Edges

        #region Delete

        private void DeleteAbsentNodes()
        {
            NodesCanvas.CommandDeleteSelectedNodes.Execute(new List<BaseNodeViewModel>()
            {
                GetNodesToDelete()
            });
        }

        private void DeleteAbsentEdges()
        {
            NodesCanvas.CommandDeleteSelectedConnectors.Execute(new List<ConnectorViewModel>()
            {
                GetEdgesToDelete()
            });
        }

        public List<BaseNodeViewModel> GetNodesToDelete()
        {
            var NodeNames = SchemeNodes.Select(x => x.Name).Except(WorkflowStates.Select(x => x.Name)).ToList();

            return SchemeNodes.Where(x => NodeNames.Any(y => x.Name == y)).ToList();
        }

        public List<ConnectorViewModel> GetEdgesToDelete()
        {
            var EdgeNames = SchemeTransitions.Select(x => (x.FromConnectorNodeName, x.ToConnectorNodeName)).Except(WorkflowEdges.Select(x => (x.Source.Name, x.Target.Name)));

            return SchemeTransitions.Where(x => EdgeNames.Any(y => x.FromConnectorNodeName == y.Item1 && x.ToConnectorNodeName == y.Item2)).ToList();
        }

        #endregion

    }
}