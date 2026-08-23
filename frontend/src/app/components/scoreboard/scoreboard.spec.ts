import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { Scoreboard } from './scoreboard';

describe('Scoreboard', () => {
  let component: Scoreboard;
  let fixture: ComponentFixture<Scoreboard>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Scoreboard],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
    .compileComponents();

    fixture = TestBed.createComponent(Scoreboard);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('scoreboard', { xWins: 0, oWins: 0, draws: 0 });
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('calls resetScoreboard and emits the result when the user confirms', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    let emitted: unknown;
    component.reset.subscribe((scoreboard) => (emitted = scoreboard));

    component['onResetClick']();

    expect(window.confirm).toHaveBeenCalledWith(
      'Reset scoreboard? This will clear X wins, O wins, and draws.',
    );
    httpMock.expectOne('/api/scoreboard/reset').flush({ xWins: 0, oWins: 0, draws: 0 });
    expect(emitted).toEqual({ xWins: 0, oWins: 0, draws: 0 });
  });

  it('does not call resetScoreboard when the user cancels', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component['onResetClick']();

    expect(window.confirm).toHaveBeenCalled();
    httpMock.expectNone('/api/scoreboard/reset');
  });
});
