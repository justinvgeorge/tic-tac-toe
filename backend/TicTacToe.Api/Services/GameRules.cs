using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services
{
    public static class GameRules
    {
        public static readonly int[][] WinningCombinations = new int[][]
        {
            new[] { 0, 1, 2 }, // row 0
            new[] { 3, 4, 5 }, // row 1
            new[] { 6, 7, 8 }, // row 2
            new[] { 0, 3, 6 }, // col 0
            new[] { 1, 4, 7 }, // col 1
            new[] { 2, 5, 8 }, // col 2
            new[] { 0, 4, 8 }, // diagonal ↘
            new[] { 2, 4, 6 }  // diagonal ↙
        };

        public static (Player? Winner, List<(int Row, int Col)>? Cells) CheckWin(Player?[,] board)
        {
            foreach (var combo in WinningCombinations)
            {
                var (r0, c0) = (combo[0] / 3, combo[0] % 3);
                var (r1, c1) = (combo[1] / 3, combo[1] % 3);
                var (r2, c2) = (combo[2] / 3, combo[2] % 3);

                var a = board[r0, c0];
                var b = board[r1, c1];
                var c = board[r2, c2];

                if (a != null && a == b && b == c)
                    return (a, new List<(int, int)> { (r0, c0), (r1, c1), (r2, c2) });
            }

            return (null, null);
        }

        public static bool CheckDraw(Player?[,] board)
        {
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (board[r, c] == null) return false;

            return true;
        }
    }
}