using Microsoft.AspNetCore.SignalR;

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

    game = GameLogic.StartGame(game);

    await _gameService.UpdateGameAsync(game, addUndoTarget: false);

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

    var (newGameState, played) = GameLogic.PlayCard(game, playerId, cardInstanceId);

    if (played)
    {
      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task EndTurnAsync(string gameId, string playerId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game is null || game.Players[game.CurrentPlayer].Id != playerId || game.ActivePlayerId != game.Players[game.CurrentPlayer].Id)
    {
      return;
    }

    var newGameState = GameLogic.EndTurn(game);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task BuyCardAsync(string gameId, string playerId, int cardId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game is null || game.Players[game.CurrentPlayer].Id != playerId || game.ActivePlayerId != game.Players[game.CurrentPlayer].Id)
    {
      return;
    }

    var newGameState = GameLogic.BuyCard(game, playerId, cardId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task EndActionPhaseAsync(string gameId, string playerId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    var newGameState = GameLogic.EndActionPhase(game, playerId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task SubmitCardInstanceChoicesAsync(string gameId, string playerId, string choiceId, string[] cardInstanceIds)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ResumeState?.EffectResumeState is null || game.ResumeState.EffectResumeState.PlayerIds[game.ResumeState.EffectResumeState.PlayerIndex] != playerId)
    {
      // Not the right player.
      return;
    }

    var player = game.GetPlayer(playerId);

    if (player.ActiveChoice?.Id != choiceId)
    {
      // Old choice.
      return;
    }

    PlayerChoiceResult? choiceResult = player.ActiveChoice switch
    {
      PlayerSelectChoice selectChoice => new PlayerSelectChoiceResult { SelectedCards = [.. cardInstanceIds.Select(cardInstanceId => CardInstance.GetCardInstance(playerId, cardInstanceId, selectChoice.Filter.From, game))] },
      PlayerArrangeChoice arrangeChoice => new PlayerArrangeChoiceResult { ArrangedCards = [.. cardInstanceIds.Select(cardInstanceId => CardInstance.GetCardInstance(playerId, cardInstanceId, arrangeChoice.ZoneToArrange, game))] },
      _ => null
    };

    if (choiceResult is not null)
    {
      var newGameState = GameLogic.ResumeProcessEffectStack(game, playerId, player.ActiveChoice, choiceResult);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task SubmitCardChoicesAsync(string gameId, string playerId, string choiceId, int[] cardIds)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ResumeState?.EffectResumeState is null || game.ResumeState.EffectResumeState.PlayerIds[game.ResumeState.EffectResumeState.PlayerIndex] != playerId)
    {
      // Not the right player.
      return;
    }

    var player = game.GetPlayer(playerId);

    if (player.ActiveChoice?.Id != choiceId)
    {
      // Old choice.
      return;
    }

    if (player.ActiveChoice is PlayerSelectChoice selectChoice)
    {
      // TODO (somewhere): Validate choices

      PlayerSelectChoiceResult result = new PlayerSelectChoiceResult
      {
        SelectedCards = [.. cardIds.Select(CardInstance.CreateByCardId)],
      };

      var newGameState = GameLogic.ResumeProcessEffectStack(game, playerId, player.ActiveChoice, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task SubmitCategorizationAsync(string gameId, string playerId, string choiceId, Dictionary<string, string[]> categorizations)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ResumeState?.EffectResumeState is null || game.ResumeState.EffectResumeState.PlayerIds[game.ResumeState.EffectResumeState.PlayerIndex] != playerId)
    {
      // Not the right player.
      return;
    }

    var player = game.GetPlayer(playerId);

    if (player.ActiveChoice?.Id != choiceId)
    {
      // Old choice.
      return;
    }

    if (player.ActiveChoice is PlayerCategorizeChoice categorizeChoice)
    {
      // TODO (somewhere): Validate choices

      PlayerCategorizeChoiceResult result = new PlayerCategorizeChoiceResult
      {
        CategorizedCards = categorizations.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.Select(id => CardInstance.GetCardInstance(playerId, id, categorizeChoice.ZoneToCategorize, game)).ToArray())).ToDictionary(),
      };

      var newGameState = GameLogic.ResumeProcessEffectStack(game, playerId, player.ActiveChoice, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task DeclineChoiceAsync(string gameId, string playerId, string choiceId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ResumeState?.EffectResumeState?.PlayerIds[game.ResumeState.EffectResumeState.PlayerIndex] != playerId)
    {
      // Not the right player.
      return;
    }

    var player = game.GetPlayer(playerId);

    if (player.ActiveChoice?.Id != choiceId)
    {
      // Old choice.
      return;
    }

    if (player.ActiveChoice is not null && !player.ActiveChoice.IsForced)
    {
      // TODO (somewhere): Validate choices

      var result = new PlayerSelectChoiceResult
      {
        SelectedCards = [],
        IsDeclined = true,
      };

      var newGameState = GameLogic.ResumeProcessEffectStack(game, playerId, player.ActiveChoice, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task UndoAsync(string gameId, string playerId)
  {
    await _gameService.UndoAsync(playerId, gameId);
    await UpdateAllAsync(gameId);
  }

  public async Task UpdateAllAsync(string gameId)
  {
    var game = await _gameService.GetGameAsync(gameId);

    foreach (var playerId in await _gameService.GetPlayersInGameAsync(gameId))
    {
      await Clients.Group(playerId).SendAsync("stateUpdated", game.ToPlayerGameStateViewModel(playerId));
    }

    if (game.GameResult is not null)
    {
      await _gameService.RemoveGameAsync(gameId);
    }
  }

  private async Task AdvertiseGameAsync(string gameId)
  {
    await Clients.All.SendAsync("gameCreated", gameId);
  }
}