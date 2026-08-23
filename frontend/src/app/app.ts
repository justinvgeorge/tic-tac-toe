import { Component } from '@angular/core';
import { GameContainer } from './components/game-container/game-container';

@Component({
  selector: 'app-root',
  imports: [GameContainer],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
