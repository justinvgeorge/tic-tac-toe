using TicTacToe.Api.Models;

namespace TicTacToe.Api.Tests
{
    /// <summary>
    /// Builds a 3x3 board from a compact 9-character string (row-major:
    /// index 0 = (0,0) ... index 8 = (2,2)). 'X' / 'O' / '.' (empty).
    /// </summary>
    internal static class BoardBuilder
    {
        public static Player?[,] From(string flat)
        {
            if (flat.Length != 9)
                throw new ArgumentException("Board string must be exactly 9 characters.", nameof(flat));

            var board = new Player?[3, 3];
            for (int i = 0; i < 9; i++)
            {
                board[i / 3, i % 3] = flat[i] switch
                {
                    'X' => Player.X,
                    'O' => Player.O,
                    '.' => (Player?)null,
                    var ch => throw new ArgumentException($"Unexpected character '{ch}' in board string.", nameof(flat)),
                };
            }
            return board;
        }
    }
}
