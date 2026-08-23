## What changed and why

<!-- One or two sentences. Link an issue if there is one. -->

## Type

- [ ] Feature
- [ ] Fix
- [ ] Chore / refactor / docs
- [ ] Infra (CI/CD, hosting, Docker)

## Checklist

- [ ] `dotnet test FinanceApp.slnx` passes locally (or CI is green)
- [ ] Mobile: `npx tsc --noEmit` passes locally (or CI is green)
- [ ] If this touches `FinanceApp.Web/Program.cs` or `FinanceApp.API/Program.cs`: the equivalent change was made in **both**, if both surfaces need it (see `CLAUDE.md`)
- [ ] If this touches currency math: routed through `ICurrencyConversionService`, not raw amount comparisons
- [ ] If this adds/changes a migration: tested `Down()`, and it was scaffolded with real data in mind, not just applied blind
- [ ] No secrets, connection strings, or API keys in the diff

## How to verify

<!-- Steps a reviewer can follow to see this working. -->
