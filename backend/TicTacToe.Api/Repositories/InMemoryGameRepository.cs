using System.Collections.Concurrent;
using TicTacToe.Api.Models;
using TicTacToe.Api.Repositories.Interfaces;

namespace TicTacToe.Api.Repositories
{
    public class InMemoryGameRepository : IGameRepository
    {
        private readonly ConcurrentDictionary<Guid, Game> _games = new();

        public Game? GetById(Guid id)
        {
            _games.TryGetValue(id, out var game);
            return game;
        }

        public void Save(Game game)
        {
            _games[game.Id] = game;
        }
    }
}