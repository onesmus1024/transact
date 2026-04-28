import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

import { TransactionService } from '../transaction.service';

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './transaction-list.component.html'
})
export class TransactionListComponent implements OnInit {
  private readonly service = inject(TransactionService);

  readonly transactions = this.service.transactions;
  readonly loading = this.service.loading;
  readonly error = this.service.error;

  readonly deletingId = signal<string | null>(null);

  ngOnInit(): void {
    this.service.load();
  }

  refresh(): void {
    this.service.load();
  }

  remove(id: string): void {
    if (!confirm('Delete this transaction? This cannot be undone.')) return;
    this.deletingId.set(id);
    this.service.remove(id).subscribe({
      next: () => this.deletingId.set(null),
      error: (err) => {
        alert(`Failed to delete: ${err?.message ?? 'unknown error'}`);
        this.deletingId.set(null);
      }
    });
  }
}
