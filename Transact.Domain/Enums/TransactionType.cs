namespace Transact.Domain.Enums
{
    public enum TransactionType
    {
        P2P,          // Person-to-Person
        Merchant,     // Business payment
        Paybill,      // Utility/Bill payment
        Withdrawal    // Agent/ATM cash out
    }
}