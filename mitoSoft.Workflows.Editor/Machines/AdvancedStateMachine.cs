using System;
using mitoSoft.Workflows;
using mitoSoft.Workflows.Advanced;
using mitoSoft.Workflows.Editor.States;

namespace mitoSoft.Workflows.Editor.Machines
{
    public class AdvancedStateMachine : SequenceStateMachine
    {

        public AdvancedStateMachine AddSubWorkflow(string nodeName, StateMachine subStateMachine, string nextNodeName = null)
        {
            var node = new SubWorkflowState(nodeName, subStateMachine);

            if (this.Start == null)
            {
                this.Start = node;
            }

            base.AddNode(node);

            if (!string.IsNullOrEmpty(nextNodeName))
            {
                base.AddEdge(node.Name, nextNodeName);
            }

            return this;
        }

        public new AdvancedStateMachine AddSingleNode(string nodeName, string nextNode, Action action)
        {
            return (AdvancedStateMachine)base.AddSingleNode(nodeName, nextNode, action);
        }

        public AdvancedStateMachine AddSingleNode(string nodeName, Action action)
        {
            return (AdvancedStateMachine)base.AddNode(nodeName, action);
        }

        public new AdvancedStateMachine AddSingleNode(State state, string nextNodeName)
        {
            return (AdvancedStateMachine)base.AddSingleNode(state, nextNodeName);
        }

        public AdvancedStateMachine AddParallelNode(ParallelState node, string nextNodeName = null)
        {
            base.AddParallelNode(node);

            if (!string.IsNullOrEmpty(nextNodeName))
            {
                base.AddEdge(node.Name, nextNodeName);
            }

            return this;
        }

        public AdvancedStateMachine AddSequenceNode(SequenceState node, string nextNodeName = null)
        {
            base.AddSequenceNode(node);

            if (!string.IsNullOrEmpty(nextNodeName))
            {
                base.AddEdge(node.Name, nextNodeName);
            }

            return this;
        }

        public new AdvancedStateMachine AddNode(State state)
        {
            return (AdvancedStateMachine)base.AddNode(state);
        }

        public new AdvancedStateMachine AddNode(string nodeName, Action action, Action? exitAction = null)
        {
            return (AdvancedStateMachine)base.AddNode(nodeName, action, exitAction);
        }

        public AdvancedStateMachine AddConditions(string name, TransitionHandler transitionAction)
        {
            return (AdvancedStateMachine)base.AddTransition(name, transitionAction);
        }

        public new AdvancedStateMachine AddEdge(string sourceName, string targetName)
        {
            return (AdvancedStateMachine)base.AddEdge(sourceName, targetName, () => true, "true");
        }

        public new AdvancedStateMachine AddEdge(string sourceName, string targetName, Condition condition)
        {
            return (AdvancedStateMachine)base.AddEdge(sourceName, targetName, condition, "");
        }

        public new AdvancedStateMachine AddEdge(string sourceName, string targetName, Condition condition, string description)
        {
            return (AdvancedStateMachine)base.AddEdge(sourceName, targetName, condition, description);
        }

        public override AdvancedStateMachine Build()
        {
            return (AdvancedStateMachine)base.Build();
        }
    }
}
