using Transact.Domain.Enums;
namespace Transact.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "KES"; 
        public TransactionType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string SenderId { get; set; }
        public required string ReceiverId { get; set; }
        public string Status { get; set; } = "Pending";
    }
}