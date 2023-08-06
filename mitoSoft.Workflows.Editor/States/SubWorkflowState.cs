using mitoSoft.Workflows;

namespace mitoSoft.Workflows.Editor.States
{
    public class SubWorkflowState : State
    {
        public StateMachine SubWorkflowStateMachine;

        public SubWorkflowState(string name, StateMachine _subSequenceStateMachine) : base(name)
        {
            SubWorkflowStateMachine = _subSequenceStateMachine;
        }

        public override void StateFunction()
        {
            SubWorkflowStateMachine.Invoke();
        }
    }
}