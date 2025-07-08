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
      KingdomCards: [.. kingdomCardInitData.Select(idCount => new CardPileState(MasterCardData.AllCards[idCount.Item1], idCount.Item2))],
      Players: [new PlayerState(hostPlayerId, [], CreateStartingDeck(), [], [], PlayerResources.Empty, null)],
      Trash: [],
      CurrentTurn: 1,
      CurrentPlayer: 0,
      ActivePlayer: 0,
      Phase: Phase.Action,
      Log: [],
      ResumeState: null
    );
  }

  public static GameState AddPlayer(GameState game, string playerId) => game with { Players = [.. game.Players, new PlayerState(playerId, [], CreateStartingDeck(), [], [], PlayerResources.Empty, null)] };
  private static CardInstance[] CreateStartingDeck() => MasterCardData.StartingDeck.SelectMany(deck => Enumerable.Range(1, deck.Item2).Select(_ => new CardInstance(MasterCardData.AllCards[deck.Item1]))).ToArray();
}