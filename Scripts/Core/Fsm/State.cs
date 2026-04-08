namespace EchoSpace.Core.Fsm;

public abstract class State<TContext>
{
    protected State(TContext context, StateMachine<TContext> stateMachine)
    {
        Context = context;
        StateMachine = stateMachine;
    }

    protected TContext Context { get; }

    protected StateMachine<TContext> StateMachine { get; }

    public virtual string Name => GetType().Name;

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Update(double delta)
    {
    }

    public virtual void PhysicsUpdate(double delta)
    {
    }
}
