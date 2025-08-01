using Microsoft.AspNetCore.SignalR;

namespace Dominion.Backend;

public class GameHub(IGameStateService gameService, ILogger<GameHub> logger) : Hub
{
  private readonly IGameStateService _gameService = gameService;
  private ILogger<GameHub> _logger = logger;

  public async Task<IEnumerable<Game>> GetAllGamesAsync()
  {
    return await _gameService.GetAllGameIdsAsync();
  }

  public async Task<string> CreateGameAsync()
  {
    string playerId = GetPlayerId();
    _logger.LogWarning($"PlayerId: {playerId}");

    string gameId = await _gameService.CreateGameAsync(playerId);

    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    await Groups.AddToGroupAsync(Context.ConnectionId, playerId);

    var game = await _gameService.GetGameAsync(gameId);
    await AdvertiseGameAsync(new Game(game.GameId, game.Players.Select(p => p.Id).ToArray()));

    return gameId;
  }

  public async Task<bool> JoinGameAsync(string gameId)
  {
    string playerId = GetPlayerId();
    bool joined = await _gameService.JoinGameAsync(playerId, gameId);

    if (!joined)
    {
      return false;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    await Groups.AddToGroupAsync(Context.ConnectionId, playerId);

    var game = await _gameService.GetGameAsync(gameId);

    game = GameLogic.StartGame(game);

    await _gameService.UpdateGameAsync(game, addUndoTarget: false);

    await UpdateAllAsync(gameId);

    return true;
  }

  public async Task AbandonGameAsync(string gameId)
  {
    string playerId = GetPlayerId();

    var game = await _gameService.GetGameAsync(gameId);

    if (game.Players.Select(p => p.Id).Contains(playerId))
    {
      // If the game is already started, end it.
      // Otherwise, remove the player.
      if (game.GameStarted)
      {
        // TODO: Make a better "Game Forfeited" result
        game = game with { GameResult = new([], []) };
      }
      else
      {
        game = game with { Players = [.. game.Players.Where(p => p.Id != playerId)] };
      }

      if (!game.Players.Any())
      {
        await _gameService.RemoveGameAsync(gameId);
      }
      else
      {
        await _gameService.UpdateGameAsync(game);

        await UpdateAllAsync(gameId);
      }
    }
  }

  public async Task<GameStateViewModel> GetGameStateAsync(string gameId)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);
    return game.ToPlayerGameStateViewModel(playerId);
  }

  public async Task PlayCardAsync(string gameId, string cardInstanceId)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);

    var (newGameState, played) = GameLogic.PlayCard(game, playerId, cardInstanceId);

    if (played)
    {
      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task EndTurnAsync(string gameId)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);

    if (game is null || game.Players[game.CurrentPlayer].Id != playerId || game.ActivePlayerId != game.Players[game.CurrentPlayer].Id)
    {
      return;
    }

    var newGameState = GameLogic.EndTurn(game);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task BuyCardAsync(string gameId, int cardId)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);

    if (game is null || game.Players[game.CurrentPlayer].Id != playerId || game.ActivePlayerId != game.Players[game.CurrentPlayer].Id)
    {
      return;
    }

    var newGameState = GameLogic.BuyCard(game, playerId, cardId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task EndActionPhaseAsync(string gameId)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);

    var newGameState = GameLogic.EndActionPhase(game, playerId);

    await _gameService.UpdateGameAsync(newGameState);

    await UpdateAllAsync(gameId);
  }

  public async Task SubmitCardInstanceChoicesAsync(string gameId, string choiceId, string[] cardInstanceIds)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);
    var player = game.GetPlayer(playerId);

    if (game?.ActivePlayerId != playerId || player.ActiveChoice?.Id != choiceId)
    {
      // Not the right player or old choice.
      return;
    }

    PlayerChoiceResult? result = player.ActiveChoice switch
    {
      PlayerSelectChoice selectChoice => new PlayerSelectChoiceResult { SelectedCards = [.. cardInstanceIds.Select(cardInstanceId => CardInstance.GetCardInstance(playerId, cardInstanceId, selectChoice.Filter.From, game))] },
      PlayerArrangeChoice arrangeChoice => new PlayerArrangeChoiceResult { ArrangedCards = [.. cardInstanceIds.Select(cardInstanceId => CardInstance.GetCardInstance(playerId, cardInstanceId, arrangeChoice.ZoneToArrange, game))] },
      PlayerReactChoice reactChoice => new PlayerReactChoiceResult { ChosenReaction = CardInstance.GetCardInstance(playerId, cardInstanceIds[0], reactChoice.EffectReferences.First(effect => effect.CardInstance.InstanceId == cardInstanceIds[0]).CardInstance.Location, game) },
      _ => null
    };

    if (result is not null)
    {
      var newGameState = GameLogic.ProcessEffectStack(game, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task SubmitCardChoicesAsync(string gameId, string choiceId, int[] cardIds)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);
    var player = game.GetPlayer(playerId);

    if (game?.ActivePlayerId != playerId || player.ActiveChoice?.Id != choiceId)
    {
      // Not the right player or old choice.
      return;
    }

    if (player.ActiveChoice is PlayerSelectChoice selectChoice)
    {
      // TODO (somewhere): Validate choices

      PlayerSelectChoiceResult result = new PlayerSelectChoiceResult
      {
        SelectedCards = [.. cardIds.Select(CardInstance.CreateByCardId)],
      };

      var newGameState = GameLogic.ProcessEffectStack(game, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task SubmitCategorizationAsync(string gameId, string choiceId, Dictionary<string, string[]> categorizations)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);
    var player = game.GetPlayer(playerId);

    if (game?.ActivePlayerId != playerId || player.ActiveChoice?.Id != choiceId)
    {
      // Not the right player or old choice.
      return;
    }

    if (player.ActiveChoice is PlayerCategorizeChoice categorizeChoice)
    {
      // TODO (somewhere): Validate choices

      PlayerCategorizeChoiceResult result = new PlayerCategorizeChoiceResult
      {
        CategorizedCards = categorizations.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.Select(id => CardInstance.GetCardInstance(playerId, id, categorizeChoice.ZoneToCategorize, game)).ToArray())).ToDictionary(),
      };

      var newGameState = GameLogic.ProcessEffectStack(game, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task DeclineChoiceAsync(string gameId, string choiceId)
  {
    string playerId = GetPlayerId();
    var game = await _gameService.GetGameAsync(gameId);

    if (game?.ActivePlayerId != playerId || game.GetPlayer(playerId).ActiveChoice is null)
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

      PlayerChoiceResult result = player.ActiveChoice switch
      {
        PlayerSelectChoice psc => new PlayerSelectChoiceResult { SelectedCards = [], IsDeclined = true },
        PlayerArrangeChoice pac => new PlayerArrangeChoiceResult { ArrangedCards = [], IsDeclined = true },
        PlayerCategorizeChoice pcc => new PlayerCategorizeChoiceResult { CategorizedCards = [], IsDeclined = true },
        PlayerReactChoice prc => new PlayerReactChoiceResult { ChosenReaction = null, IsDeclined = true },
        _ => throw new NotImplementedException()
      };

      var newGameState = GameLogic.ProcessEffectStack(game, result);

      await _gameService.UpdateGameAsync(newGameState);

      await UpdateAllAsync(gameId);
    }
  }

  public async Task UndoAsync(string gameId)
  {
    string playerId = GetPlayerId();
    await _gameService.UndoAsync(playerId, gameId);
    await UpdateAllAsync(gameId);
  }

  private async Task UpdateAllAsync(string gameId)
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

  private async Task AdvertiseGameAsync(Game game)
  {
    await Clients.All.SendAsync("gameCreated", game);
  }

  private string GetPlayerId() => (string)Context.GetHttpContext()!.Items["playerId"]!;
}