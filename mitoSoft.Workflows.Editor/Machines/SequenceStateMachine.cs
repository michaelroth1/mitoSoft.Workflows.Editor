using System;
using mitoSoft.Workflows;
using mitoSoft.Workflows.Advanced;
using mitoSoft.Workflows.Editor.States;

namespace mitoSoft.Workflows.Editor.Machines
{
    public class SequenceStateMachine : ParallelStateMachine
    {
        public SequenceStateMachine AddSequenceNode(SequenceState node)
        {
            //if (this.Start == null)           {
            //    this.Start = node;
            this.Start ??= node;

            base.AddNode(node);

            return this;
        }

        public new SequenceStateMachine AddSingleNode(string nodeName, string nextNode, Action action)
        {
            return (SequenceStateMachine)base.AddSingleNode(nodeName, nextNode, action);
        }

        public new SequenceStateMachine AddSingleNode(State state, string nextNodeName)
        {
            return (SequenceStateMachine)base.AddSingleNode(state, nextNodeName);
        }

        public new SequenceStateMachine AddParallelNode(ParallelState node)
        {
            return (SequenceStateMachine)base.AddParallelNode(node);
        }

        public new SequenceStateMachine AddNode(State state)
        {
            return (SequenceStateMachine)base.AddNode(state);
        }

        public new SequenceStateMachine AddNode(string nodeName, Action action, Action? exitAction = null)
        {
            return (SequenceStateMachine)base.AddNode(nodeName, action, exitAction);
        }

        public new SequenceStateMachine AddTransition(string name, TransitionHandler transitionAction)
        {
            return (SequenceStateMachine)base.AddTransition(name, transitionAction);
        }

        public new SequenceStateMachine AddEdge(string sourceName, string targetName)
        {
            return (SequenceStateMachine)base.AddEdge(sourceName, targetName, () => true, "true");
        }

        public new SequenceStateMachine AddEdge(string sourceName, string targetName, Condition condition)
        {
            return (SequenceStateMachine)base.AddEdge(sourceName, targetName, condition, "");
        }

        public new SequenceStateMachine AddEdge(string sourceName, string targetName, Condition condition, string description)
        {
            return (SequenceStateMachine)base.AddEdge(sourceName, targetName, condition, description);
        }

        public override SequenceStateMachine Build()
        {
            return (SequenceStateMachine)base.Build();
        }
    }
}
