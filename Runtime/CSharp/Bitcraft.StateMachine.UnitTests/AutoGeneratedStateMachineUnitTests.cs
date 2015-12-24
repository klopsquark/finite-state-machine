﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bitcraft.StateMachine.UnitTests.AutoGenerated;

namespace Bitcraft.StateMachine.UnitTests
{
    [TestClass]
    public class AutoGeneratedStateMachineUnitTests
    {
        [TestMethod]
        public void TestMethod01()
        {
            var context = new StateMachineTestContext();

            var sm = new StateManager(context);

            sm.StateChanged += (ss, ee) => System.Diagnostics.Debug.WriteLine(string.Format("State changed from '{0}' to '{1}'", ee.OldState, ee.NewState));
            sm.Completed += (ss, ee) => ((StateMachineTestContext)sm.Context).TestStatus++;

            sm.RegisterState(new TestAutoBeginState());
            sm.RegisterState(new TestAutoUpdateState());
            sm.RegisterState(new TestAutoEndState());

            sm.SetInitialState(TestAutoStateTokens.Begin, null);

            sm.PerformAction(TestAutoActionTokens.InitDone, 51);
            sm.PerformAction(TestAutoActionTokens.Update);
            sm.PerformAction(TestAutoActionTokens.Update);
            sm.PerformAction(TestAutoActionTokens.Update);
            sm.PerformAction(TestAutoActionTokens.Update);
            sm.PerformAction(TestAutoActionTokens.Update);
            sm.PerformAction(TestAutoActionTokens.Terminate);

            Assert.AreEqual(context.TestStatus, 5);

            sm.PerformAction(TestAutoActionTokens.Finalize);

            Assert.AreEqual(context.TestStatus, 6);
        }
    }
}
