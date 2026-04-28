import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  CreateTransactionPayload,
  Transaction,
  UpdateTransactionPayload
} from './transaction.model';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/transactions`;

  readonly transactions = signal<Transaction[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<Transaction[]>(this.baseUrl).subscribe({
      next: (data) => {
        this.transactions.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.toMessage(err));
        this.loading.set(false);
      }
    });
  }

  getById(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.baseUrl}/${id}`);
  }

  create(payload: CreateTransactionPayload): Observable<Transaction> {
    return this.http
      .post<Transaction>(this.baseUrl, payload)
      .pipe(tap((created) => this.transactions.update((list) => [created, ...list])));
  }

  update(payload: UpdateTransactionPayload): Observable<Transaction> {
    const { id, ...body } = payload;
    return this.http
      .put<Transaction>(`${this.baseUrl}/${id}`, { id, ...body })
      .pipe(
        tap((updated) =>
          this.transactions.update((list) =>
            list.map((t) => (t.id === updated.id ? updated : t))
          )
        )
      );
  }

  remove(id: string): Observable<void> {
    return this.http
      .delete<void>(`${this.baseUrl}/${id}`)
      .pipe(tap(() => this.transactions.update((list) => list.filter((t) => t.id !== id))));
  }

  private toMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'message' in err) {
      return String((err as { message: unknown }).message);
    }
    return 'Request failed';
  }
}
