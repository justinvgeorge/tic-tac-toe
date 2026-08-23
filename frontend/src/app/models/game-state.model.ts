import { Player } from './player.enum';
import { GameMode } from './game-mode.enum';
import { GameStatus } from './game-status.enum';
import { MoveDto } from './move.model';
import { ScoreboardDto } from './scoreboard.model';

export interface GameStateDto {
  gameId: string;
  board: (Player | null)[][];
  currentPlayer: Player;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: number[][] | null;
  moveHistory: MoveDto[];
  scoreboard: ScoreboardDto;
}
