namespace SmartTrader.Infrastructure.Persistence.Entities;

public class User
{
    public Guid Id { get; set; }
    public long ChatId { get; set; }
    public string Role { get; set; } = "user";
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = null!;
    public string Strategy { get; set; } = null!;
    public string? Params { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public User? User { get; set; }
}

public class Signal
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = null!;
    public string Strategy { get; set; } = null!;
    public DateTimeOffset CandleTs { get; set; }
    public string Side { get; set; } = null!; // BUY/SELL/EXIT
    public decimal Price { get; set; }
    public decimal Confidence { get; set; }
    public string? Reason { get; set; }
    public string? Snapshot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Outbox
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string Status { get; set; } = "pending"; // pending/sent/failed
}


