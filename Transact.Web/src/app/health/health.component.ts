import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { environment } from '../../environments/environment';
import { HealthService } from './health.service';

type CheckState = 'idle' | 'checking' | 'healthy' | 'unhealthy';

@Component({
  selector: 'app-health',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './health.component.html'
})
export class HealthComponent implements OnInit {
  private readonly service = inject(HealthService);

  readonly apiUrl = `${environment.apiBaseUrl}/health`;
  readonly state = signal<CheckState>('idle');
  readonly status = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly latencyMs = signal<number | null>(null);
  readonly lastChecked = signal<Date | null>(null);

  ngOnInit(): void {
    this.check();
  }

  check(): void {
    this.state.set('checking');
    this.error.set(null);
    this.status.set(null);
    this.latencyMs.set(null);

    const started = performance.now();
    this.service.check().subscribe({
      next: (res) => {
        this.latencyMs.set(Math.round(performance.now() - started));
        this.lastChecked.set(new Date());
        this.status.set(res?.status ?? 'Unknown');
        this.state.set(
          (res?.status ?? '').toLowerCase() === 'healthy' ? 'healthy' : 'unhealthy'
        );
      },
      error: (err) => {
        this.latencyMs.set(Math.round(performance.now() - started));
        this.lastChecked.set(new Date());
        this.error.set(this.toMessage(err));
        this.state.set('unhealthy');
      }
    });
  }

  private toMessage(err: unknown): string {
    if (err && typeof err === 'object') {
      const e = err as { status?: number; message?: string; statusText?: string };
      if (e.status === 0) return 'Cannot reach the API. Is it running?';
      if (e.status) return `${e.status} ${e.statusText ?? ''}`.trim();
      if (e.message) return e.message;
    }
    return 'Health check failed';
  }
}
