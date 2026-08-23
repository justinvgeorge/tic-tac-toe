using TicTacToe.Api.Models;
using TicTacToe.Api.Services.Interfaces;

namespace TicTacToe.Api.Services
{
    public class ComputerPlayerService : IComputerPlayerService
    {
        private const Player Computer = Player.O;
        private const Player Opponent = Player.X;

        public (int Row, int Col) GetNextMove(Player?[,] board)
        {
            var winning = TryFindWinningMove(board, Computer);
            if (winning.HasValue) return winning.Value;

            var blocking = TryFindWinningMove(board, Opponent);
            if (blocking.HasValue) return blocking.Value;

            if (board[1, 1] == null) return (1, 1);

            foreach (var (r, c) in new[] { (0, 0), (0, 2), (2, 0), (2, 2) })
                if (board[r, c] == null) return (r, c);

            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (board[r, c] == null) return (r, c);

            throw new InvalidOperationException("No available moves on the board.");
        }

        private (int Row, int Col)? TryFindWinningMove(Player?[,] board, Player player)
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (board[r, c] != null) continue;

                    board[r, c] = player;
                    var (winner, _) = GameRules.CheckWin(board);
                    board[r, c] = null;

                    if (winner == player) return (r, c);
                }
            }
            return null;
        }
    }
}