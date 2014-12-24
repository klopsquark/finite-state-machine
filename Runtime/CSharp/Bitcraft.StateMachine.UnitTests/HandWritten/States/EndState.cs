﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bitcraft.StateMachine.UnitTests.HandWritten.States
{
    public class EndState : StateBase
    {
        public EndState()
            : base(HandWrittenStateTokens.EndStateToken)
        {
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            RegisterActionHandler(HandWrittenActionTokens.FinalizeAction, (d, cb) => cb(null as TransitionInfo));
        }

        protected override void OnEnter(StateEnterEventArgs e)
        {
            base.OnEnter(e);

            var context = (StateMachineTestContext)Context;

            Assert.AreEqual(context.TestStatus, 4);
            context.TestStatus++;
        }

        protected override void OnExit()
        {
            base.OnExit();

            var context = (StateMachineTestContext)Context;

            Assert.AreEqual(context.TestStatus, 5);
            context.TestStatus++;
        }
    }
}