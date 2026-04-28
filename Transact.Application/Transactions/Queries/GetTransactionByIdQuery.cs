using MediatR;
using Transact.Application.Common.Interfaces;
using Transact.Domain.Entities;

namespace Transact.Application.Transactions.Queries;

public record GetTransactionByIdQuery(Guid Id) : IRequest<Transaction?>;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Transaction?>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdQueryHandler(ITransactionRepository repository) => _repository = repository;

    public Task<Transaction?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.Id, cancellationToken);
}
