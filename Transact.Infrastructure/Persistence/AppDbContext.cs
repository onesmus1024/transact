using Microsoft.EntityFrameworkCore;
using Transact.Application.Common.Interfaces;
using Transact.Domain.Entities;

namespace Transact.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(b =>
        {
            b.ToTable("transactions");
            b.HasKey(t => t.Id);
            b.Property(t => t.Amount).HasColumnType("numeric(18,2)");
            b.Property(t => t.Currency).HasMaxLength(3).IsRequired();
            b.Property(t => t.Status).HasMaxLength(32).IsRequired();
            b.Property(t => t.SenderId).HasMaxLength(64).IsRequired();
            b.Property(t => t.ReceiverId).HasMaxLength(64).IsRequired();
            b.Property(t => t.Type).HasConversion<string>().HasMaxLength(32);
            b.HasIndex(t => t.SenderId);
            b.HasIndex(t => t.ReceiverId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
