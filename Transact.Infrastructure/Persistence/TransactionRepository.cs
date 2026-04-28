using Microsoft.EntityFrameworkCore;
using Transact.Application.Common.Interfaces;
using Transact.Domain.Entities;

namespace Transact.Infrastructure.Persistence;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    public TransactionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _db.Transactions.AddAsync(transaction, cancellationToken);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Transactions.AsNoTracking().ToListAsync(cancellationToken);
    }

    public void Update(Transaction transaction)
    {
        _db.Transactions.Update(transaction);
    }

    public void Remove(Transaction transaction)
    {
        _db.Transactions.Remove(transaction);
    }
}
