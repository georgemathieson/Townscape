using System;
using System.Collections.Generic;

namespace Townscape.State
{
    /// <summary>
    /// Redux-style store: the single source of truth for user-facing settings such as the
    /// chosen time of day or the storm sliders.
    /// </summary>
    /// <remarks>
    /// Per-frame simulation values (the blended hour, a lightning flash) deliberately live in the
    /// systems that animate them, not here. The store only changes when someone asks for a change.
    /// Actions dispatched while another action is being processed (for example by a subscriber)
    /// are queued and processed in order once the current one completes.
    /// </remarks>
    public sealed class Store<TState> where TState : class
    {
        private readonly Reducer<TState> _reducer;
        private readonly Queue<IAction> _pending = new Queue<IAction>();
        private readonly List<ISubscription> _subscriptions = new List<ISubscription>();
        private bool _dispatching;

        public Store(Reducer<TState> reducer, TState initialState)
        {
            _reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
            State = initialState ?? throw new ArgumentNullException(nameof(initialState));
        }

        public TState State { get; private set; }

        /// <summary>Raised after every processed action with the resulting state. Useful for logging.</summary>
        public event Action<IAction, TState> ActionProcessed;

        public void Dispatch(IAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _pending.Enqueue(action);
            if (_dispatching)
            {
                return;
            }

            _dispatching = true;
            try
            {
                while (_pending.Count > 0)
                {
                    var next = _pending.Dequeue();
                    var previous = State;
                    State = _reducer(previous, next)
                        ?? throw new InvalidOperationException($"Reducer returned null for {next.GetType().Name}.");

                    ActionProcessed?.Invoke(next, State);

                    if (!ReferenceEquals(previous, State))
                    {
                        Notify();
                    }
                }
            }
            catch
            {
                _pending.Clear();
                throw;
            }
            finally
            {
                _dispatching = false;
            }
        }

        /// <summary>
        /// Calls <paramref name="onChanged"/> with the selected slice immediately, and again whenever
        /// the slice changes (compared with <paramref name="comparer"/>, or value equality by default).
        /// Dispose the returned handle to unsubscribe.
        /// </summary>
        public IDisposable Subscribe<TSlice>(
            Func<TState, TSlice> selector,
            Action<TSlice> onChanged,
            IEqualityComparer<TSlice> comparer = null)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (onChanged == null)
            {
                throw new ArgumentNullException(nameof(onChanged));
            }

            var subscription = new Subscription<TSlice>(this, selector, onChanged, comparer ?? EqualityComparer<TSlice>.Default);
            _subscriptions.Add(subscription);
            subscription.Prime(State);
            return subscription;
        }

        private void Notify()
        {
            // Snapshot so subscribers can subscribe or unsubscribe while being notified.
            var snapshot = _subscriptions.ToArray();
            foreach (var subscription in snapshot)
            {
                subscription.OnStateChanged(State);
            }
        }

        private void Remove(ISubscription subscription)
        {
            _subscriptions.Remove(subscription);
        }

        private interface ISubscription : IDisposable
        {
            void OnStateChanged(TState state);
        }

        private sealed class Subscription<TSlice> : ISubscription
        {
            private readonly Store<TState> _store;
            private readonly Func<TState, TSlice> _selector;
            private readonly Action<TSlice> _onChanged;
            private readonly IEqualityComparer<TSlice> _comparer;
            private TSlice _last;
            private bool _disposed;

            public Subscription(Store<TState> store, Func<TState, TSlice> selector, Action<TSlice> onChanged, IEqualityComparer<TSlice> comparer)
            {
                _store = store;
                _selector = selector;
                _onChanged = onChanged;
                _comparer = comparer;
            }

            public void Prime(TState state)
            {
                _last = _selector(state);
                _onChanged(_last);
            }

            public void OnStateChanged(TState state)
            {
                if (_disposed)
                {
                    return;
                }

                var slice = _selector(state);
                if (_comparer.Equals(_last, slice))
                {
                    return;
                }

                _last = slice;
                _onChanged(slice);
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _store.Remove(this);
            }
        }
    }
}
