import { apiFetch } from './client';
import type { BudgetDto, CategoryBudgetDto, SetBudgetRequest } from '../types/api';

export async function getBudget(month: number, year: number): Promise<BudgetDto | null> {
  try {
    return await apiFetch<BudgetDto>(`/api/budgets?month=${month}&year=${year}`);
  } catch {
    return null;
  }
}

export async function setBudget(body: SetBudgetRequest): Promise<BudgetDto> {
  return apiFetch<BudgetDto>('/api/budgets', {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}

export async function getCategoryBudgets(month: number, year: number): Promise<CategoryBudgetDto[]> {
  return apiFetch<CategoryBudgetDto[]>(`/api/budgets/category?month=${month}&year=${year}`);
}

/** Set or update a category budget. Currency as enum index (0–15). */
export async function setCategoryBudget(
  categoryId: string,
  month: number,
  year: number,
  amount: number,
  currency: number
): Promise<CategoryBudgetDto> {
  return apiFetch<CategoryBudgetDto>(
    `/api/budgets/category/${categoryId}?month=${month}&year=${year}`,
    {
      method: 'PUT',
      body: JSON.stringify({ month, year, amount, currency }),
    }
  );
}

export async function deleteCategoryBudget(id: string): Promise<void> {
  return apiFetch(`/api/budgets/category/${id}`, { method: 'DELETE' });
}
