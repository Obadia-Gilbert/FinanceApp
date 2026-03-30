/** Auth */
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  refreshToken: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

/** Dashboard */
export interface ChartDataPoint {
  date: string;
  amount: number;
}

export interface CategoryBudgetAlertDto {
  categoryName: string;
  spent: number;
  budget: number;
  currency: string;
  isOver: boolean;
}

export interface DashboardDto {
  totalSpend: number;
  displayCurrency: string;
  expenseCount: number;
  categoryCount: number;
  thisMonthSpend: number;
  budgetAmount: number | null;
  budgetCurrency: string | null;
  isOverBudget: boolean;
  chartData: ChartDataPoint[];
  categoryBudgetAlerts: CategoryBudgetAlertDto[];
}

/** Expense */
export interface ExpenseDto {
  id: string;
  amount: number;
  /** API may return enum index (number); use formatCurrencyCode() for display */
  currency: string | number;
  expenseDate: string;
  description: string | null;
  categoryId: string;
  categoryName: string | null;
  receiptPath: string | null;
  createdAt: string;
}

export interface CreateExpenseRequest {
  amount: number;
  currency: string;
  expenseDate: string;
  categoryId: string;
  description?: string | null;
}

export interface UpdateExpenseRequest extends CreateExpenseRequest {}

export interface PagedResultDto<T> {
  items: T[];
  totalItems: number;
  pageNumber: number;
  pageSize: number;
}

/** Category */
export interface CategoryDto {
  id: string;
  name: string;
  type: string;
  description: string | null;
  icon: string | null;
  badgeColor: string;
}

export interface CreateCategoryRequest {
  name: string;
  type?: number;
  description?: string | null;
  icon?: string | null;
  badgeColor?: string | null;
}

export interface UpdateCategoryRequest extends CreateCategoryRequest {}

/** Budget */
export interface BudgetDto {
  id: string;
  month: number;
  year: number;
  amount: number;
  currency: string;
}

export interface CategoryBudgetDto {
  id: string;
  categoryId: string;
  categoryName: string | null;
  month: number;
  year: number;
  amount: number;
  /** API returns enum index (number); use formatCurrencyCode() for display */
  currency: string | number;
  spent: number;
}

export interface SetBudgetRequest {
  month: number;
  year: number;
  amount: number;
  /** Currency: string (e.g. 'USD') or enum index number for API */
  currency: string | number;
}

/** Profile */
export interface ProfileDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  country: string | null;
  countryCode: string | null;
  profileImagePath: string | null;
  preferredLanguage: string;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string | null;
  countryCode?: string | null;
  preferredLanguage?: string;
}

/** Income */
export interface IncomeDto {
  id: string;
  accountId: string | null;
  accountName: string | null;
  categoryId: string;
  categoryName: string | null;
  amount: number;
  currency: string;
  incomeDate: string;
  description: string | null;
  source: string | null;
  createdAt: string;
}

export interface CreateIncomeRequest {
  accountId?: string | null;
  categoryId: string;
  amount: number;
  currency: string;
  incomeDate: string;
  description?: string | null;
  source?: string | null;
}

export interface UpdateIncomeRequest {
  amount: number;
  incomeDate: string;
  accountId?: string | null;
  categoryId: string;
  description?: string | null;
  source?: string | null;
}

/** Account */
export interface AccountDto {
  id: string;
  name: string;
  type: string;
  currency: string;
  initialBalance: number;
  currentBalance: number;
  description: string | null;
  isActive: boolean;
}

export interface CreateAccountRequest {
  name: string;
  type: string;
  currency: string;
  initialBalance: number;
  description?: string | null;
}

export interface UpdateAccountRequest {
  name: string;
  description?: string | null;
}

/** Transaction */
export interface TransactionDto {
  id: string;
  accountId: string;
  accountName: string | null;
  type: string;
  amount: number;
  currency: string;
  date: string;
  categoryId: string | null;
  categoryName: string | null;
  note: string | null;
  transferGroupId: string | null;
  isRecurring: boolean;
  createdAt: string;
}

export interface CreateTransactionRequest {
  accountId: string;
  type: string;
  amount: number;
  currency: string;
  date: string;
  categoryId?: string | null;
  note?: string | null;
  isRecurring?: boolean;
}

export interface CreateTransferRequest {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  currency: string;
  date: string;
  note?: string | null;
}

export interface UpdateTransactionRequest {
  amount: number;
  date: string;
  categoryId?: string | null;
  note?: string | null;
}

/** Notifications */
export interface NotificationItemDto {
  id: string;
  title: string;
  message: string;
  type: string;
  relatedLink: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationListResponse {
  items: NotificationItemDto[];
  totalItems: number;
  pageNumber: number;
  pageSize: number;
}

/** Reports */
export interface CategoryReportLine {
  categoryName: string;
  spent: number;
  budgetAmount: number | null;
  remaining: number | null;
  isOverBudget: boolean;
}

export interface ExpenseReportLine {
  description: string;
  amount: number;
  currency: string;
  date: string;
  categoryName: string;
}

export interface MonthlyReportResult {
  month: number;
  year: number;
  monthName: string;
  totalSpent: number;
  totalIncome: number;
  netCashFlow: number;
  currency: string;
  globalBudgetAmount: number | null;
  globalBudgetSpent: number | null;
  globalBudgetRemaining: number | null;
  isOverGlobalBudget: boolean;
  categoryLines: CategoryReportLine[];
  topExpenses: ExpenseReportLine[];
}

/** Subscription */
export interface SubscriptionDto {
  currentPlan: string;
  subscriptionAssignedAt: string | null;
  subscriptionExpiresAtUtc: string | null;
  billingSource: string;
}

/** Recurring templates */
export interface RecurringTemplateDto {
  id: string;
  accountId: string;
  accountName: string | null;
  categoryId: string | null;
  categoryName: string | null;
  type: number; // 0=Income, 1=Expense
  amount: number;
  currency: number;
  frequency: number; // 0=Weekly, 1=Monthly, 2=Yearly
  interval: number;
  startDate: string;
  endDate: string | null;
  nextRunDate: string;
  note: string | null;
  isActive: boolean;
}

export interface CreateRecurringTemplateRequest {
  accountId: string;
  categoryId?: string | null;
  type: number;
  amount: number;
  currency: number;
  frequency: number;
  startDate: string;
  endDate?: string | null;
  interval?: number;
  note?: string | null;
}

export interface UpdateRecurringTemplateRequest {
  amount: number;
  note?: string | null;
}

/** Feedback */
export interface FeedbackDto {
  id: string;
  type: number; // 0=Question, 1=Suggestion, 2=Comment
  subject: string | null;
  message: string;
  createdAt: string;
}

export interface CreateFeedbackRequest {
  type: number;
  message: string;
  subject?: string | null;
}
