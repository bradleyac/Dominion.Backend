using System.Collections.Generic;

namespace Dominion.Backend;

public interface IGameStateService
{
  Task<List<string>> GetAllGameIdsAsync();
  Task<GameState> GetGameAsync(string gameId);
  Task<string?> GetPlayerGameIdAsync(string playerId);
  Task<string> CreateGameAsync(string hostPlayerId);
  Task JoinGameAsync(string playerId, string gameId);
  Task UpdateGameAsync(GameState game, bool addUndoTarget = true);
  Task RemoveGameAsync(string gameId);
  Task UndoAsync(string playerId, string gameId);
  Task SetPlayerGameIdAsync(string playerId, string gameId);
  Task<string[]> GetPlayersInGameAsync(string gameId);
}
