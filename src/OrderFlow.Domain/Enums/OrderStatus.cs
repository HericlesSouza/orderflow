namespace OrderFlow.Domain.Enums;

public enum OrderStatus
{
    PendingPayment,
    Confirmed,
    Cancelled,
    PaymentRejected,
    Dispatched
}
