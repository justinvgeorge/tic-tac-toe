import { Player } from './player.enum';

export interface MoveDto {
  moveNumber: number;
  player: Player;
  row: number;
  column: number;
}
