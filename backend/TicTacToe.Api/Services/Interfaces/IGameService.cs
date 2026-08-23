using TicTacToe.Api.DTOs;

namespace TicTacToe.Api.Services.Interfaces
{
    public interface IGameService
    {
        GameStateDto CreateGame(CreateGameRequestDto request);
        GameStateDto GetGame(Guid id);
        GameStateDto MakeMove(Guid id, MoveRequestDto request);
        GameStateDto UndoMove(Guid id);
        GameStateDto ResetGame(Guid id);
    }
}