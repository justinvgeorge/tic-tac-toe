# Frontend Spec — Tic Tac Toe

Based on `docs/requirements.md` and the backend API contract (`GamesController`, `ScoreboardController`, `DTOs/`). Angular, standalone components, no NgModules. API base URL: `https://localhost:7221/api`.

## Assumptions

Requirements don't define a "start a new game" flow distinct from Reset Game (which keeps the same game id and mode). Since `Mode` can only be set at creation (`POST /api/games`) and is never changed by any other endpoint, this spec treats mode selection as a one-time step per game: `GameModeSelectorComponent` is shown before a game exists, and is also the mechanism for starting a fresh game (with the same or a different mode) later, distinct from Reset Game. This should be confirmed before implementation.

## Component tree

```
AppComponent
├── GameModeSelectorComponent      (shown when no active game; also usable to start a new game)
└── GameBoardContainerComponent    (shown once a game exists)
    ├── GameStatusComponent        (current player / winner / draw message)
    ├── BoardComponent
    │   └── CellComponent          (×9)
    ├── GameControlsComponent      (Reset Game, Undo Last Move)
    ├── MoveHistoryComponent
    └── ScoreboardComponent        (X wins / O wins / draws, Reset Scoreboard)
```

## Component responsibilities

- **AppComponent** — root shell. Renders `GameModeSelectorComponent` or `GameBoardContainerComponent` depending on whether `GameStateService` currently holds a `GameStateDto` (null = no game yet).
- **GameModeSelectorComponent** — lets the user pick `GameMode` (`TwoPlayer` / `VsComputer`) and calls `GameStateService.createGame(mode)`. Holds no game state itself.
- **GameBoardContainerComponent** — layout container for the active-game view once `GameStateDto` exists. Reads the shared state and passes relevant slices down to its children as inputs.
- **GameStatusComponent** — displays whose turn it is (`CurrentPlayer`), the selected `Mode`, and the end-of-game message (`Winner` / draw) derived from `Status` and `Winner`.
- **BoardComponent** — renders the 3×3 `Board` grid from `GameStateDto.Board`, highlights `WinningCells` when `Status === Won`, and forwards cell clicks up (as row/column) to trigger a move.
- **CellComponent** — renders a single cell's value (`X` / `O` / empty). Clickable only when its value is empty and `Status === InProgress`; emits its (row, column) on click.
- **GameControlsComponent** — "Reset Game" button (calls `GameStateService.resetGame()`) and "Undo Last Move" button (calls `GameStateService.undo()`), enabled/disabled per the rules in the Undo flow below.
- **MoveHistoryComponent** — renders `GameStateDto.MoveHistory` (move number, player, row/column) in order.
- **ScoreboardComponent** — renders `GameStateDto.Scoreboard` (X wins / O wins / draws) and the "Reset Scoreboard" button (calls `GameStateService.resetScoreboard()`).

No component holds its own copy of board, turn, history, or scoreboard data — all of it is read from the one shared `GameStateDto` in `GameStateService`.

## State management

- **`GameApiService`** — thin HTTP wrapper, one method per backend endpoint (`createGame`, `getGame`, `makeMove`, `undo`, `resetGame`, `getScoreboard`, `resetScoreboard`). Returns the raw DTOs from the API contract. Holds no state and does no client-side game logic.
- **`GameStateService`** — the single source of truth. Holds one piece of state: `GameStateDto | null` (exposed as an observable or signal). Every state-changing action is a method on this service that calls `GameApiService` and then replaces the stored `GameStateDto` wholesale with the response — never patched or merged locally, since the frontend never computes moves, wins, or history itself.
  - `createGame`, `makeMove`, `undo`, and `resetGame` all return a full `GameStateDto` (including the embedded `Scoreboard`), so the service simply overwrites its state with the response.
  - `resetScoreboard` is the one exception: `POST /api/scoreboard/reset` returns a bare `ScoreboardDto`, not a `GameStateDto`. The service merges this into the `Scoreboard` field of the current `GameStateDto` to preserve the single-state model; board/history/status/turn are left untouched.
- All components render directly from `GameStateService`'s current state — they subscribe/read, they don't fetch or hold their own copies.

## User flows

### 1. Select mode & create game
1. On load (or when starting a new game), `GameStateService` state is `null`; `GameModeSelectorComponent` is shown.
2. User picks `TwoPlayer` or `VsComputer`.
3. `GameStateService.createGame(mode)` → `POST /api/games` with `{ Mode }`.
4. Response `GameStateDto` becomes the new state (empty board, `CurrentPlayer: X`, `Status: InProgress`, empty `MoveHistory`, current session `Scoreboard`).
5. `GameBoardContainerComponent` and its children now render.

### 2. Make a move
1. User clicks an empty `CellComponent` (only clickable while `Status === InProgress` and the cell is unoccupied).
2. `BoardComponent` emits `(row, column)`; the container calls `GameStateService.makeMove(row, column)`, sending `Player: state.CurrentPlayer` (the frontend always plays the state's current player — it never tracks whose turn it is independently).
3. `POST /api/games/{id}/moves` with `{ Player, Row, Column }`.
4. Response `GameStateDto` replaces state. In `VsComputer` mode, the backend has already applied the computer's reply move (if the game is still in progress) in this same response — the frontend does not make a second request or special-case the computer's turn.
5. If `Status` is now `Won`, `GameStatusComponent` shows the winner and `BoardComponent` highlights `WinningCells`; all cells become non-interactive. If `Draw`, the draw message is shown.
6. On a 400/404 error response, surface the returned `message` to the user (e.g. as an inline error) without mutating state.

### 3. Undo last move
1. "Undo Last Move" in `GameControlsComponent` is enabled only when `MoveHistory.length > 0` and `Status === InProgress`; otherwise disabled (per requirements: no moves to undo, or game already completed).
2. Click calls `GameStateService.undo()` → `POST /api/games/{id}/undo` (no body).
3. Response `GameStateDto` replaces state. In `VsComputer` mode the backend has already popped both the computer's move and the preceding human move in this one response, so control always returns to the human — the frontend does not special-case this.

### 4. Reset game
1. "Reset Game" in `GameControlsComponent` calls `GameStateService.resetGame()` → `POST /api/games/{id}/reset` (no body).
2. Response `GameStateDto` replaces state: empty board, empty `MoveHistory`, `Status: InProgress`, `CurrentPlayer: X`, same `Mode` and `GameId` as before, `Scoreboard` unchanged.

### 5. Reset scoreboard
1. "Reset Scoreboard" in `ScoreboardComponent` calls `GameStateService.resetScoreboard()` → `POST /api/scoreboard/reset` (no body).
2. Response `ScoreboardDto` (`XWins: 0, OWins: 0, Draws: 0`) is merged into the current `GameStateDto.Scoreboard`; board, history, and turn are unaffected.
