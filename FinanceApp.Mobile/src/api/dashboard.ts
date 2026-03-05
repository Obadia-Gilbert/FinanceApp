import { apiFetch } from './client';
import type { DashboardDto } from '../types/api';

export async function getDashboard(): Promise<DashboardDto> {
  const raw = await apiFetch<{
    totalSpend: number;
    displayCurrency: string;
    expenseCount: number;
    categoryCount: number;
    thisMonthSpend: number;
    budgetAmount: number | null;
    budgetCurrency: string | null;
    isOverBudget: boolean;
    chartData: { date: string; amount: number }[];
    categoryBudgetAlerts: {
      categoryName: string;
      spent: number;
      budget: number;
      currency: string;
      isOver: boolean;
    }[];
  }>('/api/dashboard');
  return {
    ...raw,
    chartData: raw.chartData.map((d) => ({ date: d.date, amount: d.amount })),
  };
}
