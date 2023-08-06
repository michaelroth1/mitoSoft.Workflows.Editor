namespace mitoSoft.Workflows.Advanced
{
    public class BuildableStateMachine : StateMachine
    {
        private readonly List<TemporalEdge> _edges = new();

        public new BuildableStateMachine AddNode(State state)
        {
            base.AddNode(state);

            return this;
        }

        public BuildableStateMachine AddEdge(string sourceName, string targetName)
        {
            return this.AddEdge(sourceName, targetName, () => true, "true");
        }

        public new BuildableStateMachine AddEdge(string sourceName, string targetName, mitoSoft.Workflows.Condition condition)
        {
            return this.AddEdge(sourceName, targetName, condition, "");
        }

        public BuildableStateMachine AddEdge(string sourceName, string targetName, mitoSoft.Workflows.Condition condition, string description)
        {
            _edges.Add(new TemporalEdge(sourceName, targetName, condition, description));
            return this;
        }

        public virtual BuildableStateMachine Build()
        {
            foreach (var edge in _edges)
            {
                base.AddEdge(edge.Source, edge.Target, edge.Condition);
                var e = base.GetEdge(edge.Source, edge.Target);
                e.Description = edge.Description;
            }

            return this;
        }

        public BuildableStateMachine AddState(string name)
        {
            this.AddNode(new mitoSoft.Workflows.State(name));

            return this;
        }

        public BuildableStateMachine AddState(string name, Action stateAction)
        {
            this.AddNode(new mitoSoft.Workflows.State(name, stateAction));

            return this;
        }

        public BuildableStateMachine AddState(string name, Action stateAction, Action stateExitAction)
        {
            this.AddNode(new mitoSoft.Workflows.State(name, stateAction, stateExitAction));

            return this;
        }

        public bool TempEdgeExists(string source, string target)
        {
            return _edges.Any(x => x.Source == source && x.Target == target);
        }
    }
}