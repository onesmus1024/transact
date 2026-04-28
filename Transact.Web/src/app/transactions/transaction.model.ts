export type TransactionType = 'P2P' | 'Merchant' | 'Paybill' | 'Withdrawal';

export const TRANSACTION_TYPES: TransactionType[] = ['P2P', 'Merchant', 'Paybill', 'Withdrawal'];

export const TRANSACTION_STATUSES = ['Pending', 'Completed', 'Failed', 'Reversed'] as const;
export type TransactionStatus = (typeof TRANSACTION_STATUSES)[number];

export interface Transaction {
  id: string;
  amount: number;
  currency: string;
  type: TransactionType;
  createdAt: string;
  senderId: string;
  receiverId: string;
  status: string;
}

export interface CreateTransactionPayload {
  amount: number;
  currency: string;
  type: TransactionType;
  senderId: string;
  receiverId: string;
}

export interface UpdateTransactionPayload extends CreateTransactionPayload {
  id: string;
  status: string;
}
