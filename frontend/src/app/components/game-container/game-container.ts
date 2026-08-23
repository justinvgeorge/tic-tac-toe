import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { GameBoard, OptimisticMark } from '../game-board/game-board';
import { ModeSelector } from '../mode-selector/mode-selector';
import { Scoreboard } from '../scoreboard/scoreboard';
import { MoveHistory } from '../move-history/move-history';
import { GameService } from '../../services/game.service';
import { GameStateDto } from '../../models/game-state.model';
import { GameMode } from '../../models/game-mode.enum';
import { GameStatus } from '../../models/game-status.enum';
import { ScoreboardDto } from '../../models/scoreboard.model';
import { Player } from '../../models/player.enum';

const AUTO_RESET_SECONDS = 5;
export const COMPUTER_MOVE_DELAY_MS = 200;

@Component({
  selector: 'app-game-container',
  imports: [GameBoard, ModeSelector, Scoreboard, MoveHistory],
  templateUrl: './game-container.html',
  styleUrl: './game-container.css',
})
export class GameContainer implements OnDestroy {
  private readonly gameService = inject(GameService);

  protected readonly gameState = signal<GameStateDto | null>(null);
  protected readonly changingMode = signal(false);
  protected readonly autoResetSecondsRemaining = signal<number | null>(null);
  protected readonly optimisticMark = signal<OptimisticMark | null>(null);
  protected readonly pendingComputerMove = signal(false);

  private autoResetIntervalId: ReturnType<typeof setInterval> | null = null;
  private computerMoveTimeoutId: ReturnType<typeof setTimeout> | null = null;

  protected readonly canUndo = computed(() => {
    const state = this.gameState();
    return !!state && state.moveHistory.length > 0 && state.status === GameStatus.InProgress;
  });

  protected readonly statusMessage = computed(() => {
    const state = this.gameState();
    if (!state) {
      return null;
    }
    if (state.status === GameStatus.Won) {
      return `${state.winner === Player.X ? 'X' : 'O'} wins!`;
    }
    if (state.status === GameStatus.Draw) {
      return "It's a draw!";
    }
    return null;
  });

  ngOnDestroy(): void {
    this.cancelAutoResetTimer();
    this.clearComputerMoveTimer();
  }

  protected onModeSelected(mode: GameMode): void {
    this.cancelAutoResetTimer();
    this.clearComputerMoveTimer();
    this.gameService.createGame({ mode }).subscribe({
      next: (state) => {
        this.gameState.set(state);
        console.log('Game created:', state.gameId);
        this.changingMode.set(false);
      },
      error: (err) => this.logError('Failed to create game', err),
    });
  }

  protected onChangeModeClick(): void {
    const state = this.gameState();
    if (!state) {
      return;
    }

    if (state.status === GameStatus.InProgress) {
      const confirmed = confirm(
        'This game is still in progress. Changing mode will discard it. Continue?',
      );
      if (!confirmed) {
        return;
      }
    }

    this.cancelAutoResetTimer();
    this.clearComputerMoveTimer();
    this.changingMode.set(true);
  }

  protected onCellClick(event: { row: number; column: number }): void {
    const state = this.gameState();
    if (!state || this.pendingComputerMove()) {
      return;
    }

    // Purely a local visual mirror of the click for perceived responsiveness —
    // not game logic. It's overwritten the moment the real state is applied.
    this.optimisticMark.set({
      row: event.row,
      column: event.column,
      player: state.currentPlayer,
    });

    this.gameService
      .makeMove(state.gameId, {
        player: state.currentPlayer,
        row: event.row,
        column: event.column,
      })
      .subscribe({
        next: (newState) => this.handleMoveResponse(state.mode, newState),
        error: (err) => {
          this.optimisticMark.set(null);
          this.logError('Failed to make move', err);
        },
      });
  }

  protected onUndoClick(): void {
    const state = this.gameState();
    if (!state) {
      return;
    }

    this.cancelAutoResetTimer();
    this.clearComputerMoveTimer();
    this.gameService.undoMove(state.gameId).subscribe({
      next: (newState) => this.gameState.set(newState),
      error: (err) => this.logError('Failed to undo move', err),
    });
  }

  protected onResetGameClick(): void {
    const state = this.gameState();
    if (!state) {
      return;
    }

    this.cancelAutoResetTimer();
    this.clearComputerMoveTimer();
    this.gameService.resetGame(state.gameId).subscribe({
      next: (newState) => this.gameState.set(newState),
      error: (err) => this.logError('Failed to reset game', err),
    });
  }

  protected onScoreboardReset(scoreboard: ScoreboardDto): void {
    const state = this.gameState();
    if (!state) {
      return;
    }

    this.gameState.set({ ...state, scoreboard });
  }

  /** Sets game state and, on a fresh transition into Won/Draw, starts the auto-reset countdown. */
  private applyGameState(newState: GameStateDto): void {
    this.gameState.set(newState);

    if (newState.status !== GameStatus.InProgress) {
      this.startAutoResetTimer(newState.gameId);
    }
  }

  /** In VsComputer mode, holds back the (already-received) response for a
   * beat so the computer's move reads as its own deliberate turn rather
   * than appearing instantly alongside the human's. Two Player mode has no
   * computer move to pace out, so it applies immediately as before. */
  private handleMoveResponse(mode: GameMode, newState: GameStateDto): void {
    if (mode !== GameMode.VsComputer) {
      this.optimisticMark.set(null);
      this.applyGameState(newState);
      return;
    }

    this.pendingComputerMove.set(true);
    this.computerMoveTimeoutId = setTimeout(() => {
      this.computerMoveTimeoutId = null;
      this.pendingComputerMove.set(false);
      this.optimisticMark.set(null);
      this.applyGameState(newState);
    }, COMPUTER_MOVE_DELAY_MS);
  }

  private clearComputerMoveTimer(): void {
    if (this.computerMoveTimeoutId !== null) {
      clearTimeout(this.computerMoveTimeoutId);
      this.computerMoveTimeoutId = null;
    }
    this.pendingComputerMove.set(false);
    this.optimisticMark.set(null);
  }

  private startAutoResetTimer(gameId: string): void {
    this.cancelAutoResetTimer();
    this.autoResetSecondsRemaining.set(AUTO_RESET_SECONDS);

    this.autoResetIntervalId = setInterval(() => {
      const remaining = this.autoResetSecondsRemaining();
      const next = (remaining ?? 1) - 1;

      if (next <= 0) {
        this.cancelAutoResetTimer();
        this.autoReset(gameId);
      } else {
        this.autoResetSecondsRemaining.set(next);
      }
    }, 1000);
  }

  private cancelAutoResetTimer(): void {
    if (this.autoResetIntervalId !== null) {
      clearInterval(this.autoResetIntervalId);
      this.autoResetIntervalId = null;
    }
    this.autoResetSecondsRemaining.set(null);
  }

  private autoReset(gameId: string): void {
    this.gameService.resetGame(gameId).subscribe({
      next: (newState) => this.gameState.set(newState),
      error: (err) => this.logError('Failed to auto-reset game', err),
    });
  }

  private logError(message: string, err: unknown): void {
    console.error(message, err);
  }
}
