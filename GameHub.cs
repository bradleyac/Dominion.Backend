using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;

namespace Dominion.Backend;

public class GameHub(IGameStateService gameService) : Hub
{
  private readonly IGameStateService _gameService = gameService;

  public async Task<IEnumerable<string>> GetAllGamesAsync()
  {
    return await _gameService.GetAllGameIdsAsync();
  }

  public async Task<string> CreateGameAsync(string playerId)
  {
    string gameId = await _gameService.CreateGameAsync(playerId);
    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    await Groups.AddToGroupAsync(Context.ConnectionId, playerId);

    await AdvertiseGameAsync(gameId);

    return gameId;
  }

  public async Task JoinGameAsync(string gameId, string playerId)
  {
    // TODO: Validation
    await _gameService.JoinGameAsync(playerId, gameId);
    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    await Groups.AddToGroupAsync(Context.ConnectionId, playerId);

    var game = await _gameService.GetGameAsync(gameId);

    game = await GameLogic.StartGameAsync(game);

    await _gameService.UpdateGameAsync(game);

    await UpdateAllAsync(gameId);
  }

  public async Task<GameStateViewModel> GetGameStateAsync(string gameId, string playerId)
  {
    var game = await _gameService.GetGameAsync(gameId);
    return game.ToPlayerGameStateViewModel(playerId);
  }

  public async Task PlayCardAsync(string gameId, string playerId, string cardInstanceId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    var newGameState = await GameLogic.PlayCardAsync(game, playerId, cardInstanceId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task EndTurnAsync(string gameId, string playerId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game is null || game.Players[game.CurrentPlayer].Id != playerId || game.ActivePlayer != game.CurrentPlayer)
    {
      return;
    }

    var newGameState = await GameLogic.EndTurnAsync(game);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task BuyCardAsync(string gameId, string playerId, int cardId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game is null || game.Players[game.CurrentPlayer].Id != playerId || game.ActivePlayer != game.CurrentPlayer)
    {
      return;
    }

    var newGameState = await GameLogic.BuyCardAsync(game, playerId, cardId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task EndActionPhaseAsync(string gameId, string playerId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    var newGameState = await GameLogic.EndActionPhaseAsync(game, playerId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task SubmitCardInstanceChoicesAsync(string gameId, string playerId, string[] cardInstanceIds)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ResumeState?.EffectResumeState is null || game.ResumeState.EffectResumeState.PlayerIds[game.ResumeState.EffectResumeState.PlayerIndex] != playerId)
    {
      // Not the right player.
      return;
    }
    // TODO (somewhere): Validate choices

    var player = game.GetPlayer(playerId);

    ChosenCards chosenCards = new ChosenCards
    {
      CardInstances = [.. cardInstanceIds.Select(cardInstanceId => CardInstance.GetCardInstance(playerId, cardInstanceId, player.ActiveFilter.From, game))],
      From = player.ActiveFilter.From
    };

    var newGameState = await GameLogic.ResumePlayingCard(game, playerId, chosenCards);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task SubmitCardChoicesAsync(string gameId, string playerId, int[] cardIds)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ResumeState?.EffectResumeState is null || game.ResumeState.EffectResumeState.PlayerIds[game.ResumeState.EffectResumeState.PlayerIndex] != playerId)
    {
      // Not the right player.
      return;
    }
    // TODO (somewhere): Validate choices

    var player = game.GetPlayer(playerId);

    ChosenCards chosenCards = new ChosenCards
    {
      CardInstances = [.. cardIds.Select(CardInstance.CreateByCardId)],
      From = player.ActiveFilter.From
    };

    var newGameState = await GameLogic.ResumePlayingCard(game, playerId, chosenCards);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task UpdateAllAsync(string gameId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    foreach (var playerId in await _gameService.GetPlayersInGameAsync(gameId))
    {
      await Clients.Group(playerId).SendAsync("stateUpdated", game.ToPlayerGameStateViewModel(playerId));
    }
  }

  private async Task AdvertiseGameAsync(string gameId)
  {
    await Clients.All.SendAsync("gameCreated", gameId);
  }
}