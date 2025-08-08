using System.Collections.Concurrent;

namespace Dominion.Backend;

public class InMemoryGameStateService(ILogger<InMemoryGameStateService> logger) : IGameStateService
{
  private const int UndoCount = 50;
  private readonly ILogger<InMemoryGameStateService> _logger = logger;
  // GameId to GameState map
  private readonly ConcurrentDictionary<string, GameState> _games = new();
  private readonly ConcurrentDictionary<string, GameState[]> _undoTargets = new();

  public Task<List<Game>> GetAllGameIdsAsync() => Task.FromResult(_games.Values.Select(g => new Game(g.GameId, g.DisplayName, [.. g.Players.Select(p => p.Id)], g.ActivePlayerId)).ToList());

  public Task<string> CreateGameAsync(string hostPlayerId)
  {
    var gameState = GameFactory.CreateGameState(hostPlayerId);

    _games[gameState.GameId] = gameState;

    _logger.LogInformation($"Created game: {gameState.GameId}");

    return Task.FromResult(gameState.GameId);
  }

  public async Task<bool> JoinGameAsync(string playerId, string gameId)
  {
    if (_games[gameId].Players.Any(player => player.Id == playerId))
    {
      return false;
    }

    // TODO: Validation player not in game already, game exists
    await UpdateGameAsync(GameFactory.AddPlayer(_games[gameId], playerId), addUndoTarget: false);
    return true;
  }

  public Task<GameState> GetGameAsync(string gameId)
  {
    _games.TryGetValue(gameId, out var game);
    return Task.FromResult(game ?? throw new InvalidOperationException($"Game {gameId} not found"));
  }

  public Task RemoveGameAsync(string gameId)
  {
    _games.Remove(gameId, out _);
    _undoTargets.Remove(gameId, out _);
    return Task.CompletedTask;
  }

  public async Task UndoAsync(string playerId, string gameId)
  {
    if (_games.TryGetValue(gameId, out var game) && _undoTargets.TryGetValue(gameId, out var targets))
    {
      var lastActiveStateForPlayer = targets.Where(t => t.ActivePlayerId == playerId).LastOrDefault();

      if (lastActiveStateForPlayer is null)
      {
        return;
      }

      _undoTargets[gameId] = [.. targets.Where(t => t.SequenceId < lastActiveStateForPlayer.SequenceId)];
      await UpdateGameAsync(lastActiveStateForPlayer, addUndoTarget: false);
    }
  }

  public Task UpdateGameAsync(GameState newGame, bool addUndoTarget = true)
  {
    var oldGame = _games[newGame.GameId];

    if (addUndoTarget && oldGame?.ActivePlayerId is not null or "")
    {
      if (_undoTargets.TryGetValue(newGame.GameId, out var gameStates))
      {
        _undoTargets[newGame.GameId] = [.. gameStates.TakeLast(UndoCount - 1), oldGame];
      }
      else
      {
        _undoTargets[newGame.GameId] = [oldGame];
      }
    }

    _games[newGame.GameId] = newGame with { SequenceId = newGame.SequenceId + 1 };

    return Task.CompletedTask;
  }

  public Task<string[]> GetPlayersInGameAsync(string gameId)
  {
    return Task.FromResult(_games[gameId].Players.Select(p => p.Id).ToArray());
  }
}