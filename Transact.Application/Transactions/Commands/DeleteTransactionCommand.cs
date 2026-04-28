using MediatR;
using Transact.Application.Common.Interfaces;

namespace Transact.Application.Transactions.Commands;

public record DeleteTransactionCommand(Guid Id) : IRequest<bool>;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, bool>
{
    private readonly ITransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTransactionCommandHandler(ITransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null) return false;

        _repository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
