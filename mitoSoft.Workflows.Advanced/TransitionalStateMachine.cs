namespace mitoSoft.Workflows.Advanced
{
    public class TransitionalStateMachine : BuildableStateMachine
    {
        public new TransitionalStateMachine AddNode(State state)
        {
            base.AddNode(state);

            return this;
        }

        public new TransitionalStateMachine AddEdge(string sourceName, string targetName)
        {
            return this.AddEdge(sourceName, targetName, () => true, "true");
        }

        public new TransitionalStateMachine AddEdge(string sourceName, string targetName, Condition condition)
        {
            return this.AddEdge(sourceName, targetName, condition, "");
        }

        public new TransitionalStateMachine AddEdge(string sourceName, string targetName, Condition condition, string description)
        {
            return (TransitionalStateMachine)base.AddEdge(sourceName, targetName, condition, description);
        }

        public override TransitionalStateMachine Build()
        {
            return (TransitionalStateMachine)base.Build();
        }

        public TransitionalStateMachine AddTransition(string name, TransitionHandler transitionHandler)
        {
            this.AddTransition(new Transition(name, transitionHandler));

            return this;
        }

        protected void AddTransition(Transition transition)
        {
            var args = new TransitionArgs()
            {
                Name = transition.Name,
            };

            transition.TransitionHandler.Invoke(this, args);

            this.AddNode(new State($"{transition.Name}", args.Action));

            foreach (var condition in args.Conditions)
            {
                this.AddEdge($"{transition.Name}", condition.Key.Target, condition.Value, condition.Key.Name);
            }

            this.TryAddEdge($"{transition.Name}", $"{transition.Name}");
        }

        /// <summary>
        /// Try to avoid deadlocks
        /// Add a self-circle loop to activate the transition activation again.
        /// </summary>
        protected void TryAddEdge(string from, string to)
        {
            try
            {
                if (!TempEdgeExists(from, to))
                    this.AddEdge(from, to);
            }
            catch { }
        }
    }
}