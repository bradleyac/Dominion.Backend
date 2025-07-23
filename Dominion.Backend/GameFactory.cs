using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dominion.Backend;

public static class GameFactory
{
  public static GameState CreateGameState(string hostPlayerId)
  {
    var startingSet = MasterCardData.StartingPiles.Select(idCount => idCount.Item1).ToHashSet();
    var randomCardPool = MasterCardData.AllCards.Keys.Where(id => !startingSet.Contains(id)).ToList();
    (int, int)[] kingdomCardInitData = [
      .. MasterCardData.StartingPiles,
      .. randomCardPool
        .Shuffle()
        .Take(10)
        .Select(id => (id, 10))
    ];

    return new GameState(
      GameId: Guid.NewGuid().ToString(),
      GameStarted: false,
      GameResult: null,
      KingdomCards: [.. kingdomCardInitData.Select(idCount => new CardPileState(MasterCardData.AllCards[idCount.Item1], idCount.Item2))],
      Players: [new PlayerState(hostPlayerId, 0, [], CreateStartingDeck(), [], [], [], PlayerResources.Empty, null)],
      Trash: [],
      Reveal: [],
      CurrentTurn: 1,
      CurrentPlayer: 0,
      ActivePlayerId: null,
      Phase: Phase.Action,
      Log: [],
      EffectStack: []
    );
  }

  public static GameState AddPlayer(GameState game, string playerId) => game with { Players = [.. game.Players, new PlayerState(playerId, game.Players.Length, [], CreateStartingDeck(), [], [], [], PlayerResources.Empty, null)] };
  private static CardInstance[] CreateStartingDeck() => MasterCardData.StartingDeck.SelectMany(deck => Enumerable.Range(1, deck.Item2).Select(_ => new CardInstance(MasterCardData.AllCards[deck.Item1]))).ToArray();
}