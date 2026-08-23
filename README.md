# Tic Tac Toe

A full-stack tic-tac-toe app with an ASP.NET Core
Web API backend that owns all game state and rules, and an Angular frontend that renders
whatever the backend returns.

## 0.  Quick Setup

```bash
git clone https://github.com/justinvgeorge/tic-tac-toe
cd tic-tac-toe/
cd backend/
dotnet build TicTacToe.sln
dotnet run --project TicTacToe.Api
```

The API is listening at `https://localhost:7221/`

Open another terminal at `tic-tac-toe/frontend`:

```bash
npm install
npm start
```
**Note:** After running `npm start`, the Angular dev server takes 
roughly 30-60 seconds to compile and bundle the application before 
it's ready. This is expected, wait for output similar to the following 
before opening the app in your browser:

```
Application bundle generation complete. [4.292 seconds]
Watch mode enabled. Watching for file changes...
  ➜  Local:   http://localhost:4200/
```

Once you see the `Local: http://localhost:4200/` line, the app is ready 
to open.

## 1. Project Overview

Two play modes : Two Player and Play vs Computer; with move history, a session-level
scoreboard, undo, and reset. The backend is the sole source of truth: it validates every
move, computes win/draw state, and (in Vs Computer mode) plays the computer's reply move
server-side in the same request. The frontend never computes a move, a win, or history
locally, it only calls the API and displays the response.

Full product requirements: `docs/requirements_v2.md`. Full frontend architecture and user
flows: `docs/frontend-spec_v2.md`.

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/): a current LTS or later (Angular 20 requires a recent Node; this project was built and verified against Node 24.14.1)
- Git

### Getting the code

```bash
git clone <this-repo-url>
cd tic-tac-toe
```

Then follow sections 4 and 5 below, start the backend first, then the frontend.

## 2. Tech Stack

- **Backend**: ASP.NET Core Web API (.NET 8), C#, in-memory state (no database), Swagger/OpenAPI in Development.
- **Frontend**: Angular 20, standalone components (no NgModules), TypeScript, plain CSS (no UI framework), RxJS (via `HttpClient`).
- **Testing**: Karma + Jasmine (frontend unit tests); xUnit (backend unit tests, `backend/TicTacToe.Api.Tests`).
- **Dev tooling**: Angular CLI dev-server proxy (`proxy.conf.js`) bridging the frontend's `http://localhost:4200` to the backend's `https://localhost:7221`.

## 3. Features Implemented

Base requirements (`docs/requirements_v2.md`):
- 3x3 board, click to place, cells lock once played or once the game ends.
- Two Player and Vs Computer modes; computer always plays O, following win → block → center → corner → any-available priority.
- Win detection (row/column/diagonal) with winning-cell highlighting; draw detection.
- Move history (move number, player, cell).
- Undo Last Move: removes one move in Two Player mode, removes the pair (computer + human) in Vs Computer mode; disabled with no moves to undo or once the game is Won/Draw.
- Reset Game (same game id and mode, scoreboard untouched) and Reset Scoreboard (separate action), both backend-driven.

Notable additions beyond the base requirements:
- **Change Mode**: a dedicated action (distinct from Reset Game) that starts a brand-new game with a freshly chosen mode, since mode is immutable once a game is created. Confirms first only if the current game is still in progress.
- **Auto-reset countdown**: on Won/Draw, a 5-second "Next game starting in Xs..." countdown automatically resets the game; cancelled if the user acts manually first.
- **Reset Scoreboard confirmation**: a confirmation prompt before clearing X/O wins and draws (same confirmation pattern as Change Mode).
- **Delayed computer-move reveal** (Vs Computer only): the backend returns both the human's and computer's move in one response, but the frontend holds the computer's move back briefly (currently 200ms: see note under Known Limitations) so it reads as a separate turn rather than appearing instantly; the human's own move still renders immediately via a local optimistic overlay.
- **Animations**: mark-placement bounce, an SVG-drawn win line, and a move-history slide-in, all respecting `prefers-reduced-motion`.
- Custom warm-palette visual design (rounded "tile" board, pill-shaped buttons) matching a supplied reference design and asset set (`./stylesheet`).

## 4. How to Run the Backend Locally

From `backend/`:

```bash
dotnet build TicTacToe.sln
dotnet run --project TicTacToe.Api
```

Serves on `http://localhost:5201` and `https://localhost:7221`; Swagger UI at `/swagger`
in Development. No `appsettings` changes needed, state is in-memory and resets on restart.

## 5. How to Run the Frontend Locally

From `frontend/`:

```bash
npm install
npm start
```

Serves on `http://localhost:4200`. Requires the backend running first the dev-server
proxy (`proxy.conf.js`) forwards `/api/*` to `https://localhost:7221`. Open
`http://localhost:4200` in a browser; there's nothing to configure.

`npm start` runs `ng serve` via the Angular CLI already pinned in `package.json`
(`@angular/cli ^20.3.34`), installed locally by `npm install`, no global CLI install or
version pinning needed. (If you ever invoke a *global* `ng`/`npx @angular/cli@latest`
instead of the project's local one, make sure it resolves to a v20+ CLI, v19 flags newer
Node versions like 24.x as unsupported.)

## 6. API Endpoint Summary

Base path `/api`. All responses are JSON, camelCase (ASP.NET Core's default
`System.Text.Json` policy the wire format doesn't match the C# PascalCase source).
Every `GameStateDto` response embeds the current `ScoreboardDto`.

| Method | Route | Body | Returns |
|---|---|---|---|
| POST | `/games` | `{ mode }` | `GameStateDto` |
| GET | `/games/{id}` | — | `GameStateDto` |
| POST | `/games/{id}/moves` | `{ player, row, column }` | `GameStateDto` |
| POST | `/games/{id}/undo` | — | `GameStateDto` |
| POST | `/games/{id}/reset` | — | `GameStateDto` |
| GET | `/scoreboard` | — | `ScoreboardDto` |
| POST | `/scoreboard/reset` | — | `ScoreboardDto` |

`GameStateDto`: `gameId, board (Player?[][]), currentPlayer, mode, status, winner,
winningCells (int[][]?), moveHistory (MoveDto[]), scoreboard`. Enum wire values:
`Player` X=0/O=1, `GameMode` TwoPlayer=0/VsComputer=1, `GameStatus`
InProgress=0/Won=1/Draw=2. Errors: `{ message: string }`, 400 (invalid move) or 404
(unknown game id). Swagger UI (backend running, Development mode) has the live/interactive
version; frontend-side TypeScript models: `frontend/src/app/models/`.

## 7. How to Run Tests

**Backend** (xUnit, `backend/TicTacToe.Api.Tests`), from `backend/`:

```bash
dotnet test TicTacToe.sln
```

48 tests covering `GameRules` (all 8 winning lines, draw detection), `ComputerPlayerService`
(win → block → center → corner → fallback priority, including the full-board error case),
`ScoreboardService`, and `GameService` (move validation, win/draw detection, the
Vs-Computer auto-reply, both undo variants, and reset), all against the real in-memory
repositories, no mocking library needed. Controllers themselves aren't covered (no
integration/HTTP-level tests yet: see Known Limitations).

**Frontend** (Karma + Jasmine), from `frontend/`:

```bash
npm install   # if not already done
npx ng test --watch=false --browsers=ChromeHeadless
```

Covers all five components and the game container, including `fakeAsync`-driven tests for
the auto-reset countdown and the delayed computer-move timer (verifying exact timing,
cancellation on manual actions, and cleanup on destroy).

## 8. AI Tools and Prompt Summary

The frontend (`frontend/`) was built with Claude Code (Anthropic):
all Angular scaffolding, components, services, models, styling, animations, and this
documentation. Work proceeded feature-by-feature via targeted prompts rather than one
large generation, roughly: reading the existing backend to summarize its API contract →
frontend spec drafts (v1, then v2 after clarifying requirements) → Angular project
scaffold + proxy setup → models + API service → one prompt per component (game board,
mode selector, scoreboard, move history) → top-level container wiring → bug-fix passes
(root component never rendered the tree; dev-server proxy latency) → incremental feature
prompts (Change Mode, auto-reset countdown, Reset Scoreboard confirmation, styling pass
against a reference design + provided assets, placement/win animations, delayed
computer-move reveal) → this README. Each change was verified with `tsc --noEmit`,
`ng test`, and `ng build` before moving on; UI changes were additionally checked against
a live-running instance (screenshots, and for animation/timing behavior, direct
`getAnimations()`/`fakeAsync` inspection) rather than taken on faith.

## 9. Design Decisions

- **Backend as sole source of truth**: the frontend holds one signal (`GameContainer.gameState`)
  that's wholesale-replaced by every API response: never patched, never computed locally.
  No separate frontend state-management library or service.
- **Standalone Angular components** throughout, no NgModules, per the project's stated convention.
- **Shared pill-button styling as one global CSS class** (`.pill-button` in `styles.css`)
  rather than duplicated per-component styles, so every action button (Undo, Reset Game,
  Change Mode, Reset Scoreboard, mode selection) stays visually consistent from one
  definition.
- **Design tokens as CSS custom properties** (`:root` in `frontend/src/styles.css`) rather
  than hardcoded hex values scattered across component stylesheets.
- **Change Mode as a distinct action from Reset Game**: since the backend treats `Mode` as
  immutable per game, switching modes has to create a new game (new `gameId`); reusing
  Reset Game for this would have been incorrect.
- **CSS-driven animations with no manual "is this new" bookkeeping**: Angular's `@for`
  `track` semantics already guarantee existing DOM nodes are reused across state updates,
  and CSS animations only fire on a node's actual creation, so a plain unconditional
  `animation:` declaration on marks/history entries is correct by construction, verified
  empirically (not just assumed) via `getAnimations()` timing checks on the live app.
- **Filled-path SVG icons → scale-bounce placement animation, not stroke-drawing**: the
  provided X/O assets are single filled `<path>`s (Font Awesome solid icons), not
  stroke-based, so a "draw the stroke" animation wasn't applicable; a physical
  "token placed" bounce was used instead, for both marks, for consistency.

## 10. Clarifications and Assumptions

- **Reset Game vs. Change Mode**: resolved explicitly in `docs/requirements_v2.md`: Reset
  Game reuses the same game id and mode; Change Mode (switching modes) always creates a new
  game id. An earlier draft of the frontend spec (`docs/frontend-spec.md`) had left this as
  an open assumption before the v2 requirements resolved it.
  Assumed: Change Mode discards the current game and its scoreboard is unaffected.
- **Undo disabled after game completion**: `docs/requirements_v2.md` states this was a
  deliberate choice ("Clarification 2") over the alternative of allowing undo past a
  completed game (which would require reversing scoreboard increments) implemented
  exactly as specified.
- **JSON casing**: assumed nothing about wire format in advance; verified empirically by
  running the backend and inspecting a real response (`camelCase`, not the C# source's
  PascalCase) before writing the frontend models, since ASP.NET Core's default JSON policy
  isn't obvious from reading the C# code alone.
- **Scoreboard is a single global counter**, not per-game or per-user: matches the
  backend's actual implementation (`InMemoryScoreboardRepository`, a singleton with plain
  counters), consistent with "session-level scoreboard" in the requirements for a
  single-instance, no-auth app.

## 11. Known Limitations

- **In-memory backend state**: all games and the scoreboard reset on backend restart;
  state is shared across every connected client, there's no authentication or per-user
  isolation. Expected for this exercise, not a bug.
- **Backend test coverage stops at the service layer.** `GameService`, `GameRules`,
  `ComputerPlayerService`, and `ScoreboardService` have unit tests
  (`backend/TicTacToe.Api.Tests`), but `GamesController`/`ScoreboardController` themselves
  (routing, status-code mapping, request binding) have no automated coverage yet, only
  manual/`curl`-level verification during development.
- **No frontend routing**: a single view gated by component state, not URL-addressable.
- **Dev-server proxy required locally**: the frontend calls relative `/api/...` paths and
  depends on `ng serve`'s proxy to reach the backend; there's no production deployment
  configuration (e.g. a real reverse proxy or same-host hosting) set up.
- **`COMPUTER_MOVE_DELAY_MS`** in `frontend/src/app/components/game-container/game-container.ts`
  currently reads `200`ms on disk. It was set to `500`ms earlier in this project's history:
  worth confirming which value is actually intended before treating this as final.
- No E2E/integration tests: verification during development relied on unit tests plus
  manual and scripted (Puppeteer, ad hoc) browser checks, not a standing E2E suite.

## 12. Future Improvements

- Controller-level integration tests (`GamesController`/`ScoreboardController`: routing,
  status-code mapping, request validation) using `WebApplicationFactory`, to close the gap
  left by the current service-layer-only backend test coverage.
- Frontend E2E tests (e.g. Playwright) covering full user flows end-to-end against a real
  backend, replacing the ad hoc manual/scripted verification used during development.
- Persistent storage (replace the in-memory repositories) if this were to become a real
  multi-user product, plus authentication/per-user game and scoreboard isolation.
- Production build/deploy configuration: a real reverse-proxy or same-origin hosting setup
  to replace the dev-only `ng serve` proxy.
- Configurable computer difficulty (the current heuristic is fixed and unbeatable-or-draw).

## 13. Computer's Winning Priority

1.	If O can win, play the winning move 
2.	If X can win next, block X 
3.	Take center if available 
4.	Take a corner if available 
5.	Take any available cell 
