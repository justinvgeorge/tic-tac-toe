using TicTacToe.Api.DTOs;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services.Interfaces
{
    public interface IScoreboardService
    {
        ScoreboardDto GetScoreboard();
        void RecordResult(GameStatus status, Player? winner);
        ScoreboardDto ResetScoreboard();
    }
}