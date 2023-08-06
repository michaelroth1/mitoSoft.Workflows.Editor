namespace mitoSoft.Workflows.Advanced
{
    internal class TemporalEdge
    {
        public string Source { get; set; }

        public string Target { get; set; }

        public Condition Condition { get; set; }

        public string Description { get; set; }

        public TemporalEdge(string source, string target, mitoSoft.Workflows.Condition condition, string description)
        {
            this.Source = source;
            this.Target = target;
            this.Condition = condition;
            this.Description = description;
        }
    }
}