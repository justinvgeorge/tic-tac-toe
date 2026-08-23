using TicTacToe.Api.Repositories.Interfaces;

namespace TicTacToe.Api.Repositories
{
    public class InMemoryScoreboardRepository : IScoreboardRepository
    {
        private int _xWins;
        private int _oWins;
        private int _draws;

        public (int XWins, int OWins, int Draws) Get() => (_xWins, _oWins, _draws);
        public void IncrementXWins() => _xWins++;
        public void IncrementOWins() => _oWins++;
        public void IncrementDraws() => _draws++;
        public void Reset()
        {
            _xWins = 0;
            _oWins = 0;
            _draws = 0;
        }
    }
}