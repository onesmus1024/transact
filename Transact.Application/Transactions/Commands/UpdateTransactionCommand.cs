using MediatR;
using Transact.Application.Common.Interfaces;
using Transact.Domain.Entities;
using Transact.Domain.Enums;

namespace Transact.Application.Transactions.Commands;

public record UpdateTransactionCommand(
    Guid Id,
    decimal Amount,
    string Currency,
    TransactionType Type,
    string SenderId,
    string ReceiverId,
    string Status) : IRequest<Transaction?>;

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, Transaction?>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTransactionCommandHandler(ITransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Transaction?> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null) return null;

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.SenderId))
            throw new ArgumentException("SenderId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ReceiverId))
            throw new ArgumentException("ReceiverId is required.", nameof(request));

        if (string.Equals(request.SenderId, request.ReceiverId, StringComparison.Ordinal))
            throw new InvalidOperationException("Sender and receiver cannot be the same.");

        existing.Amount = request.Amount;
        existing.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "KES" : request.Currency;
        existing.Type = request.Type;
        existing.SenderId = request.SenderId;
        existing.ReceiverId = request.ReceiverId;
        existing.Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status;

        _repository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existing;
    }
}
