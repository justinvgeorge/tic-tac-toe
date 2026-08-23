import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GameBoard } from './game-board';
import { GameStatus } from '../../models/game-status.enum';

describe('GameBoard', () => {
  let component: GameBoard;
  let fixture: ComponentFixture<GameBoard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GameBoard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GameBoard);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('board', [
      [null, null, null],
      [null, null, null],
      [null, null, null],
    ]);
    fixture.componentRef.setInput('status', GameStatus.InProgress);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
