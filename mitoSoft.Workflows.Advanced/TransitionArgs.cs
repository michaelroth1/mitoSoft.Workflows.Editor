namespace mitoSoft.Workflows.Advanced
{
    public class TransitionArgs
    {
        public Dictionary<(string Name, string Target), Condition> Conditions { get; } = new();

        public Action? Action { get; set; }

        public string Name { get; set; } = String.Empty;

        public void AddCondition(Condition condition, string target)
        {
            this.Conditions.Add((this.Name, target), condition);
        }
    }
}