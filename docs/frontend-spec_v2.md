# Frontend Spec v2 — Tic Tac Toe

Based on `docs/requirements_v2.md` and the backend API contract (`GamesController`, `ScoreboardController`, `DTOs/`). Angular, standalone components, no NgModules. API base URL: `https://localhost:7221/api`.

Supersedes `docs/frontend-spec.md`. That draft left one open assumption — whether switching modes reuses Reset Game or is a separate action. `requirements_v2.md` resolves it explicitly: **Reset Game** keeps the same game id and mode; switching modes creates a fresh game id via a new `POST /api/games` call. These are two distinct actions, both reflected below. This mode-switching action is implemented as the **"Change Mode"** button (see flow 1a) — an earlier draft of this doc called it "New Game"; the name changed, the behavior didn't.

## Component tree

```
AppComponent
├── GameModeSelectorComponent      (shown when no active game, and re-openable via "Change Mode")
└── GameBoardContainerComponent    (shown once a game exists)
    ├── GameStatusComponent        (current player / mode / winner / draw message)
    ├── BoardComponent
    │   └── CellComponent          (×9)
    ├── GameControlsComponent      (Change Mode, Reset Game, Undo Last Move)
    ├── MoveHistoryComponent
    └── ScoreboardComponent        (X wins / O wins / draws, Reset Scoreboard)
```

## Component responsibilities

- **AppComponent** — root shell. Renders `GameModeSelectorComponent` or `GameBoardContainerComponent` depending on whether `GameStateService` currently holds a `GameStateDto` (null = no game yet).
- **GameModeSelectorComponent** — lets the user pick `GameMode` (`TwoPlayer` / `VsComputer`) and calls `GameStateService.createGame(mode)`. Used both on first load and whenever "Change Mode" is triggered from `GameControlsComponent`. Holds no game state itself.
- **GameBoardContainerComponent** — layout container for the active-game view once `GameStateDto` exists. Reads the shared state and passes relevant slices down to its children as inputs.
- **GameStatusComponent** — displays whose turn it is (`CurrentPlayer`), the game's fixed `Mode`, and the end-of-game message (`Winner` / draw) derived from `Status` and `Winner`.
- **BoardComponent** — renders the 3×3 `Board` grid from `GameStateDto.Board`, highlights `WinningCells` when `Status === Won`, and forwards cell clicks up (as row/column) to trigger a move.
- **CellComponent** — renders a single cell's value (`X` / `O` / empty). Clickable only when its value is empty and `Status === InProgress`; emits its (row, column) on click.
- **GameControlsComponent** — three actions:
  - "Change Mode" — visible whenever a game exists, regardless of `Status`. Returns to `GameModeSelectorComponent` to pick a mode and start a fresh game id (`Mode` is immutable for an existing game's lifetime, so switching modes can't go through Reset). If `Status === InProgress`, a confirmation prompt is shown first, since the in-progress game is discarded; no prompt is needed once the game is `Won` or `Draw`, since there is nothing in-progress to lose.
  - "Reset Game" — calls `GameStateService.resetGame()`, reusing the current game id and mode.
  - "Undo Last Move" — calls `GameStateService.undo()`; enabled/disabled per the rules in the Undo flow below.
- **MoveHistoryComponent** — renders `GameStateDto.MoveHistory` (move number, player, row/column) in order.
- **ScoreboardComponent** — renders `GameStateDto.Scoreboard` (X wins / O wins / draws) and the "Reset Scoreboard" button. Clicking it shows a confirmation prompt first (same pattern as "Change Mode"'s in-progress confirmation — a native `confirm()` dialog, not a separate modal component); only on confirming does it call `GameStateService.resetScoreboard()`. Cancelling makes no API call and leaves the scoreboard untouched.

No component holds its own copy of board, turn, history, or scoreboard data — all of it is read from the one shared `GameStateDto` in `GameStateService`.

## State management

- **`GameApiService`** — thin HTTP wrapper, one method per backend endpoint (`createGame`, `getGame`, `makeMove`, `undo`, `resetGame`, `getScoreboard`, `resetScoreboard`). Returns the raw DTOs from the API contract. Holds no state and does no client-side game logic.
- **`GameStateService`** — the single source of truth. Holds one piece of state: `GameStateDto | null` (exposed as an observable or signal). Every state-changing action is a method on this service that calls `GameApiService` and then replaces the stored `GameStateDto` wholesale with the response — never patched or merged locally, since the frontend never computes moves, wins, or history itself.
  - `createGame`, `makeMove`, `undo`, and `resetGame` all return a full `GameStateDto` (including the embedded `Scoreboard`), so the service simply overwrites its state with the response. `createGame` is used both for the very first game and for every subsequent "Change Mode" — each call produces a new `GameId`, replacing whatever game (if any) was previously in state. Because `Scoreboard` is server-side global state (not tied to a game id), the backend's `CreateGame` handler never touches it — the `Scoreboard` embedded in the response is simply the current persisted value, so "Change Mode" naturally leaves `XWins`/`OWins`/`Draws` unaffected without any special-casing on the frontend.
  - `resetScoreboard` is the one exception: `POST /api/scoreboard/reset` returns a bare `ScoreboardDto`, not a `GameStateDto`. The service merges this into the `Scoreboard` field of the current `GameStateDto` to preserve the single-state model; board/history/status/turn are left untouched.
- All components render directly from `GameStateService`'s current state — they subscribe/read, they don't fetch or hold their own copies.

## User flows

### 1. Select mode & create game (first load, or "Change Mode")
1. State starts `null` on first load; `GameModeSelectorComponent` is shown.
2. User picks `TwoPlayer` or `VsComputer`.
3. `GameStateService.createGame(mode)` → `POST /api/games` with `{ Mode }`.
4. Response `GameStateDto` becomes the new state (fresh `GameId`, empty board, `CurrentPlayer: X`, `Status: InProgress`, empty `MoveHistory`, current session `Scoreboard` — unchanged from whatever it was, since `Scoreboard` is server-side global state that `POST /api/games` never touches) — replacing any prior game's state entirely.
5. `GameBoardContainerComponent` and its children render (or re-render) from the new state.

### 1a. Change mode (mid-session)
1. "Change Mode" in `GameControlsComponent` is visible whenever a game exists, in any `Status` (`InProgress`, `Won`, or `Draw`).
2. If `Status === InProgress`: show a confirmation prompt before proceeding, since the in-progress board/history will be discarded. If the user cancels, nothing changes — the current game stays exactly as it was. If `Status` is `Won` or `Draw`, skip the prompt entirely (there's no in-progress state to lose) — in that case the pending auto-reset countdown (flow 2, step 7) is cancelled instead.
3. Once confirmed (or skipped), hide the active-game view and show `GameModeSelectorComponent` again — same component, same behavior as the first-load case.
4. From here, flow is identical to flow 1 above: user picks a mode, `createGame` is called, and the response (fresh `GameId`, unchanged `Scoreboard`) replaces state entirely. This is a brand new game session, not a reset — it reuses no data from the discarded game, and never calls `POST /api/games/{id}/reset`.
5. `Scoreboard` (`XWins`/`OWins`/`Draws`) is unaffected throughout — Change Mode is not Reset Scoreboard and never calls `POST /api/scoreboard/reset`.

### 2. Make a move
1. User clicks an empty `CellComponent` (only clickable while `Status === InProgress` and the cell is unoccupied).
2. `BoardComponent` emits `(row, column)`; the container calls `GameStateService.makeMove(row, column)`, sending `Player: state.CurrentPlayer` (the frontend always plays the state's current player — it never tracks whose turn it is independently).
3. `POST /api/games/{id}/moves` with `{ Player, Row, Column }`.
4. Response `GameStateDto` replaces state. In `VsComputer` mode, the backend has already applied the computer's reply move (if the game is still in progress) in this same response — the frontend does not make a second request or special-case the computer's turn.
5. If `Status` is now `Won`, `GameStatusComponent` shows the winner and `BoardComponent` highlights `WinningCells`; all cells become non-interactive. If `Draw`, the draw message is shown.
6. On a 400/404 error response, surface the returned `message` to the user (e.g. as an inline error) without mutating state.
7. **Auto-reset on Won/Draw**: the moment `Status` transitions to `Won` or `Draw` (i.e. only right after this move response — `createGame`/`resetGame`/`undo` always return `Status: InProgress`, so they can't trigger this), a 5-second countdown starts. A "Next game starting in Xs..." indicator is shown next to the winner/draw message for its duration. When it reaches zero, `GameStateService.resetGame()` is called automatically — `POST /api/games/{id}/reset` (no body), same endpoint as flow 4, same game id and mode, scoreboard untouched. This is a Reset, not a Change Mode: the mode carries over. If the user manually triggers Undo (flow 3), Reset Game (flow 4), or Change Mode (flow 1a) before the countdown reaches zero, the pending timer is cancelled immediately so it can't also fire on top of that manual action; the countdown indicator disappears. The timer is also cleared if the component is destroyed, so it can never fire against a component that's gone.

### 3. Undo last move
1. "Undo Last Move" in `GameControlsComponent` is enabled only when `MoveHistory.length > 0` and `Status === InProgress`; otherwise disabled (per requirements: no moves to undo, or game already completed). Since the auto-reset countdown (flow 2, step 7) only ever runs while `Status` is `Won`/`Draw`, Undo is always disabled whenever that countdown is visible — clicking it to cancel the countdown isn't a reachable interaction in practice, but the cancellation is still wired defensively.
2. Click calls `GameStateService.undo()` → `POST /api/games/{id}/undo` (no body).
3. Response `GameStateDto` replaces state. In `VsComputer` mode the backend has already popped both the computer's move and the preceding human move in this one response, so control always returns to the human — the frontend does not special-case this.

### 4. Reset game
1. "Reset Game" in `GameControlsComponent` calls `GameStateService.resetGame()` → `POST /api/games/{id}/reset` (no body).
2. Response `GameStateDto` replaces state: empty board, empty `MoveHistory`, `Status: InProgress`, `CurrentPlayer: X`, **same `GameId` and `Mode` as before**, `Scoreboard` unchanged.
3. Distinct from "Change Mode" (flow 1a): Reset Game never changes `GameId` or `Mode` and never re-opens `GameModeSelectorComponent`.
4. If the auto-reset countdown (flow 2, step 7) is running when this is clicked manually, it is cancelled first so it can't also fire afterward — the manual reset and the auto-reset call the same endpoint, so there's nothing left to auto-reset once this completes.

### 5. Reset scoreboard
1. "Reset Scoreboard" in `ScoreboardComponent` requires confirmation first: a `confirm()` prompt (e.g. "Reset scoreboard? This will clear X wins, O wins, and draws.") — the same confirmation mechanism used by "Change Mode" (flow 1a) for an in-progress game, not a separate UI pattern.
2. If the user cancels, nothing happens: no API call, scoreboard stays exactly as it was.
3. Only on confirming does it call `GameStateService.resetScoreboard()` → `POST /api/scoreboard/reset` (no body).
4. Response `ScoreboardDto` (`XWins: 0, OWins: 0, Draws: 0`) is merged into the current `GameStateDto.Scoreboard`; board, history, and turn are unaffected.
