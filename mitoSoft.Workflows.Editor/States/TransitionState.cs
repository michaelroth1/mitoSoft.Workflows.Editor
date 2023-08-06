using System;
using mitoSoft.Workflows;

namespace mitoSoft.StateMachine.AdvancedStateMachines
{
    public class TransitionState : State
    {
        public TransitionState(string name, Action action) : base(name, action) { }
    }
}