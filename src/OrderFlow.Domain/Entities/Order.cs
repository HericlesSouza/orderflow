using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _orderItems = [];

    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }

    private Order() { }

    private Order(IEnumerable<OrderItem> orderItems)
    {
        var items = orderItems?.ToList() ?? [];
        Validate(items);

        Id = Guid.NewGuid();
        Status = OrderStatus.PendingPayment;
        _orderItems.AddRange(items);
        CalculateTotalAmount();
    }

    public static Order Create(IEnumerable<OrderItem> orderItems)
    {
        return new Order(orderItems);
    }

    public void Cancel()
    {
        EnsureCanBeCancelled();

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void EnsureCanBeCancelled()
    {
        if (Status == OrderStatus.Confirmed)
            throw new InvalidOperationException("Confirmed order cannot be cancelled.");

        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");

        if (Status == OrderStatus.PaymentRejected)
            throw new InvalidOperationException("Payment rejected order cannot be cancelled.");

        if (Status == OrderStatus.Dispatched)
            throw new InvalidOperationException("Dispatched order cannot be cancelled.");
    }

    public void ConfirmPayment()
    {
        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("Only pending payment orders can be confirmed.");

        Status = OrderStatus.Confirmed;
    }

    public void RejectPayment()
    {
        EnsurePaymentCanBeRejected();

        Status = OrderStatus.PaymentRejected;
    }

    public void EnsurePaymentCanBeRejected()
    {
        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("Only pending payment orders can be rejected.");
    }

    public void Dispatch()
    {
        EnsureCanBeDispatched();

        Status = OrderStatus.Dispatched;
        DispatchedAt = DateTime.UtcNow;
    }

    public void EnsureCanBeDispatched()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be dispatched.");
    }

    private static void Validate(IReadOnlyCollection<OrderItem> orderItems)
    {
        if (orderItems.Count == 0)
            throw new ArgumentException("Order must contain at least one item.");
    }

    private void CalculateTotalAmount()
    {
        TotalAmount = _orderItems.Sum(item => item.TotalPrice);
    }
}
