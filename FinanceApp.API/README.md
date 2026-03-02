# FinanceApp.API

REST API for FinanceApp. Uses the same Application and Infrastructure layers as the Web project. Authenticates via JWT.

## Configuration

- **Connection string**: Override in `appsettings.Development.json` or environment variables. Default uses SQL Server LocalDB (Windows only). On macOS, use a SQL Server instance (e.g. Docker) or configure your own connection.
- **JWT**: Set `Jwt:Key` (min 32 chars) in appsettings. Use a strong secret in production.

## Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | No | Login with email/password, returns JWT |
| GET | `/api/expenses` | JWT | List expenses (paginated) |
| GET | `/api/expenses/{id}` | JWT | Get expense by id |
| POST | `/api/expenses` | JWT | Create expense |
| PUT | `/api/expenses/{id}` | JWT | Update expense |
| DELETE | `/api/expenses/{id}` | JWT | Soft delete expense |
| GET | `/api/categories` | JWT | List categories |
| GET | `/api/categories/{id}` | JWT | Get category |
| POST | `/api/categories` | JWT | Create category |
| PUT | `/api/categories/{id}` | JWT | Update category |
| DELETE | `/api/categories/{id}` | JWT | Delete category |
| GET | `/api/budgets?month=&year=` | JWT | Get monthly budget |
| PUT | `/api/budgets` | JWT | Set monthly budget |
| GET | `/api/budgets/category?month=&year=` | JWT | List category budgets |
| PUT | `/api/budgets/category/{categoryId}?month=&year=` | JWT | Set category budget |
| DELETE | `/api/budgets/category/{id}` | JWT | Delete category budget |
| GET | `/api/dashboard` | JWT | Dashboard summary |

## Example: Login and use token

```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5022/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"your@email.com","password":"yourpassword"}' \
  | jq -r '.token')

# Use token
curl -H "Authorization: Bearer $TOKEN" http://localhost:5022/api/dashboard
```
