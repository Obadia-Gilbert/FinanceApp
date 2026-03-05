import { apiFetch } from './client';
import type { MonthlyReportResult } from '../types/api';

export async function getMonthlyReport(
  year: number,
  month: number,
  currency?: string
): Promise<MonthlyReportResult> {
  const params = new URLSearchParams({ year: String(year), month: String(month) });
  if (currency) params.set('currency', currency);
  return apiFetch<MonthlyReportResult>(`/api/reports/monthly?${params}`);
}
