using TicTacToe.Api.Models;

namespace TicTacToe.Api.Repositories.Interfaces
{
    public interface IGameRepository
    {
        Game? GetById(Guid id);
        void Save(Game game);
    }
}
