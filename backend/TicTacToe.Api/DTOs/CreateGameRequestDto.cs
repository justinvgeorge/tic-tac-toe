using TicTacToe.Api.Models;

namespace TicTacToe.Api.DTOs
{
    public class CreateGameRequestDto
    {
        public GameMode Mode { get; set; }
    }
}