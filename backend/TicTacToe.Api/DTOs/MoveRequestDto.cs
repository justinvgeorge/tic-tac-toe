using TicTacToe.Api.Models;

namespace TicTacToe.Api.DTOs
{
    public class MoveRequestDto
    {
        public Player Player { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }
}