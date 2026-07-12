using OrderFlow.Application.Common.Results;

namespace OrderFlow.Application.Common.Transactions;

public interface IUnitOfWork
{
    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken);
}
