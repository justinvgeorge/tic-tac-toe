import { Component, inject, input, output } from '@angular/core';
import { ScoreboardDto } from '../../models/scoreboard.model';
import { GameService } from '../../services/game.service';

@Component({
  selector: 'app-scoreboard',
  imports: [],
  templateUrl: './scoreboard.html',
  styleUrl: './scoreboard.css',
})
export class Scoreboard {
  private readonly gameService = inject(GameService);

  readonly scoreboard = input.required<ScoreboardDto>();

  readonly reset = output<ScoreboardDto>();

  protected onResetClick(): void {
    const confirmed = confirm('Reset scoreboard? This will clear X wins, O wins, and draws.');
    if (!confirmed) {
      return;
    }

    this.gameService.resetScoreboard().subscribe({
      next: (scoreboard) => this.reset.emit(scoreboard),
      error: (err) => console.error('Failed to reset scoreboard', err),
    });
  }
}
