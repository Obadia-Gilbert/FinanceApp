import { apiFetch } from './client';
import type {
  CategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '../types/api';

export async function getCategories(): Promise<CategoryDto[]> {
  return apiFetch<CategoryDto[]>('/api/categories');
}

export async function getCategory(id: string): Promise<CategoryDto> {
  return apiFetch<CategoryDto>(`/api/categories/${id}`);
}

export async function createCategory(body: CreateCategoryRequest): Promise<CategoryDto> {
  return apiFetch<CategoryDto>('/api/categories', {
    method: 'POST',
    body: JSON.stringify(body),
  });
}

export async function updateCategory(
  id: string,
  body: UpdateCategoryRequest
): Promise<CategoryDto> {
  return apiFetch<CategoryDto>(`/api/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}

export async function deleteCategory(id: string): Promise<void> {
  return apiFetch(`/api/categories/${id}`, { method: 'DELETE' });
}
