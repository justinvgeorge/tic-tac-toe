import { Component, input } from '@angular/core';
import { MoveDto } from '../../models/move.model';
import { Player } from '../../models/player.enum';

@Component({
  selector: 'app-move-history',
  imports: [],
  templateUrl: './move-history.html',
  styleUrl: './move-history.css',
})
export class MoveHistory {
  readonly moveHistory = input.required<MoveDto[]>();

  protected readonly Player = Player;
}
