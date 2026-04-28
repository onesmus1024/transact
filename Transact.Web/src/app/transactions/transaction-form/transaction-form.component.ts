import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { TransactionService } from '../transaction.service';
import {
  CreateTransactionPayload,
  TRANSACTION_STATUSES,
  TRANSACTION_TYPES,
  TransactionType,
  UpdateTransactionPayload
} from '../transaction.model';

@Component({
  selector: 'app-transaction-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './transaction-form.component.html'
})
export class TransactionFormComponent implements OnInit {
  private readonly service = inject(TransactionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly types = TRANSACTION_TYPES;
  readonly statuses = TRANSACTION_STATUSES;

  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);

  // Plain mutable form state (compatible with [(ngModel)])
  amount = 0;
  currency = 'KES';
  type: TransactionType = 'P2P';
  senderId = '';
  receiverId = '';
  status = 'Pending';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.editingId.set(id);
      this.loading.set(true);
      this.service.getById(id).subscribe({
        next: (t) => {
          this.amount = t.amount;
          this.currency = t.currency;
          this.type = t.type;
          this.senderId = t.senderId;
          this.receiverId = t.receiverId;
          this.status = t.status;
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.message ?? 'Failed to load transaction');
          this.loading.set(false);
        }
      });
    }
  }

  submit(): void {
    this.error.set(null);

    if (this.amount <= 0) {
      this.error.set('Amount must be greater than zero.');
      return;
    }
    if (!this.senderId.trim() || !this.receiverId.trim()) {
      this.error.set('Sender and receiver are required.');
      return;
    }
    if (this.senderId.trim() === this.receiverId.trim()) {
      this.error.set('Sender and receiver cannot be the same.');
      return;
    }

    this.saving.set(true);
    const id = this.editingId();

    if (id) {
      const payload: UpdateTransactionPayload = {
        id,
        amount: this.amount,
        currency: this.currency.trim() || 'KES',
        type: this.type,
        senderId: this.senderId.trim(),
        receiverId: this.receiverId.trim(),
        status: this.status
      };
      this.service.update(payload).subscribe({
        next: () => this.router.navigate(['/transactions']),
        error: (err) => {
          this.error.set(err?.error ?? err?.message ?? 'Update failed');
          this.saving.set(false);
        }
      });
    } else {
      const payload: CreateTransactionPayload = {
        amount: this.amount,
        currency: this.currency.trim() || 'KES',
        type: this.type,
        senderId: this.senderId.trim(),
        receiverId: this.receiverId.trim()
      };
      this.service.create(payload).subscribe({
        next: () => this.router.navigate(['/transactions']),
        error: (err) => {
          this.error.set(err?.error ?? err?.message ?? 'Create failed');
          this.saving.set(false);
        }
      });
    }
  }
}
