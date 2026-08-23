## Requirements

- Standard 3x3 board, cells clickable when empty, locked once played
- Two players (X and O), alternating turns, clearly displayed whose turn it is
- Win detection: row, column, or diagonal — show winner, highlight winning cells, prevent 
  further moves, update scoreboard
- Draw detection when board fills with no winner
- Reset Game: clears board/history/status, sets current player to X, keeps scoreboard unchanged
- Move history shown for the current game: move number, player, cell position
- Undo Last Move: in Two Player Mode removes only the last move; in Computer Mode removes 
  the computer's move AND the human's previous move together. Disabled when no moves to undo.
- Session-level scoreboard: X wins, O wins, draws — updates once per completed game, served 
  by the backend, has its own Reset Scoreboard option separate from Reset Game
- Two modes: Two Player Mode, and Play Against Computer (human is always X, computer is 
  always O, computer moves automatically after the human, following priority: win if possible 
  → block if X can win next → center → corner → any available cell)
- Undo is disabled after game completion (this is the option we've chosen — see 
  Clarification 2 in the full doc)
- Frontend must show: board, current player, selected mode, winner/draw message, highlighted 
  winning cells, move history, scoreboard, and Reset Game / Undo Last Move / Reset Scoreboard 
  buttons — calling the backend for every action and rendering only what the backend returns