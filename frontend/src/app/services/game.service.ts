import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateGameRequestDto,
  GameStateDto,
  MoveRequestDto,
  ScoreboardDto,
} from '../models';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);

  private readonly gamesUrl = '/api/games';
  private readonly scoreboardUrl = '/api/scoreboard';

  createGame(request: CreateGameRequestDto): Observable<GameStateDto> {
    return this.http.post<GameStateDto>(this.gamesUrl, request);
  }

  getGame(gameId: string): Observable<GameStateDto> {
    return this.http.get<GameStateDto>(`${this.gamesUrl}/${gameId}`);
  }

  makeMove(gameId: string, request: MoveRequestDto): Observable<GameStateDto> {
    return this.http.post<GameStateDto>(`${this.gamesUrl}/${gameId}/moves`, request);
  }

  undoMove(gameId: string): Observable<GameStateDto> {
    return this.http.post<GameStateDto>(`${this.gamesUrl}/${gameId}/undo`, {});
  }

  resetGame(gameId: string): Observable<GameStateDto> {
    return this.http.post<GameStateDto>(`${this.gamesUrl}/${gameId}/reset`, {});
  }

  getScoreboard(): Observable<ScoreboardDto> {
    return this.http.get<ScoreboardDto>(this.scoreboardUrl);
  }

  resetScoreboard(): Observable<ScoreboardDto> {
    return this.http.post<ScoreboardDto>(`${this.scoreboardUrl}/reset`, {});
  }
}
