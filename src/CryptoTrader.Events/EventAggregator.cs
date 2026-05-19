namespace CryptoTrader.Events;

public interface IEvent
{
}

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
}

public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<object>> _subscriptions = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var type = typeof(TEvent);
        if (_subscriptions.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers.Cast<Action<TEvent>>().ToList())
            {
                handler(@event);
            }
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        var type = typeof(TEvent);
        if (!_subscriptions.TryGetValue(type, out var handlers))
        {
            handlers = new List<object>();
            _subscriptions[type] = handlers;
        }

        handlers.Add(handler);
        return new Subscription(() => handlers.Remove(handler));
    }

    private class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}
