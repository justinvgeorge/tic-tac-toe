import { Component, computed, input, output } from '@angular/core';
import { Player } from '../../models/player.enum';
import { GameStatus } from '../../models/game-status.enum';

const CELL_SIZE = 84;
const CELL_GAP = 6;
const GRID_PADDING = 6;
const GRID_SIZE = GRID_PADDING * 2 + CELL_SIZE * 3 + CELL_GAP * 2;

function cellCenter(index: number): number {
  return GRID_PADDING + index * (CELL_SIZE + CELL_GAP) + CELL_SIZE / 2;
}

export interface OptimisticMark {
  row: number;
  column: number;
  player: Player;
}

@Component({
  selector: 'app-game-board',
  imports: [],
  templateUrl: './game-board.html',
  styleUrl: './game-board.css',
})
export class GameBoard {
  readonly board = input.required<(Player | null)[][]>();
  readonly status = input.required<GameStatus>();
  readonly winningCells = input<number[][] | null>(null);
  /** A locally-rendered mark shown before the server response for that cell has arrived. */
  readonly optimisticMark = input<OptimisticMark | null>(null);
  /** Blocks all cell interaction, e.g. while a computer-move reveal is pending. */
  readonly boardDisabled = input(false);

  readonly cellClick = output<{ row: number; column: number }>();

  protected readonly Player = Player;
  protected readonly gridSize = GRID_SIZE;

  protected readonly winLine = computed(() => {
    const cells = this.winningCells();
    if (!cells || cells.length < 2) {
      return null;
    }
    const [r1, c1] = cells[0];
    const [r2, c2] = cells[cells.length - 1];
    return {
      x1: cellCenter(c1),
      y1: cellCenter(r1),
      x2: cellCenter(c2),
      y2: cellCenter(r2),
    };
  });

  protected isWinningCell(row: number, column: number): boolean {
    const cells = this.winningCells();
    return !!cells && cells.some(([r, c]) => r === row && c === column);
  }

  protected isClickable(row: number, column: number): boolean {
    return (
      !this.boardDisabled() &&
      this.status() === GameStatus.InProgress &&
      this.board()[row][column] === null
    );
  }

  /** Real board value takes priority; falls back to the optimistic mark for
   * a cell that's still null server-side but was just clicked locally. */
  protected displayValue(row: number, column: number): Player | null {
    const real = this.board()[row][column];
    if (real !== null) {
      return real;
    }
    const optimistic = this.optimisticMark();
    if (optimistic && optimistic.row === row && optimistic.column === column) {
      return optimistic.player;
    }
    return null;
  }

  protected onCellClick(row: number, column: number): void {
    if (!this.isClickable(row, column)) {
      return;
    }
    this.cellClick.emit({ row, column });
  }
}
