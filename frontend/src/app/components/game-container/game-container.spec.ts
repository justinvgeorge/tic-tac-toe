import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { GameContainer, COMPUTER_MOVE_DELAY_MS } from './game-container';
import { GameStateDto } from '../../models/game-state.model';
import { GameStatus } from '../../models/game-status.enum';
import { GameMode } from '../../models/game-mode.enum';
import { Player } from '../../models/player.enum';

const GAME_ID = 'game-1';

function inProgressState(overrides: Partial<GameStateDto> = {}): GameStateDto {
  return {
    gameId: GAME_ID,
    board: [
      [null, null, null],
      [null, null, null],
      [null, null, null],
    ],
    currentPlayer: Player.X,
    mode: GameMode.TwoPlayer,
    status: GameStatus.InProgress,
    winner: null,
    winningCells: null,
    moveHistory: [],
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}

function wonState(): GameStateDto {
  return inProgressState({
    status: GameStatus.Won,
    winner: Player.X,
    winningCells: [[0, 0], [0, 1], [0, 2]],
  });
}

function vsComputerMoveResponse(): GameStateDto {
  return inProgressState({
    mode: GameMode.VsComputer,
    board: [
      [Player.X, null, null],
      [null, Player.O, null],
      [null, null, null],
    ],
    moveHistory: [
      { moveNumber: 1, player: Player.X, row: 0, column: 0 },
      { moveNumber: 2, player: Player.O, row: 1, column: 1 },
    ],
  });
}

describe('GameContainer', () => {
  let component: GameContainer;
  let fixture: ComponentFixture<GameContainer>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GameContainer],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
    .compileComponents();

    fixture = TestBed.createComponent(GameContainer);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  function createGame(): void {
    component['onModeSelected'](GameMode.TwoPlayer);
    httpMock.expectOne('/api/games').flush(inProgressState());
  }

  function winGame(): void {
    component['onCellClick']({ row: 0, column: 0 });
    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(wonState());
  }

  function createVsComputerGame(): void {
    component['onModeSelected'](GameMode.VsComputer);
    httpMock.expectOne('/api/games').flush(inProgressState({ mode: GameMode.VsComputer }));
  }

  it('starts a 5s countdown and calls Reset Game automatically when it elapses', fakeAsync(() => {
    createGame();
    winGame();

    expect(component['autoResetSecondsRemaining']()).toBe(5);

    tick(4000);
    expect(component['autoResetSecondsRemaining']()).toBe(1);
    httpMock.expectNone(`/api/games/${GAME_ID}/reset`);

    tick(1000);
    const resetReq = httpMock.expectOne(`/api/games/${GAME_ID}/reset`);
    resetReq.flush(inProgressState());

    expect(component['autoResetSecondsRemaining']()).toBeNull();
  }));

  it('cancels the pending auto-reset when Reset Game is clicked manually', fakeAsync(() => {
    createGame();
    winGame();

    tick(2000);
    component['onResetGameClick']();
    httpMock.expectOne(`/api/games/${GAME_ID}/reset`).flush(inProgressState());

    expect(component['autoResetSecondsRemaining']()).toBeNull();

    // Advance well past the original 5s window; no second reset call should fire.
    tick(10000);
    httpMock.expectNone(`/api/games/${GAME_ID}/reset`);
  }));

  it('cancels the pending auto-reset when Change Mode is clicked', fakeAsync(() => {
    createGame();
    winGame();

    tick(2000);
    component['onChangeModeClick'](); // status is Won, no confirm() prompt

    expect(component['autoResetSecondsRemaining']()).toBeNull();
    expect(component['changingMode']()).toBeTrue();

    tick(10000);
    httpMock.expectNone(`/api/games/${GAME_ID}/reset`);
  }));

  it('clears the timer on destroy so it never fires afterward', fakeAsync(() => {
    createGame();
    winGame();

    tick(2000);
    fixture.destroy();
    expect(component['autoResetSecondsRemaining']()).toBeNull();

    tick(10000);
    httpMock.expectNone(`/api/games/${GAME_ID}/reset`);
  }));

  it('shows an optimistic mark immediately on click, before any response arrives', fakeAsync(() => {
    createVsComputerGame();

    component['onCellClick']({ row: 0, column: 0 });
    expect(component['optimisticMark']()).toEqual({ row: 0, column: 0, player: Player.X });

    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(vsComputerMoveResponse());
    tick(COMPUTER_MOVE_DELAY_MS);
  }));

  it('holds back the move response in VsComputer mode for 800ms before applying it', fakeAsync(() => {
    createVsComputerGame();

    component['onCellClick']({ row: 0, column: 0 });
    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(vsComputerMoveResponse());

    // Response has arrived but must not be applied yet.
    expect(component['pendingComputerMove']()).toBeTrue();
    expect(component['gameState']()!.board[1][1]).toBeNull();
    expect(component['optimisticMark']()).toEqual({ row: 0, column: 0, player: Player.X });

    tick(COMPUTER_MOVE_DELAY_MS - 1);
    expect(component['pendingComputerMove']()).toBeTrue();

    tick(1);
    expect(component['pendingComputerMove']()).toBeFalse();
    expect(component['optimisticMark']()).toBeNull();
    expect(component['gameState']()!.board[0][0]).toBe(Player.X);
    expect(component['gameState']()!.board[1][1]).toBe(Player.O);
  }));

  it('applies the move response immediately in Two Player mode (no delay)', fakeAsync(() => {
    createGame();

    component['onCellClick']({ row: 0, column: 0 });
    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(
      inProgressState({
        board: [
          [Player.X, null, null],
          [null, null, null],
          [null, null, null],
        ],
        currentPlayer: Player.O,
        moveHistory: [{ moveNumber: 1, player: Player.X, row: 0, column: 0 }],
      }),
    );

    expect(component['pendingComputerMove']()).toBeFalse();
    expect(component['optimisticMark']()).toBeNull();
    expect(component['gameState']()!.board[0][0]).toBe(Player.X);
  }));

  it('ignores further clicks while a computer-move reveal is pending', fakeAsync(() => {
    createVsComputerGame();

    component['onCellClick']({ row: 0, column: 0 });
    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(vsComputerMoveResponse());
    expect(component['pendingComputerMove']()).toBeTrue();

    component['onCellClick']({ row: 2, column: 2 });
    httpMock.expectNone(`/api/games/${GAME_ID}/moves`);

    tick(COMPUTER_MOVE_DELAY_MS);
  }));

  it('clears a pending computer-move reveal on destroy so it never applies late', fakeAsync(() => {
    createVsComputerGame();

    component['onCellClick']({ row: 0, column: 0 });
    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(vsComputerMoveResponse());

    tick(200);
    fixture.destroy();
    expect(component['pendingComputerMove']()).toBeFalse();
    expect(component['optimisticMark']()).toBeNull();

    tick(COMPUTER_MOVE_DELAY_MS);
  }));

  it('cancels a pending computer-move reveal if Reset Game runs while it is pending', fakeAsync(() => {
    createVsComputerGame();

    component['onCellClick']({ row: 0, column: 0 });
    httpMock.expectOne(`/api/games/${GAME_ID}/moves`).flush(vsComputerMoveResponse());

    tick(200);
    component['onResetGameClick']();
    httpMock
      .expectOne(`/api/games/${GAME_ID}/reset`)
      .flush(inProgressState({ mode: GameMode.VsComputer }));

    expect(component['pendingComputerMove']()).toBeFalse();
    expect(component['gameState']()!.board[0][0]).toBeNull();

    // The stale reveal must not fire and clobber the reset state.
    tick(COMPUTER_MOVE_DELAY_MS);
    expect(component['gameState']()!.board[0][0]).toBeNull();
    expect(component['gameState']()!.board[1][1]).toBeNull();
  }));
});
