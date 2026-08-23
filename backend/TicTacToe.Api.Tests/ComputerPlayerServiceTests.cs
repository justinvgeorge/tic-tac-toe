using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests
{
    public class ComputerPlayerServiceTests
    {
        private readonly ComputerPlayerService _service = new();

        [Fact]
        public void GetNextMove_TakesWinningMove_WhenOneIsAvailable()
        {
            // O has two in a row at (0,0)-(0,1); (0,2) completes the win.
            var board = BoardBuilder.From("OO.......");

            var move = _service.GetNextMove(board);

            Assert.Equal((0, 2), move);
        }

        [Fact]
        public void GetNextMove_PrefersWinningOverBlocking_WhenBothAreAvailable()
        {
            // O can win at (0,2); X separately threatens to win at (1,1). Winning takes priority.
            var board = BoardBuilder.From("OO.X.X...");

            var move = _service.GetNextMove(board);

            Assert.Equal((0, 2), move);
        }

        [Fact]
        public void GetNextMove_BlocksOpponentsWinningMove_WhenNoWinIsAvailable()
        {
            // X has two in a row at (1,0)-(1,1); O must block at (1,2).
            var board = BoardBuilder.From("...XX....");

            var move = _service.GetNextMove(board);

            Assert.Equal((1, 2), move);
        }

        [Fact]
        public void GetNextMove_TakesCenter_WhenAvailableAndNoWinOrBlockIsNeeded()
        {
            var board = BoardBuilder.From("X........");

            var move = _service.GetNextMove(board);

            Assert.Equal((1, 1), move);
        }

        [Fact]
        public void GetNextMove_TakesFirstAvailableCorner_WhenCenterIsAlreadyTaken()
        {
            // Center already occupied, nothing else on the board: falls through to the
            // fixed corner order (0,0) -> (0,2) -> (2,0) -> (2,2) and takes the first.
            var board = BoardBuilder.From("....O....");

            var move = _service.GetNextMove(board);

            Assert.Equal((0, 0), move);
        }

        [Fact]
        public void GetNextMove_ReturnsTheOnlyRemainingEmptyCell()
        {
            // Only (1,2) is empty; whichever branch of the heuristic applies, this is
            // the only legal move.
            var board = BoardBuilder.From("XOXXO.OXO");

            var move = _service.GetNextMove(board);

            Assert.Equal((1, 2), move);
        }

        [Fact]
        public void GetNextMove_Throws_WhenBoardIsCompletelyFull()
        {
            var board = BoardBuilder.From("XOXXOOOXX");

            Assert.Throws<InvalidOperationException>(() => _service.GetNextMove(board));
        }
    }
}
