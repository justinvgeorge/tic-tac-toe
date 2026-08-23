using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;
using TicTacToe.Api.Repositories.Interfaces;
using TicTacToe.Api.Services.Interfaces;

namespace TicTacToe.Api.Services
{
    public class ScoreboardService : IScoreboardService
    {
        private readonly IScoreboardRepository _scoreboardRepository;

        public ScoreboardService(IScoreboardRepository scoreboardRepository)
        {
            _scoreboardRepository = scoreboardRepository;
        }

        public ScoreboardDto GetScoreboard()
        {
            var (xWins, oWins, draws) = _scoreboardRepository.Get();
            return new ScoreboardDto { XWins = xWins, OWins = oWins, Draws = draws };
        }

        public void RecordResult(GameStatus status, Player? winner)
        {
            if (status == GameStatus.Won)
            {
                if (winner == Player.X) _scoreboardRepository.IncrementXWins();
                else if (winner == Player.O) _scoreboardRepository.IncrementOWins();
            }
            else if (status == GameStatus.Draw)
            {
                _scoreboardRepository.IncrementDraws();
            }
        }

        public ScoreboardDto ResetScoreboard()
        {
            _scoreboardRepository.Reset();
            return GetScoreboard();
        }
    }
}