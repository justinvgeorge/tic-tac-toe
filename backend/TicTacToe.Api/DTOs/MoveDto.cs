using TicTacToe.Api.Models;

namespace TicTacToe.Api.DTOs
{
    public class MoveDto
    {
        public int MoveNumber { get; set; }
        public Player Player { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }
}