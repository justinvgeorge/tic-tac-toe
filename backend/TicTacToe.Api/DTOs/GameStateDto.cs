using TicTacToe.Api.Models;

namespace TicTacToe.Api.DTOs
{
    public class GameStateDto
    {
        public Guid GameId { get; set; }
        public Player?[][] Board { get; set; } = new Player?[3][];
        public Player CurrentPlayer { get; set; }
        public GameMode Mode { get; set; }
        public GameStatus Status { get; set; }
        public Player? Winner { get; set; }
        public List<int[]>? WinningCells { get; set; }
        public List<MoveDto> MoveHistory { get; set; } = new();
        public ScoreboardDto Scoreboard { get; set; } = new();
    }
}