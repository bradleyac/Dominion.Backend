using System.Collections.Generic;

namespace Dominion.Backend;

public interface IGameStateService
{
  public Task<List<Game>> GetAllGameIdsAsync();
  Task<GameState> GetGameAsync(string gameId);
  Task<string> CreateGameAsync(string hostPlayerId);
  Task<bool> JoinGameAsync(string playerId, string gameId);
  Task UpdateGameAsync(GameState game, bool addUndoTarget = true);
  Task RemoveGameAsync(string gameId);
  Task UndoAsync(string playerId, string gameId);
  Task<string[]> GetPlayersInGameAsync(string gameId);
}
