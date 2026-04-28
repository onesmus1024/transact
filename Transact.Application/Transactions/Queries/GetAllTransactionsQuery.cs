using MediatR;
using Transact.Application.Common.Interfaces;
using Transact.Domain.Entities;

namespace Transact.Application.Transactions.Queries;

public record GetAllTransactionsQuery : IRequest<IReadOnlyList<Transaction>>;

public class GetAllTransactionsQueryHandler : IRequestHandler<GetAllTransactionsQuery, IReadOnlyList<Transaction>>
{
    private readonly ITransactionRepository _repository;

    public GetAllTransactionsQueryHandler(ITransactionRepository repository) => _repository = repository;

    public Task<IReadOnlyList<Transaction>> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);
}
