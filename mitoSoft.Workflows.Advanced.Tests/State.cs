using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace mitoSoft.Workflows.Advanced.Tests
{
    internal class State : mitoSoft.Workflows.State
    {
        public State(string name) : base(name)
        {
        }

        public override void StateFunction()
        {
            Logger.Log(this.Name);

            base.StateFunction();
        }
    }
}