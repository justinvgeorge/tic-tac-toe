using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;
using TicTacToe.Api.Repositories.Interfaces;
using TicTacToe.Api.Services.Interfaces;

namespace TicTacToe.Api.Services
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IScoreboardService _scoreboardService;
        private readonly IComputerPlayerService _computerPlayerService;

        public GameService(
            IGameRepository gameRepository,
            IScoreboardService scoreboardService,
            IComputerPlayerService computerPlayerService)
        {
            _gameRepository = gameRepository;
            _scoreboardService = scoreboardService;
            _computerPlayerService = computerPlayerService;
        }

        public GameStateDto CreateGame(CreateGameRequestDto request)
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Board = new Player?[3, 3],
                CurrentPlayer = Player.X,
                Mode = request.Mode,
                Status = GameStatus.InProgress,
                MoveHistory = new List<Move>()
            };

            _gameRepository.Save(game);
            return MapToDto(game);
        }

        public GameStateDto GetGame(Guid id)
        {
            var game = _gameRepository.GetById(id)
                ?? throw new KeyNotFoundException($"Game {id} not found.");
            return MapToDto(game);
        }

        public GameStateDto MakeMove(Guid id, MoveRequestDto request)
        {
            var game = _gameRepository.GetById(id)
                ?? throw new KeyNotFoundException($"Game {id} not found.");

            ValidateMove(game, request);
            PlaceMove(game, request.Player, request.Row, request.Column);
            EvaluateGameEnd(game);

            if (game.Mode == GameMode.VsComputer
                && game.Status == GameStatus.InProgress
                && game.CurrentPlayer == Player.O)
            {
                var (row, col) = _computerPlayerService.GetNextMove(game.Board);
                PlaceMove(game, Player.O, row, col);
                EvaluateGameEnd(game);
            }

            _gameRepository.Save(game);
            return MapToDto(game);
        }

        public GameStateDto UndoMove(Guid id)
        {
            var game = _gameRepository.GetById(id)
                ?? throw new KeyNotFoundException($"Game {id} not found.");

            if (game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Cannot undo a completed game.");

            if (game.MoveHistory.Count == 0)
                throw new InvalidOperationException("No moves to undo.");

            int movesToPop = game.Mode == GameMode.VsComputer && game.MoveHistory.Count >= 2 ? 2 : 1;

            for (int i = 0; i < movesToPop; i++)
                game.MoveHistory.RemoveAt(game.MoveHistory.Count - 1);

            RebuildBoardFromHistory(game);

            _gameRepository.Save(game);
            return MapToDto(game);
        }

        public GameStateDto ResetGame(Guid id)
        {
            var game = _gameRepository.GetById(id)
                ?? throw new KeyNotFoundException($"Game {id} not found.");

            game.Board = new Player?[3, 3];
            game.MoveHistory = new List<Move>();
            game.Status = GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = null;
            game.CurrentPlayer = Player.X;

            _gameRepository.Save(game);
            return MapToDto(game);
        }

        private void ValidateMove(Game game, MoveRequestDto request)
        {
            if (game.Status != GameStatus.InProgress)
                throw new InvalidOperationException("Game is already finished.");

            if (request.Row < 0 || request.Row > 2 || request.Column < 0 || request.Column > 2)
                throw new ArgumentException("Move is out of bounds.");

            if (game.Board[request.Row, request.Column] != null)
                throw new InvalidOperationException("Cell is already occupied.");

            if (request.Player != game.CurrentPlayer)
                throw new InvalidOperationException("It is not this player's turn.");
        }

        private void PlaceMove(Game game, Player player, int row, int col)
        {
            game.Board[row, col] = player;

            game.MoveHistory.Add(new Move
            {
                MoveNumber = game.MoveHistory.Count + 1,
                Player = player,
                Row = row,
                Column = col
            });

            game.CurrentPlayer = player == Player.X ? Player.O : Player.X;
        }

        private void EvaluateGameEnd(Game game)
        {
            var (winner, winningCells) = GameRules.CheckWin(game.Board);

            if (winner != null)
            {
                game.Status = GameStatus.Won;
                game.Winner = winner;
                game.WinningCells = winningCells;
                _scoreboardService.RecordResult(GameStatus.Won, winner);
            }
            else if (GameRules.CheckDraw(game.Board))
            {
                game.Status = GameStatus.Draw;
                _scoreboardService.RecordResult(GameStatus.Draw, null);
            }
        }

        private void RebuildBoardFromHistory(Game game)
        {
            game.Board = new Player?[3, 3];

            foreach (var move in game.MoveHistory)
                game.Board[move.Row, move.Column] = move.Player;

            game.CurrentPlayer = game.MoveHistory.Count % 2 == 0 ? Player.X : Player.O;
            game.Status = GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = null;
        }

        private GameStateDto MapToDto(Game game)
        {
            var jaggedBoard = new Player?[3][];
            for (int r = 0; r < 3; r++)
            {
                jaggedBoard[r] = new Player?[3];
                for (int c = 0; c < 3; c++)
                    jaggedBoard[r][c] = game.Board[r, c];
            }

            return new GameStateDto
            {
                GameId = game.Id,
                Board = jaggedBoard,
                CurrentPlayer = game.CurrentPlayer,
                Mode = game.Mode,
                Status = game.Status,
                Winner = game.Winner,
                WinningCells = game.WinningCells?.Select(c => new[] { c.Row, c.Col }).ToList(),
                MoveHistory = game.MoveHistory.Select(m => new MoveDto
                {
                    MoveNumber = m.MoveNumber,
                    Player = m.Player,
                    Row = m.Row,
                    Column = m.Column
                }).ToList(),
                Scoreboard = _scoreboardService.GetScoreboard()
            };
        }
    }
}