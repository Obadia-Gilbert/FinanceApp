# FinanceApp Mobile

React Native (Expo) mobile app for FinanceApp. Connects to the same REST API as the web app.

## Run on your phone

1. **Same Wi‑Fi:** Phone and computer must be on the same network.

2. **Get your computer’s IP**
   - **Mac/Linux:** Terminal → `ifconfig` or `ip addr` → find `192.168.x.x` (Wi‑Fi).
   - **Windows:** CMD → `ipconfig` → find IPv4 Address for your Wi‑Fi adapter.

3. **Configure API URL**
   - Copy `.env.example` to `.env`.
   - Set:
     ```bash
     EXPO_PUBLIC_API_URL=http://YOUR_IP:5279
     ```
     Example: `EXPO_PUBLIC_API_URL=http://192.168.1.100:5279`

4. **Start the API** (in the repo root):
   ```bash
   dotnet run --project FinanceApp.API
   ```
   The API listens on `http://0.0.0.0:5279` (or the port in launchSettings). Allow it through Windows Firewall if prompted.

5. **Start Expo and open on phone**
   ```bash
   cd FinanceApp.Mobile
   npm install
   npx expo start
   ```
   - Install **Expo Go** on your phone (App Store / Play Store).
   - Scan the QR code from the terminal (or from the browser that opens).
   - The app will load and talk to the API at your computer’s IP.

**"Network request failed" / "Pull down to retry" (simulator or phone):**

- **iOS Simulator:** `localhost` in `.env` does *not* work (the simulator sees its own loopback, not your Mac). Set `EXPO_PUBLIC_API_URL=http://YOUR_MAC_IP:5279` (e.g. `http://192.168.100.132:5279`). Find your Mac IP: `ipconfig getifaddr en0` or System Settings → Network.
- **All cases:** Start the API with the Mobile profile so it listens on port 5279 (see step 1 below). If the API runs without `--launch-profile Mobile`, it uses port 5022 and the app will fail to connect.
- After changing `.env`, restart Expo; if needed clear cache: `npx expo start -c`.

1. **Start the API with the Mobile profile** (from the repo root) so it listens on port 5279:
   ```bash
   dotnet run --project FinanceApp.API --launch-profile Mobile
   ```
   Without `--launch-profile Mobile`, the API runs on port 5022 and the app (which uses 5279) will get "network request failed".

2. **Confirm `.env`** has your Mac’s current IP (e.g. `EXPO_PUBLIC_API_URL=http://192.168.100.132:5279`). If you change `.env`, restart Expo (`npx expo start`).

3. **Allow the API through the firewall** if your Mac asks when you first run the API.

**Alternative (different networks):** Use tunnel so the phone can reach your dev server:
```bash
npx expo start --tunnel
```
Then still set `EXPO_PUBLIC_API_URL` to a URL the phone can reach (e.g. a deployed API or ngrok URL for the API).

## Setup (summary)

1. **Install dependencies** (Node 18+):
   ```bash
   cd FinanceApp.Mobile
   npm install
   ```

2. **Configure API URL** — copy `.env.example` to `.env` and set `EXPO_PUBLIC_API_URL` (see above).

3. **Run the API** (from solution root) **with the Mobile profile** so it listens on port 5279 and is reachable from simulator/phone:
   ```bash
   dotnet run --project FinanceApp.API --launch-profile Mobile
   ```
   (Plain `dotnet run --project FinanceApp.API` uses port 5022; the app expects 5279.)

4. **Start Expo:** `npx expo start` — then press `i`/`a` for simulator/emulator or scan QR with Expo Go on your phone.

## Features

- **Auth:** Login, Register, JWT + refresh token (stored in SecureStore).
- **Dashboard:** KPIs (total spend, this month, expense count, categories), budget alerts, 30-day expense trend chart.
- **Expenses:** List, add, edit, delete; category picker; multi-currency.
- **Income:** List, add, edit, delete; category and optional account; multi-currency.
- **Accounts:** List, add, edit, deactivate; balance display.
- **Transactions:** List; add income/expense; transfer between accounts.
- **Budget:** View/set monthly budget; progress bar; over-budget alert.
- **More:** Profile, Income, Accounts, Transactions, Categories, Monthly report, Notifications, Subscription, Privacy, Sign out.
- **Notifications:** List, mark read, mark all read.
- **Reports:** Monthly report (spent, income, net cash flow, by category, top expenses).
- **Subscription:** View current plan.
- **Theme:** Light/dark (theme context; can add toggle in Profile).

## Stack

- **Expo** ~52, **React Native** 0.76
- **Expo Router** (file-based routing: `app/`)
- **TanStack Query** (server state)
- **SecureStore** (tokens), **AsyncStorage** (theme preference)

## Project structure

- `app/` — Routes: `index` (redirect), `(auth)/login`, `(auth)/register`, `(tabs)/` (Dashboard, Expenses, Budget, More).
- `src/api/` — API client (JWT, refresh), auth, dashboard, expenses, categories, budget, profile.
- `src/context/` — AuthContext, ThemeContext.
- `src/theme/` — Light/dark colors (aligned with web).
- `src/components/` — Card, Button, Input.

## Troubleshooting

**"Unable to run simctl: xcrun simctl help exited with non-zero code: 72"**

This appears when Expo checks for the iOS Simulator but Xcode/CLI tools aren’t set up correctly. **Metro still starts** and you can use Expo Go on a physical device or run in the browser. To fix the warning (and use the iOS Simulator):

1. Install Xcode from the App Store (or ensure it’s up to date).
2. Point the active developer directory to Xcode:
   ```bash
   sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
   ```
3. Accept the Xcode license if prompted:
   ```bash
   sudo xcodebuild -license accept
   ```
4. Open Xcode once and install any requested components (e.g. additional simulators).

To start without triggering the simulator check, you can run the dev server for web only: `npm run start:web`.

## Adding more features

- **Income:** Use `GET/POST /api/income` (see API controllers).
- **Accounts, Transactions, Recurring, Reports, Subscription, Notifications:** Add API modules and screens under `(tabs)` or nested stacks as needed.
