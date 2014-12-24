﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bitcraft.StateMachine
{
    /// <summary>
    /// Represents a finite state machine.
    /// The state machine manages the states and the transtions.
    /// </summary>
    public class StateManager : IStateMachine
    {
        /// <summary>
        /// Gets the context of the current state machine.
        /// </summary>
        public object Context { get; private set; }

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public StateBase CurrentState { get; private set; }

        /// <summary>
        /// Gets the token of the currently active state. (shortcut to CurrentState.Token)
        /// </summary>
        public StateToken CurrentStateToken
        {
            get
            {
                return CurrentState != null ? CurrentState.Token : null;
            }
        }

        /// <summary>
        /// Gets the registered states.
        /// </summary>
        public ReadOnlyCollection<StateBase> States { get; private set; }

        private List<StateBase> states = new List<StateBase>();

        /// <summary>
        /// Initializes the StateManager instance without context.
        /// </summary>
        public StateManager()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes the StateManager instance with a context.
        /// </summary>
        /// <param name="context">The context to share among the states of the current state machine.</param>
        public StateManager(object context)
        {
            Context = context;
            States = new ReadOnlyCollection<StateBase>(states);
        }

        /// <summary>
        /// Sets the initial state of the current state machine, and resets its internal state.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public void SetInitialState(StateToken initialState)
        {
            SetInitialState(initialState, null);
        }

        /// <summary>
        /// Sets the initial state of the current state machine, and resets its internal state.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        /// <param name="data">The data to be provided to the initial state.</param>
        public void SetInitialState(StateToken initialState, object data)
        {
            if (initialState == null)
                throw new ArgumentNullException("initialState");

            CurrentState = null;
            PerformTransitionTo(initialState, data);
        }

        private void PerformTransitionTo(StateToken stateToken, object data)
        {
            var targetStateToken = stateToken;
            var targetData = data;

            while (true)
            {
                var transition = TransitionTo(targetStateToken, targetData);
                if (transition.TargetStateToken == null)
                    break;

                targetStateToken = transition.TargetStateToken;
                targetData = transition.TargetStateData;
            }
        }

        private TransitionInfo TransitionTo(StateToken stateToken, object data)
        {
            if (stateToken == null)
                throw new ArgumentNullException("stateToken");

            var state = states.FirstOrDefault(s => s.Token == stateToken);
            if (state == null)
                throw new UnknownStateException(CurrentStateToken, stateToken);

            if (CurrentState != null)
                CurrentState.OnExit();

            var oldState = CurrentState;
            CurrentState = state;

            var e = new StateEnterEventArgs(oldState != null ? oldState.Token : null, data);
            CurrentState.OnEnter(e);

            OnStateChanged(new StateChangedEventArgs(oldState, CurrentState));

            return e.Redirect;
        }

        /// <summary>
        /// Gets whether it is possible to call PerformAction without being returned false.
        /// <remarks>Typically, CanPerformAction returns false when a transition is being evaluated asynchronously and still underway.</remarks>
        /// </summary>
        public bool CanPerformAction
        {
            get
            {
                if (CurrentState == null)
                    return false;

                return CurrentState.IsHandlingAsync;
            }
        }

        /// <summary>
        /// Tells the state machine that an external action occured.
        /// This is the only way to make the state machine to possibly change its internal state.
        /// </summary>
        /// <param name="action">The action done that may change the state machine internal state.</param>
        /// <returns>Returns false if it is already processing an asynchronous action, true otherwise.</returns>
        public ActionResultType PerformAction(ActionToken action)
        {
            return PerformAction(action, null);
        }

        /// <summary>
        /// Tells the state machine that an external action occured.
        /// This is the only way to make the state machine to possibly change its internal state.
        /// </summary>
        /// <param name="action">The action done that may change the state machine internal state.</param>
        /// <param name="data">A custom data related to the action performed.</param>
        /// <returns>Returns false if it is already processing an asynchronous action, true otherwise.</returns>
        public ActionResultType PerformAction(ActionToken action, object data)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (CurrentState == null)
                throw new InvalidOperationException("State machine not yet initialized or has reached its final state.");

            return CurrentState.Handle(action, data, transitionInfo =>
            {
                if (transitionInfo == null || transitionInfo.TargetStateToken == null)
                {
                    CurrentState = null;
                    OnCompleted();
                    return;
                }

                if (CurrentState.Token != transitionInfo.TargetStateToken)
                    PerformTransitionTo(transitionInfo.TargetStateToken, transitionInfo.TargetStateData);
            });
        }

        /// <summary>
        /// Raised when the state machine transitions from a state to another.
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>
        /// Called when the state machine transitions from a state to another.
        /// </summary>
        /// <param name="e">Custom event arguments.</param>
        protected virtual void OnStateChanged(StateChangedEventArgs e)
        {
            var handler = StateChanged;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Raised when the state machine has reached its final state.
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        /// Called when the state machine has reached its final state.
        /// </summary>
        protected virtual void OnCompleted()
        {
            var handler = Completed;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Registers a state, given a new context for this state and its sub states.
        /// </summary>
        /// <param name="state">A state to be known by the state machine.</param>
        public void RegisterState(StateBase state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            if (states.Contains(state))
                throw new InvalidOperationException(string.Format("State '{0}' already registered.", state.Token));

            states.Add(state);
            state.Initialize(this);
        }
    }
}