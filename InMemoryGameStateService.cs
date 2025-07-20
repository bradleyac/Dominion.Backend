using System.Collections.Concurrent;

namespace Dominion.Backend;

public class InMemoryGameStateService(ILogger<InMemoryGameStateService> logger) : IGameStateService
{
  private const int UndoCount = 20;
  private readonly ILogger<InMemoryGameStateService> _logger = logger;
  // GameId to GameState map
  private readonly ConcurrentDictionary<string, GameState> _games = new();
  private readonly ConcurrentDictionary<(string GameId, string PlayerId), GameState[]> _undoTargets = new();
  // PlayerId to GameId map
  private readonly ConcurrentDictionary<string, string> _playerGameMap = new();

  public Task<List<string>> GetAllGameIdsAsync() => Task.FromResult(_games.Keys.ToList());

  public Task<string> CreateGameAsync(string hostPlayerId)
  {
    var gameState = GameFactory.CreateGameState(hostPlayerId);

    _games[gameState.GameId] = gameState;
    _playerGameMap[hostPlayerId] = gameState.GameId;

    _logger.LogInformation($"Created game: {gameState.GameId}");

    return Task.FromResult(gameState.GameId);
  }

  public async Task JoinGameAsync(string playerId, string gameId)
  {
    if (_games[gameId].Players.Any(player => player.Id == playerId)) return;
    // TODO: Validation player not in game already, game exists
    await UpdateGameAsync(GameFactory.AddPlayer(_games[gameId], playerId));
    _playerGameMap[playerId] = gameId;
  }

  public Task<GameState> GetGameAsync(string gameId)
  {
    _games.TryGetValue(gameId, out var game);
    return Task.FromResult(game ?? throw new InvalidOperationException($"Game {gameId} not found"));
  }

  public Task<string?> GetPlayerGameIdAsync(string playerId)
  {
    _playerGameMap.TryGetValue(playerId, out var gameId);
    return Task.FromResult(gameId);
  }

  public Task RemoveGameAsync(string gameId)
  {
    _games.Remove(gameId, out _);
    _undoTargets.RemoveIf(key => key.GameId == gameId);
    var playersInGame = _playerGameMap.Where(kvp => kvp.Value == gameId).Select(kvp => kvp.Key).ToArray();
    _playerGameMap.RemoveIf(key => playersInGame.Contains(key));
    return Task.CompletedTask;
  }

  public Task SetPlayerGameIdAsync(string playerId, string gameId)
  {
    _playerGameMap[playerId] = gameId;
    return Task.CompletedTask;
  }

  public async Task UndoAsync(string playerId, string gameId)
  {
    if (_games.TryGetValue(gameId, out var game))
    {
      if (_undoTargets.TryGetValue((gameId, playerId), out var targets))
      {
        if (targets.Any())
        {
          _undoTargets[(gameId, playerId)] = [.. targets.SkipLast(1)];
          await UpdateGameAsync(targets.Last(), addUndoTarget: false);
        }
      }
    }
  }

  public Task UpdateGameAsync(GameState newGame, bool addUndoTarget = true)
  {
    var oldGame = _games[newGame.GameId];

    if (addUndoTarget && oldGame is not null)
    {
      if (oldGame.ActivePlayerId is not null or "")
      {
        if (_undoTargets.TryGetValue((oldGame.GameId, oldGame.ActivePlayerId), out var gameStates))
        {
          _undoTargets[(oldGame.GameId, oldGame.ActivePlayerId)] = [.. gameStates.TakeLast(UndoCount - 1), oldGame];
        }
        else
        {
          _undoTargets[(oldGame.GameId, oldGame.ActivePlayerId)] = [oldGame];
        }
      }
    }

    _games[newGame.GameId] = newGame;
    return Task.CompletedTask;
  }

  public Task<string[]> GetPlayersInGameAsync(string gameId)
  {
    return Task.FromResult(_games[gameId].Players.Select(p => p.Id).ToArray());
  }
}