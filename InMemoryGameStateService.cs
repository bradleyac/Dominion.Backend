using System.Collections.Concurrent;

namespace Dominion.Backend;

public class InMemoryGameStateService(ILogger<InMemoryGameStateService> logger) : IGameStateService
{
  private readonly ILogger<InMemoryGameStateService> _logger = logger;
  // GameId to GameState map
  private readonly ConcurrentDictionary<string, GameState> _games = new();
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

  public Task<GameState?> GetGameAsync(string gameId)
  {
    _games.TryGetValue(gameId, out var game);
    return Task.FromResult(game);
  }

  public Task<string?> GetPlayerGameIdAsync(string playerId)
  {
    _playerGameMap.TryGetValue(playerId, out var gameId);
    return Task.FromResult(gameId);
  }

  public Task RemoveGameAsync(string gameId)
  {
    var playersInGame = _playerGameMap.Where(kvp => kvp.Value == gameId).Select(kvp => kvp.Key);
    foreach (var player in playersInGame)
    {
      _playerGameMap.Remove(player, out _);
    }
    _games.Remove(gameId, out _);
    return Task.CompletedTask;
  }

  public Task SetPlayerGameIdAsync(string playerId, string gameId)
  {
    _playerGameMap[playerId] = gameId;
    return Task.CompletedTask;
  }

  public Task UpdateGameAsync(GameState game)
  {
    _games[game.GameId] = game;
    return Task.CompletedTask;
  }

  public Task<string[]> GetPlayersInGameAsync(string gameId)
  {
    return Task.FromResult(_games[gameId].Players.Select(p => p.Id).ToArray());
  }
}