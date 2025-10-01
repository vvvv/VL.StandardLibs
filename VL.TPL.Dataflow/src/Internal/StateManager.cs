namespace VL.TPL.Dataflow.Internal;

sealed class StateManager<TState> : IDisposable
{
    private readonly Stack<WorkerState> _states = new();
    private int _nextWorkerId = -1;

    public Lease LeaseState(Func<int, TState> create)
    {
        WorkerState? state = null;

        lock (_states)
        {
            if (_states.Count > 0)
                state = _states.Pop();
        }

        if (state is null)
        {
            var id = Interlocked.Increment(ref _nextWorkerId);
            state = new WorkerState(id, create(id));
        }

        return new Lease(this, state);
    }

    private void ReleaseState(WorkerState state)
    {
        lock (_states)
        {
            _states.Push(state);
        }
    }

    public void Dispose()
    {
        lock (_states)
        {
            while (_states.Count > 0)
                _states.Pop().Dispose();
        }
    }

    internal sealed class WorkerState(int id, TState state) : IDisposable
    {
        public TState State = state;

        public int Id => id;

        public void Dispose()
        {
            if (State is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public readonly struct Lease : IDisposable
    {
        private readonly StateManager<TState> stateManager;
        private readonly WorkerState state;

        public Lease(StateManager<TState> stateManager, WorkerState state)
        {
            this.stateManager = stateManager;
            this.state = state;
        }

        public ref TState State => ref state.State;

        public void Dispose() => stateManager.ReleaseState(state);
    }
}

static class StateManagerExtensions
{
    public static StateManager<object>.Lease LeaseState(this StateManager<object> manager, CreateHandler create)
    {
        return manager.LeaseState(id =>
        {
            create(id, out var s);
            return s;
        });
    }
}