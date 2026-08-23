using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.DTOs;
using TicTacToe.Api.Services.Interfaces;

namespace TicTacToe.Api.Controllers
{
    [ApiController]
    [Route("api/scoreboard")]
    public class ScoreboardController : ControllerBase
    {
        private readonly IScoreboardService _scoreboardService;

        public ScoreboardController(IScoreboardService scoreboardService)
        {
            _scoreboardService = scoreboardService;
        }

        [HttpGet]
        public ActionResult<ScoreboardDto> Get()
        {
            return Ok(_scoreboardService.GetScoreboard());
        }

        [HttpPost("reset")]
        public ActionResult<ScoreboardDto> Reset()
        {
            return Ok(_scoreboardService.ResetScoreboard());
        }
    }
}