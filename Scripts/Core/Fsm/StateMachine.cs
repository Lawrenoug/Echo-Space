using System;
using System.Collections.Generic;

namespace EchoSpace.Core.Fsm;

public sealed class StateMachine<TContext>
{
    private readonly Dictionary<Type, State<TContext>> _states = new();

    public StateMachine(TContext context)
    {
        Context = context;
    }

    public TContext Context { get; }

    public State<TContext>? CurrentState { get; private set; }

    public void Register(State<TContext> state)
    {
        _states[state.GetType()] = state;
    }

    public bool TryGet<TState>(out TState? state) where TState : State<TContext>
    {
        if (_states.TryGetValue(typeof(TState), out var result))
        {
            state = (TState)result;
            return true;
        }

        state = null;
        return false;
    }

    public void ChangeState<TState>() where TState : State<TContext>
    {
        if (!_states.TryGetValue(typeof(TState), out var nextState))
        {
            throw new InvalidOperationException($"State {typeof(TState).Name} has not been registered.");
        }

        if (ReferenceEquals(CurrentState, nextState))
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void Update(double delta)
    {
        CurrentState?.Update(delta);
    }

    public void PhysicsUpdate(double delta)
    {
        CurrentState?.PhysicsUpdate(delta);
    }
}
