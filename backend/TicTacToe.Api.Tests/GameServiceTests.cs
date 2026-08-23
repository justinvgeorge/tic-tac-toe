using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;
using TicTacToe.Api.Repositories;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests
{
    public class GameServiceTests
    {
        private static GameService CreateService() =>
            new(
                new InMemoryGameRepository(),
                new ScoreboardService(new InMemoryScoreboardRepository()),
                new ComputerPlayerService());

        [Fact]
        public void CreateGame_ReturnsFreshInProgressGame_WithXToMoveFirst()
        {
            var service = CreateService();

            var state = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            Assert.NotEqual(Guid.Empty, state.GameId);
            Assert.Equal(GameStatus.InProgress, state.Status);
            Assert.Equal(Player.X, state.CurrentPlayer);
            Assert.Empty(state.MoveHistory);
            Assert.All(state.Board, row => Assert.All(row, cell => Assert.Null(cell)));
        }

        [Fact]
        public void GetGame_Throws_WhenGameDoesNotExist()
        {
            var service = CreateService();

            Assert.Throws<KeyNotFoundException>(() => service.GetGame(Guid.NewGuid()));
        }

        [Fact]
        public void MakeMove_PlacesTheMoveAndAdvancesTurn()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            var result = service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });

            Assert.Equal(Player.X, result.Board[0][0]);
            Assert.Equal(Player.O, result.CurrentPlayer);
            Assert.Single(result.MoveHistory);
            Assert.Equal(1, result.MoveHistory[0].MoveNumber);
        }

        [Fact]
        public void MakeMove_Throws_WhenCellIsAlreadyOccupied()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });
            service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });

            Assert.Throws<InvalidOperationException>(() =>
                service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.O, Row = 0, Column = 0 }));
        }

        [Fact]
        public void MakeMove_Throws_WhenItIsNotThatPlayersTurn()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            Assert.Throws<InvalidOperationException>(() =>
                service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.O, Row = 0, Column = 0 }));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)]
        [InlineData(0, 3)]
        public void MakeMove_Throws_WhenMoveIsOutOfBounds(int row, int column)
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            Assert.Throws<ArgumentException>(() =>
                service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = row, Column = column }));
        }

        [Fact]
        public void MakeMove_Throws_WhenGameDoesNotExist()
        {
            var service = CreateService();

            Assert.Throws<KeyNotFoundException>(() =>
                service.MakeMove(Guid.NewGuid(), new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 }));
        }

        [Fact]
        public void MakeMove_Throws_WhenGameIsAlreadyFinished()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });
            PlayTopRowWinForX(service, game.GameId);

            Assert.Throws<InvalidOperationException>(() =>
                service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.O, Row = 2, Column = 2 }));
        }

        [Fact]
        public void MakeMove_DetectsWinAndRecordsScoreboard()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            var result = PlayTopRowWinForX(service, game.GameId);

            Assert.Equal(GameStatus.Won, result.Status);
            Assert.Equal(Player.X, result.Winner);
            Assert.NotNull(result.WinningCells);
            Assert.Equal(1, result.Scoreboard.XWins);
        }

        [Fact]
        public void MakeMove_DetectsDraw_WhenBoardFillsWithNoWinner()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            // X O X
            // X O O
            // O X X
            var moves = new (Player Player, int Row, int Column)[]
            {
                (Player.X, 0, 0), (Player.O, 0, 1), (Player.X, 0, 2),
                (Player.O, 1, 1), (Player.X, 1, 0), (Player.O, 1, 2),
                (Player.X, 2, 1), (Player.O, 2, 0),
            };
            foreach (var (player, row, column) in moves)
                service.MakeMove(game.GameId, new MoveRequestDto { Player = player, Row = row, Column = column });

            var result = service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 2, Column = 2 });

            Assert.Equal(GameStatus.Draw, result.Status);
            Assert.Null(result.Winner);
            Assert.Equal(1, result.Scoreboard.Draws);
        }

        [Fact]
        public void MakeMove_InVsComputerMode_AutomaticallyPlaysTheComputersReply()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.VsComputer });

            var result = service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });

            // Two plies happened in this single request: the human's move and the
            // computer's reply, both already applied.
            Assert.Equal(2, result.MoveHistory.Count);
            Assert.Equal(Player.X, result.MoveHistory[0].Player);
            Assert.Equal(Player.O, result.MoveHistory[1].Player);
            Assert.Equal(Player.X, result.CurrentPlayer); // control returns to the human
            Assert.Equal(Player.O, result.Board[1][1]); // heuristic takes the empty center
        }

        [Fact]
        public void UndoMove_InTwoPlayerMode_RemovesOnlyTheLastMove()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });
            service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });
            service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.O, Row = 1, Column = 1 });

            var result = service.UndoMove(game.GameId);

            Assert.Single(result.MoveHistory);
            Assert.Equal(Player.X, result.Board[0][0]);
            Assert.Null(result.Board[1][1]);
            Assert.Equal(Player.O, result.CurrentPlayer);
        }

        [Fact]
        public void UndoMove_InVsComputerMode_RemovesTheComputersMoveAndThePrecedingHumanMove()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.VsComputer });
            service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });

            var result = service.UndoMove(game.GameId);

            Assert.Empty(result.MoveHistory);
            Assert.All(result.Board, row => Assert.All(row, cell => Assert.Null(cell)));
            Assert.Equal(Player.X, result.CurrentPlayer); // control stays with the human
        }

        [Fact]
        public void UndoMove_Throws_WhenThereAreNoMovesToUndo()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });

            Assert.Throws<InvalidOperationException>(() => service.UndoMove(game.GameId));
        }

        [Fact]
        public void UndoMove_Throws_WhenGameIsAlreadyFinished()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });
            PlayTopRowWinForX(service, game.GameId);

            Assert.Throws<InvalidOperationException>(() => service.UndoMove(game.GameId));
        }

        [Fact]
        public void UndoMove_Throws_WhenGameDoesNotExist()
        {
            var service = CreateService();

            Assert.Throws<KeyNotFoundException>(() => service.UndoMove(Guid.NewGuid()));
        }

        [Fact]
        public void ResetGame_ClearsBoardAndHistory_ButKeepsGameIdModeAndScoreboard()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.VsComputer });
            service.MakeMove(game.GameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });

            var result = service.ResetGame(game.GameId);

            Assert.Equal(game.GameId, result.GameId);
            Assert.Equal(GameMode.VsComputer, result.Mode);
            Assert.Equal(GameStatus.InProgress, result.Status);
            Assert.Equal(Player.X, result.CurrentPlayer);
            Assert.Empty(result.MoveHistory);
            Assert.All(result.Board, row => Assert.All(row, cell => Assert.Null(cell)));
        }

        [Fact]
        public void ResetGame_DoesNotAffectTheScoreboard()
        {
            var service = CreateService();
            var game = service.CreateGame(new CreateGameRequestDto { Mode = GameMode.TwoPlayer });
            PlayTopRowWinForX(service, game.GameId); // XWins becomes 1

            var result = service.ResetGame(game.GameId);

            Assert.Equal(1, result.Scoreboard.XWins);
        }

        [Fact]
        public void ResetGame_Throws_WhenGameDoesNotExist()
        {
            var service = CreateService();

            Assert.Throws<KeyNotFoundException>(() => service.ResetGame(Guid.NewGuid()));
        }

        /// <summary>Plays X(0,0) O(1,0) X(0,1) O(1,1) X(0,2), giving X the top-row win.</summary>
        private static GameStateDto PlayTopRowWinForX(GameService service, Guid gameId)
        {
            service.MakeMove(gameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 0 });
            service.MakeMove(gameId, new MoveRequestDto { Player = Player.O, Row = 1, Column = 0 });
            service.MakeMove(gameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 1 });
            service.MakeMove(gameId, new MoveRequestDto { Player = Player.O, Row = 1, Column = 1 });
            return service.MakeMove(gameId, new MoveRequestDto { Player = Player.X, Row = 0, Column = 2 });
        }
    }
}
