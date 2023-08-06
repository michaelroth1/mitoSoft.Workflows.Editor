using System;
using System.Collections.Generic;
using System.Linq;
using mitoSoft.Workflows;
using mitoSoft.Workflows.Editor.Machines;

namespace mitoSoft.Workflows.Editor.States
{

    public class SequenceState : State
    {

        public SequenceStateMachine stateMachine;
        public List<State> internStates = new List<State>();

        public SequenceState(string name) : base(name)
        {
            stateMachine = new SequenceStateMachine();
        }

        public SequenceState AddToSequence(State state)
        {
            internStates.Add(state);
            return this;
        }

        public SequenceState AddToSequence(string name, Action action = null)
        {
            internStates.Add(new State(name, action ?? (() => { })));
            return this;
        }

        public override void StateFunction()
        {
            BuildSequence();
            stateMachine.Build();
            stateMachine.Invoke();
        }

        private void BuildSequence()
        {
            State tempNode = null;
            foreach (var node in internStates)
            {
                if (tempNode != null)
                {
                    stateMachine.AddSingleNode(tempNode, node.Name);
                }
                tempNode = node;
            }
            stateMachine.AddNode(internStates.Last());
        }
    }
}