import { Player } from './player.enum';

export interface MoveRequestDto {
  player: Player;
  row: number;
  column: number;
}
