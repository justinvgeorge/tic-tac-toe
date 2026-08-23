using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests
{
    public class GameRulesTests
    {
        [Theory]
        [InlineData("XXX......", 0, 0, 0, 1, 0, 2)] // row 0
        [InlineData("...XXX...", 1, 0, 1, 1, 1, 2)] // row 1
        [InlineData("......XXX", 2, 0, 2, 1, 2, 2)] // row 2
        [InlineData("X..X..X..", 0, 0, 1, 0, 2, 0)] // col 0
        [InlineData(".X..X..X.", 0, 1, 1, 1, 2, 1)] // col 1
        [InlineData("..X..X..X", 0, 2, 1, 2, 2, 2)] // col 2
        [InlineData("X...X...X", 0, 0, 1, 1, 2, 2)] // diagonal ↘
        [InlineData("..X.X.X..", 0, 2, 1, 1, 2, 0)] // diagonal ↙
        public void CheckWin_DetectsEachWinningCombination(
            string boardString, int r0, int c0, int r1, int c1, int r2, int c2)
        {
            var board = BoardBuilder.From(boardString);

            var (winner, cells) = GameRules.CheckWin(board);

            Assert.Equal(Player.X, winner);
            Assert.Equal(new List<(int, int)> { (r0, c0), (r1, c1), (r2, c2) }, cells);
        }

        [Fact]
        public void CheckWin_ReturnsNoWinner_WhenBoardHasNoThreeInARow()
        {
            var board = BoardBuilder.From("XO.......");

            var (winner, cells) = GameRules.CheckWin(board);

            Assert.Null(winner);
            Assert.Null(cells);
        }

        [Fact]
        public void CheckWin_ReturnsNoWinner_OnEmptyBoard()
        {
            var board = new Player?[3, 3];

            var (winner, _) = GameRules.CheckWin(board);

            Assert.Null(winner);
        }

        [Fact]
        public void CheckDraw_ReturnsFalse_WhenBoardHasEmptyCells()
        {
            var board = BoardBuilder.From("XOXOXOXO.");

            Assert.False(GameRules.CheckDraw(board));
        }

        [Fact]
        public void CheckDraw_ReturnsTrue_WhenBoardIsCompletelyFull()
        {
            var board = BoardBuilder.From("XOXXOOOXX");

            Assert.True(GameRules.CheckDraw(board));
        }
    }
}
