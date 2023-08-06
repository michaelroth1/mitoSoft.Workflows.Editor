using System.Collections.Generic;
using System.Threading;
using mitoSoft.Workflows;

namespace mitoSoft.Workflows.Editor.States
{
    public class ParallelState : State
    {
        public List<StateMachine> stateMachines;
        public ParallelState(string name) : base(name)
        {
            stateMachines = new List<StateMachine>();
        }

        public ParallelState AddToParallel(StateMachine stateMachine)
        {
            stateMachines.Add(stateMachine);

            return this;
        }

        public override void StateFunction()
        {
            List<Thread> tasks = new List<Thread>();

            foreach (var machine in stateMachines)
            {
                tasks.Add(new Thread(machine.Invoke));
            }

            tasks.ForEach(x => x.Start());

            tasks.ForEach(x => x.Join());
        }
    }
}
