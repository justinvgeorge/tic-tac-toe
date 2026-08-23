namespace TicTacToe.Api.Models
{
    public class Game
    {
        public Guid Id { get; set; }
        public Player?[,] Board { get; set; } = new Player?[3, 3];
        public Player CurrentPlayer { get; set; }
        public GameMode Mode { get; set; }
        public GameStatus Status { get; set; }
        public Player? Winner { get; set; }
        public List<(int Row, int Col)>? WinningCells { get; set; }
        public List<Move> MoveHistory { get; set; } = new List<Move>();
    }
}