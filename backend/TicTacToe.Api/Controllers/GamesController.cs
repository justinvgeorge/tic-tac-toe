using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.DTOs;
using TicTacToe.Api.Services.Interfaces;

namespace TicTacToe.Api.Controllers
{
    [ApiController]
    [Route("api/games")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost]
        public ActionResult<GameStateDto> Create([FromBody] CreateGameRequestDto request)
        {
            return Ok(_gameService.CreateGame(request));
        }

        [HttpGet("{id}")]
        public ActionResult<GameStateDto> Get(Guid id)
        {
            try { return Ok(_gameService.GetGame(id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id}/moves")]
        public ActionResult<GameStateDto> Move(Guid id, [FromBody] MoveRequestDto request)
        {
            try { return Ok(_gameService.MakeMove(id, request)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("{id}/undo")]
        public ActionResult<GameStateDto> Undo(Guid id)
        {
            try { return Ok(_gameService.UndoMove(id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("{id}/reset")]
        public ActionResult<GameStateDto> Reset(Guid id)
        {
            try { return Ok(_gameService.ResetGame(id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}