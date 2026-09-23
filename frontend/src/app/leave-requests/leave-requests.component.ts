import { Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Employee, LeaveRequest, LeaveStatus, LeaveType } from '../models/leave-request.model';
import { LeaveRequestService } from '../services/leave-request.service';
import { EmployeeService } from '../services/employee.service';

// Flags an invalid range when the end date is earlier than the start date
// (which is also the only way the computed day count could come out negative).
function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
  const start = group.get('startDate')?.value;
  const end = group.get('endDate')?.value;
  if (!start || !end) {
    return null;
  }
  return new Date(start) > new Date(end) ? { dateRange: true } : null;
}

@Component({
  selector: 'app-leave-requests',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './leave-requests.component.html',
  styleUrls: ['./leave-requests.component.css']
})
export class LeaveRequestsComponent implements OnInit {
  // Exposed so the template can compare against named values instead of magic numbers.
  readonly LeaveStatus = LeaveStatus;

  requests: LeaveRequest[] = [];
  employees: Employee[] = [];
  loading = false;
  submitting = false;
  submitError: string | null = null;

  private approvingIds = new Set<number>();
  private justApprovedIds = new Set<number>();
  approveErrors: Record<number, string> = {};

  form: FormGroup;

  constructor(
    private leaveRequestService: LeaveRequestService,
    private employeeService: EmployeeService,
    private fb: FormBuilder,
    private destroyRef: DestroyRef
  ) {
    this.form = this.fb.group(
      {
        employeeId: [null, Validators.required],
        type: [null, Validators.required],
        startDate: ['', Validators.required],
        endDate: ['', Validators.required]
      },
      { validators: dateRangeValidator }
    );
  }

  ngOnInit(): void {
    this.load();
    this.loadEmployees();
  }

  load(): void {
    this.loading = true;
    this.leaveRequestService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.requests = data;
        this.loading = false;
      });
  }

  loadEmployees(): void {
    this.employeeService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.employees = data;
      });
  }

  submit(): void {
    this.submitError = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { employeeId, type, startDate, endDate } = this.form.value;
    this.submitting = true;

    this.leaveRequestService
      .create({ employeeId, type, startDate, endDate })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting = false;
          this.form.reset();
          this.load();
        },
        error: (err) => {
          this.submitting = false;
          this.submitError = typeof err?.error === 'string' ? err.error : 'Failed to create leave request.';
        }
      });
  }

  approve(id: number): void {
    delete this.approveErrors[id];
    this.approvingIds.add(id);

    this.leaveRequestService
      .approve(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.approvingIds.delete(id);
          // Patch just the status in place: the approve response has no Employee
          // navigation loaded, so replacing the whole row would blank that column.
          const row = this.requests.find((r) => r.id === id);
          if (row) {
            row.status = updated.status;
          }
          this.justApprovedIds.add(id);
          setTimeout(() => this.justApprovedIds.delete(id), 3000);
        },
        error: (err) => {
          this.approvingIds.delete(id);
          this.approveErrors[id] = typeof err?.error === 'string' ? err.error : 'Failed to approve the request.';
        }
      });
  }

  isApproving(id: number): boolean {
    return this.approvingIds.has(id);
  }

  isJustApproved(id: number): boolean {
    return this.justApprovedIds.has(id);
  }

  typeLabel(type: LeaveType): string {
    if (type === LeaveType.Vacation) return 'Vacation';
    if (type === LeaveType.Sick) return 'Sick';
    return 'Unpaid';
  }

  statusLabel(status: LeaveStatus): string {
    if (status === LeaveStatus.Pending) return 'Pending';
    if (status === LeaveStatus.Approved) return 'Approved';
    return 'Rejected';
  }
}

