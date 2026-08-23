import { Component, output } from '@angular/core';
import { GameMode } from '../../models/game-mode.enum';

@Component({
  selector: 'app-mode-selector',
  imports: [],
  templateUrl: './mode-selector.html',
  styleUrl: './mode-selector.css',
})
export class ModeSelector {
  readonly modeSelected = output<GameMode>();

  protected readonly GameMode = GameMode;

  protected selectMode(mode: GameMode): void {
    this.modeSelected.emit(mode);
  }
}
