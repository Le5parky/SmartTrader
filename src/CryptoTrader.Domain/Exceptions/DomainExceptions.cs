namespace CryptoTrader.Domain.Exceptions;

public class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message) { }
}

public class OrderCreationException : Exception
{
    public OrderCreationException(string message) : base(message) { }
}

public class OrderCancelledException : Exception
{
    public OrderCancelledException(string message) : base(message) { }
}

public class ParserNotFoundException : Exception
{
    public ParserNotFoundException(string message) : base(message) { }
}
