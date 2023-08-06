namespace mitoSoft.Workflows.Advanced.Tests
{
    [TestClass]
    public class UnitTests
    {
        /// <summary>
        /// Allows the build of a state machine by a better 
        /// nomenclature (state->edge->state...)
        /// </summary>
        [TestMethod]
        [TestCategory("BuildableStateMachine")]
        public void BuildableStateMachineStandardTest()
        {
            Logger.Clear();

            new BuildableStateMachine()
                .AddNode(new State("Start"))
                .AddEdge("Start", "Middle")
                .AddNode(new State("Middle"))
                .AddEdge("Middle", "End")
                .AddNode(new State("End"))
                .Build()
                .Invoke();

            Assert.AreEqual("Start->Middle->End", Logger.ShowTrace());
        }

        [TestMethod]
        [TestCategory("BuildableStateMachine")]
        public void BuildableStateMachineEdgeFirstTest()
        {
            Logger.Clear();

            new BuildableStateMachine()
                .AddEdge("Start", "Middle")
                .AddEdge("Middle", "End")
                .AddNode(new State("Start"))
                .AddNode(new State("Middle"))
                .AddNode(new State("End"))
                .Build()
                .Invoke();

            Assert.AreEqual("Start->Middle->End", Logger.ShowTrace());
        }

        [TestMethod]
        [TestCategory("TransitionalStateMachine")]
        public void TransitionalStateMachineStandardTest()
        {
            Logger.Clear();

            new TransitionalStateMachine()
                .AddNode(new State("Start"))
                .AddEdge("Start", "Middle")
                .AddNode(new State("Middle"))
                .AddEdge("Middle", "End")
                .AddNode(new State("End"))
                .Build()
                .Invoke();

            Assert.AreEqual("Start->Middle->End", Logger.ShowTrace());
        }

        [TestMethod]
        [TestCategory("TransitionalStateMachine")]
        public void TransitionalStateMachineComplexTest()
        {
            Logger.Clear();

            new TransitionalStateMachine()
                .AddNode(new State("Start"))
                .AddEdge("Start", "T1")
                .AddTransition("T1", (sender, transition) =>
                    {
                        int i = 0;
                        transition.Action = () =>
                        {
                            Logger.Log($"Loop {i + 1}");
                            i++;
                        };
                        transition.AddCondition(() => { return i < 5; }, "T1");
                        transition.AddCondition(() => { return i >= 5; }, "Next");
                    })
                .AddNode(new State("Next"))
                .AddEdge("Next", "End")
                .AddNode(new State("End"))

                .Build()
                .Invoke();

            Assert.AreEqual("Start->Loop 1->Loop 2->Loop 3->Loop 4->Loop 5->Next->End", Logger.ShowTrace());
        }
    }
}