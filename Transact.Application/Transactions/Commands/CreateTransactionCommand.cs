using MediatR;
using Transact.Application.Common.Interfaces;
using Transact.Domain.Entities;
using Transact.Domain.Enums;

namespace Transact.Application.Transactions.Commands;

public record CreateTransactionCommand(
    decimal Amount,
    string Currency,
    TransactionType Type,
    string SenderId,
    string ReceiverId) : IRequest<Transaction>;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Transaction>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionCommandHandler(ITransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Transaction> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.SenderId))
            throw new ArgumentException("SenderId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ReceiverId))
            throw new ArgumentException("ReceiverId is required.", nameof(request));

        if (string.Equals(request.SenderId, request.ReceiverId, StringComparison.Ordinal))
            throw new InvalidOperationException("Sender and receiver cannot be the same.");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "KES" : request.Currency,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            Status = "Pending"
        };

        await _repository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}
