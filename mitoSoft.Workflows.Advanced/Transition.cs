namespace mitoSoft.Workflows.Advanced
{
    public delegate void TransitionHandler(StateMachine sender, TransitionArgs args);

    public delegate void ScopeHandler(StateMachine sender);

    public class Transition
    {
        public string Name { get; set; }

        public TransitionHandler TransitionHandler { get; set; }

        public Transition(string name, TransitionHandler trans)
        {
            this.Name = name;
            this.TransitionHandler = trans;
        }
    }
}