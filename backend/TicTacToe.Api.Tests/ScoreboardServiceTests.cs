using TicTacToe.Api.Models;
using TicTacToe.Api.Repositories;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests
{
    public class ScoreboardServiceTests
    {
        private static ScoreboardService CreateService() => new(new InMemoryScoreboardRepository());

        [Fact]
        public void GetScoreboard_StartsAtZero()
        {
            var scoreboard = CreateService().GetScoreboard();

            Assert.Equal(0, scoreboard.XWins);
            Assert.Equal(0, scoreboard.OWins);
            Assert.Equal(0, scoreboard.Draws);
        }

        [Fact]
        public void RecordResult_IncrementsXWins_WhenXWon()
        {
            var service = CreateService();

            service.RecordResult(GameStatus.Won, Player.X);

            Assert.Equal(1, service.GetScoreboard().XWins);
        }

        [Fact]
        public void RecordResult_IncrementsOWins_WhenOWon()
        {
            var service = CreateService();

            service.RecordResult(GameStatus.Won, Player.O);

            Assert.Equal(1, service.GetScoreboard().OWins);
        }

        [Fact]
        public void RecordResult_IncrementsDraws_OnDraw()
        {
            var service = CreateService();

            service.RecordResult(GameStatus.Draw, null);

            Assert.Equal(1, service.GetScoreboard().Draws);
        }

        [Fact]
        public void RecordResult_DoesNothing_WhenStatusIsInProgress()
        {
            var service = CreateService();

            service.RecordResult(GameStatus.InProgress, null);

            var scoreboard = service.GetScoreboard();
            Assert.Equal(0, scoreboard.XWins);
            Assert.Equal(0, scoreboard.OWins);
            Assert.Equal(0, scoreboard.Draws);
        }

        [Fact]
        public void RecordResult_AccumulatesAcrossMultipleGames()
        {
            var service = CreateService();

            service.RecordResult(GameStatus.Won, Player.X);
            service.RecordResult(GameStatus.Won, Player.X);
            service.RecordResult(GameStatus.Won, Player.O);
            service.RecordResult(GameStatus.Draw, null);

            var scoreboard = service.GetScoreboard();
            Assert.Equal(2, scoreboard.XWins);
            Assert.Equal(1, scoreboard.OWins);
            Assert.Equal(1, scoreboard.Draws);
        }

        [Fact]
        public void ResetScoreboard_ZeroesAllCounters()
        {
            var service = CreateService();
            service.RecordResult(GameStatus.Won, Player.X);
            service.RecordResult(GameStatus.Won, Player.O);
            service.RecordResult(GameStatus.Draw, null);

            var result = service.ResetScoreboard();

            Assert.Equal(0, result.XWins);
            Assert.Equal(0, result.OWins);
            Assert.Equal(0, result.Draws);
        }
    }
}

