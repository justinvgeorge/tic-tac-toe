namespace TicTacToe.Api.Repositories.Interfaces
{
    public interface IScoreboardRepository
    {
        (int XWins, int OWins, int Draws) Get();
        void IncrementXWins();
        void IncrementOWins();
        void IncrementDraws();
        void Reset();
    }
}
