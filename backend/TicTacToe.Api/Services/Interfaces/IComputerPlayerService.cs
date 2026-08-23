using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services.Interfaces
{
    public interface IComputerPlayerService
    {
        (int Row, int Col) GetNextMove(Player?[,] board);
    }
}