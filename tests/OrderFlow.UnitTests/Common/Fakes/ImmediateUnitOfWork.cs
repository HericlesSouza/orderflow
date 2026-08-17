using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;

namespace OrderFlow.UnitTests.Common.Fakes;

internal sealed class ImmediateUnitOfWork : IUnitOfWork
{
    public int ExecutionCount { get; private set; }

    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return await operation(cancellationToken);
    }
}
