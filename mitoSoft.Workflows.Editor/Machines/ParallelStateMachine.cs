using System;
using mitoSoft.Workflows;
using mitoSoft.Workflows.Advanced;
using mitoSoft.Workflows.Editor.States;

namespace mitoSoft.Workflows.Editor.Machines
{
    public class ParallelStateMachine : TransitionalStateMachine
    {
        public ParallelStateMachine AddParallelNode(ParallelState node)
        {
            if (this.Start == null)
            {
                this.Start = node;
            }

            base.AddNode(node);

            return this;
        }

        public ParallelStateMachine AddSingleNode(string nodeName, string nextNode, Action action)
        {
            this.AddState(nodeName, action);

            this.AddEdge(nodeName, nextNode);

            return this;
        }

        public ParallelStateMachine AddSingleNode(State state, string nextNodeName)
        {
            this.AddNode(state);

            this.AddEdge(state.Name, nextNodeName);

            return this;
        }


        public new ParallelStateMachine AddNode(State state)
        {
            base.AddNode(state);

            return this;
        }

        public ParallelStateMachine AddNode(string nodeName, Action action, Action? exitAction = null)
        {
            this.AddState(nodeName, action);

            return this;
        }

        public new ParallelStateMachine AddTransition(string name, TransitionHandler transitionAction)
        {
            return (ParallelStateMachine)base.AddTransition(name, transitionAction);
        }

        public new ParallelStateMachine AddEdge(string sourceName, string targetName)
        {
            return (ParallelStateMachine)base.AddEdge(sourceName, targetName, () => true, "true");
        }

        public new ParallelStateMachine AddEdge(string sourceName, string targetName, Condition condition)
        {
            return (ParallelStateMachine)base.AddEdge(sourceName, targetName, condition, "");
        }

        public new ParallelStateMachine AddEdge(string sourceName, string targetName, Condition condition, string description)
        {
            return (ParallelStateMachine)base.AddEdge(sourceName, targetName, condition, description);
        }

        public override ParallelStateMachine Build()
        {
            return (ParallelStateMachine)base.Build();
        }
    }
}
