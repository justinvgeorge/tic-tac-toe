# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Angular + .NET Web API tic-tac-toe app, built for a technical interview assignment. Both halves are complete: `backend/TicTacToe.Api` (ASP.NET Core Web API, in-memory state) and `frontend` (Angular 20, standalone components, project name `tic-tac-toe-ui`, files live directly under `frontend/` — not nested in a subfolder). Product requirements are in `docs/requirements_v2.md` (supersedes `docs/requirements.md`); the frontend architecture/flows are in `docs/frontend-spec_v2.md` (supersedes `docs/frontend-spec.md`) — read those for the full detail this file intentionally doesn't repeat.

## Commands

Backend, run from `backend/`:
- Build: `dotnet build TicTacToe.sln`
- Run: `dotnet run --project TicTacToe.Api` (HTTP `http://localhost:5201`, HTTPS `https://localhost:7221`; Swagger UI at `/swagger` in Development)
- No test project exists for the backend.

Frontend, run from `frontend/`:
- Install: `npm install`
- Dev server: `npx @angular/cli@20 serve` — serves on `http://localhost:4200`, proxies `/api/*` to the backend per `proxy.conf.js`
- Build: `npx @angular/cli@20 build`
- Unit tests: `npx @angular/cli@20 test --watch=false --browsers=ChromeHeadless`
- Pin the CLI to `@20`: this machine's Node (24.14.x) is flagged "Unsupported" by Angular CLI 19; CLI 20 runs clean. Don't rely on a bare `ng`/`@angular/cli@latest` without checking this first.

Run both together for local dev: start the backend, then `ng serve` in `frontend/` — the browser only ever talks to `localhost:4200`; the proxy makes the hop to `:7221` server-side (see Known constraints below).

## Project structure

- `backend/TicTacToe.Api/` — `Controllers/`, `Services/`, `Repositories/`, `Models/`, `DTOs/` (see Architecture below).
- `frontend/src/app/`
  - `components/` — `game-container` (top-level state holder + orchestration), `game-board`, `mode-selector`, `move-history`, `scoreboard`. One folder per component (`.ts`/`.html`/`.css`/`.spec.ts`).
  - `services/game.service.ts` — thin `HttpClient` wrapper, one method per backend endpoint, no state.
  - `models/` — one file per DTO/enum, mirroring the backend's DTOs (see API contract below); barrel-exported via `models/index.ts`.
- `frontend/public/images/` — the design assets (`background.jpg`, `x-solid-full.svg`, `o-solid-full.svg`), copied here from `./stylesheet` (the source-of-truth asset drop folder at repo root) since this is Angular's actual served/build-output asset directory.
- `frontend/proxy.conf.js` — dev-server proxy config (see below).
- `docs/` — `requirements.md`/`requirements_v2.md`, `frontend-spec.md`/`frontend-spec_v2.md`.

## Backend architecture

The backend is a layered ASP.NET Core Web API, all state held **in memory** (no database):

- **Controllers** (`Controllers/`) — thin, catch domain exceptions and map them to HTTP status codes (`KeyNotFoundException` → 404, `InvalidOperationException`/`ArgumentException` → 400). `GamesController` at `api/games`, `ScoreboardController` at `api/scoreboard`.
- **Services** (`Services/`) — hold all game logic:
  - `GameService` orchestrates game creation, moves, undo, and reset. It validates moves, applies them, evaluates end-of-game state, and — for `VsComputer` games — immediately triggers the computer's reply move within the same `MakeMove` call (so a single move request can advance the game by two plies).
  - `GameRules` is a static helper with the 8 winning combinations; `CheckWin`/`CheckDraw` operate directly on the `Player?[,]` board.
  - `ComputerPlayerService` implements the AI opponent (always plays `O`) via a fixed heuristic: win if possible → block opponent's win → take center → take a corner → take any remaining cell.
  - `ScoreboardService` records win/draw results and reads/resets the running scoreboard.
- **Repositories** (`Repositories/`) — `InMemoryGameRepository` (`ConcurrentDictionary<Guid, Game>`) and `InMemoryScoreboardRepository` (plain counters). Both are singletons in `Program.cs`, so all game/scoreboard state resets on app restart and is shared across all clients (no per-user isolation).
- **Models** (`Models/`) vs **DTOs** (`DTOs/`): `Game.Board` is a 2D array (`Player?[,]`) internally; DTOs expose it as a jagged array (`Player?[][]`) since 2D arrays don't serialize cleanly to JSON. `GameService.MapToDto` does this conversion, and also embeds the current `ScoreboardDto` into every `GameStateDto` response.
- **Undo semantics**: undo pops one move in `TwoPlayer` mode, but pops *two* moves (computer's reply + the preceding human move) in `VsComputer` mode, so undo always returns control to the human — unless there's only one move in history.
- Game state machine: `GameStatus` is `InProgress` → `Won` | `Draw`. `Player` is `X` | `O`; `X` always moves first. `GameMode` is `TwoPlayer` | `VsComputer`.

DI wiring is in `Program.cs`: repositories are singletons, services are scoped. CORS (`AllowAngularDev` policy) is locked to `http://localhost:4200` with a 30-minute preflight cache (`SetPreflightMaxAge`) — though in normal dev usage the browser never triggers CORS at all, since the frontend proxy makes requests same-origin (see Known constraints).

## API contract

Base path `/api`. Every `GameStateDto` response embeds the current `ScoreboardDto`. Wire JSON is **camelCase** (ASP.NET Core's default `System.Text.Json` policy), even though the C# properties are PascalCase — frontend model field names match the wire format, not the C# source.

| Method | Route | Body | Returns |
|---|---|---|---|
| POST | `/games` | `{ mode }` | `GameStateDto` |
| GET | `/games/{id}` | — | `GameStateDto` |
| POST | `/games/{id}/moves` | `{ player, row, column }` | `GameStateDto` |
| POST | `/games/{id}/undo` | — | `GameStateDto` |
| POST | `/games/{id}/reset` | — | `GameStateDto` |
| GET | `/scoreboard` | — | `ScoreboardDto` |
| POST | `/scoreboard/reset` | — | `ScoreboardDto` |

`GameStateDto`: `gameId, board (Player?[][]), currentPlayer, mode, status, winner, winningCells (int[][]?), moveHistory (MoveDto[]), scoreboard`. Enum wire values: `Player` X=0/O=1, `GameMode` TwoPlayer=0/VsComputer=1, `GameStatus` InProgress=0/Won=1/Draw=2. Errors are `{ message: string }` with 400 (invalid/occupied/wrong-turn move) or 404 (unknown game id). Full frontend-side model definitions: `frontend/src/app/models/`.

## Key architectural decisions

- **Backend owns all game state and rules.** The frontend never computes moves, wins, undo results, or history locally — every action is a round trip that replaces the held `GameStateDto` wholesale with the response. `GameContainer.gameState` is the single frontend source of truth (a signal), not a separate state-management service.
- **Undo differs by mode**: `TwoPlayer` removes one move; `VsComputer` removes the pair (computer's reply + the human move before it), always handing the turn back to the human. This is entirely a backend behavior (`GameService.UndoMove`) — the frontend just displays whatever board/history comes back.
- **Mode is immutable per game.** Set once at `POST /games`, never changed by `reset`. Switching modes means creating a brand-new game (new `gameId`) — see "Change Mode" below, distinct from Reset Game.
- **Scoreboard is global, server-side state**, not tied to any one game — creating/resetting a game never touches it; only `POST /scoreboard/reset` does.

## Notable UX features beyond the base requirements

These aren't in `docs/requirements_v2.md` — they were added during the build and are documented in detail in `docs/frontend-spec_v2.md` (flows 1a, 2, and 5):

- **Change Mode** (`GameContainer.onChangeModeClick`) — a dedicated action, distinct from Reset Game, that starts a brand-new game via `createGame`. Confirms first (native `confirm()`) only if the current game is `InProgress`; no prompt once `Won`/`Draw`.
- **Auto-reset countdown** — on a fresh transition to `Won`/`Draw`, a 5s countdown starts (`AUTO_RESET_SECONDS`, shown as "Next game starting in Xs..."); on elapse it calls `resetGame` automatically. Cancelled if the user manually triggers Undo/Reset/Change Mode first, or on component destroy.
- **Reset Scoreboard confirmation** — `Scoreboard.onResetClick` shows a `confirm()` prompt before calling the endpoint; cancelling makes no API call. Same confirmation mechanism as Change Mode, not a separate UI pattern.
- **Delayed computer-move reveal (Vs Computer mode only)** — `makeMove` returns both the human's and computer's move already applied in one response, but the frontend holds it back briefly (`COMPUTER_MOVE_DELAY_MS` in `game-container.ts`, currently **200ms** — note: this was set to 500ms in the prior session and now reads 200ms on disk; if that wasn't an intentional further tweak, it's worth confirming) before applying it, so the computer's move reads as a separate, deliberate turn. The human's own move renders instantly via a local optimistic mark (`GameBoard.optimisticMark`/`displayValue` — a pure rendering overlay, not game logic). The board and all action buttons are disabled for the duration (`pendingComputerMove` signal) so a click can't fire a second move mid-delay; the pending timer is cleared on destroy or on Undo/Reset/Change Mode.
- **Animations** — mark placement (scale-bounce, since the X/O SVGs are filled paths, not stroke-based — see `game-board.css`), a drawn win-line (SVG `stroke-dashoffset`, timed to start after the placement bounce finishes), and a move-history slide-in on new entries. All gated behind `prefers-reduced-motion: no-preference`.

## Known constraints / assumptions

- **In-memory backend state**: everything resets on backend restart; state is shared across all frontend clients (no auth/per-user isolation) — expected for this exercise, not a bug.
- **Dev-server proxy hides the backend from browser DevTools.** The frontend calls relative `/api/...` paths; `ng serve`'s proxy (`proxy.conf.js`, targeting `https://localhost:7221`, with an explicit keep-alive `https.Agent` — without it, every proxied request pays a fresh TLS handshake, ~30-130ms) forwards server-side. The browser's Network tab will only ever show `localhost:4200` requests, never `:7221` — that's expected, not a sign the backend isn't being called.
- **Mode is set only at game creation**, not mutable in place — see Change Mode above.
- No frontend routing (single view, gated by component state, not URL).

## Design tokens / palette

Defined once as CSS custom properties in `frontend/src/styles.css` (`:root`), consumed by every component's own stylesheet — don't hardcode these hex values elsewhere:
- `--color-tile` (`#e8b563`), `--color-tile-frame` (`#c97b3d`), `--color-tray-base` (`#4a2e1e`), `--color-button-fill` (`#f5e6d3`), `--color-button-text` (`#4a2e1e`), `--color-win-highlight` (`#f6c445`), `--color-win-outline` (`#fff2c4`).
- The shared pill-button styling (`.pill-button`, icon-above-label, disabled state) also lives in `styles.css` as a global class, not per-component, so Undo/Reset Game/Change Mode/Reset Scoreboard/mode-selector buttons all stay visually consistent.
- Background image: `background-image: url('/images/background.jpg')` on `body`, `background-size: cover`.
