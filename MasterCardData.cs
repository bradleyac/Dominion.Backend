namespace Dominion.Backend;

using Microsoft.AspNetCore.Routing.Tree;
using Stateless.Graph;
using static Fluent.Fluent;

public static class MasterCardData
{
  public static class CardIDs
  {
    public const int Copper = 5;
    public const int Silver = 6;
    public const int Gold = 7;
    public const int Estate = 8;
    public const int Duchy = 9;
    public const int Province = 10;
    public const int Curse = 11;
  }

  public static readonly (int, int)[] StartingPiles = [
      (CardIDs.Copper, 60),
      (CardIDs.Silver, 40),
      (CardIDs.Gold, 30),
      (CardIDs.Estate, 8),
      (CardIDs.Duchy, 8),
      (CardIDs.Province, 8),
      (CardIDs.Curse, 10)
  ];

  public static readonly (int, int)[] StartingDeck = [
    (CardIDs.Copper, 7),
    (CardIDs.Estate, 3),
  ];

  public static readonly Dictionary<int, CardData> AllCards = new CardData[] {
    new(
        1, "Village", 3, 0,
        [CardType.Action],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainActions(2).DrawCards(1)))
        ]
    ),
    new(
        2, "Laboratory", 5, 0,
        [CardType.Action],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainActions(1).DrawCards(2)))
        ]
    ),
    new(
        3, "Festival", 5, 0,
        [CardType.Action],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainActions(2).GainBuys(1).GainCoins(2)))
        ]
    ),
    new(
        4, "Market", 5, 0,
        [CardType.Action],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainActions(1).DrawCards(1).GainBuys(1).GainCoins(1)))
        ]
    ),
    new(
        5, "Copper", 0, 0,
        [CardType.Treasure],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainCoins(1)))
        ]
    ),
    new(
        6, "Silver", 3, 0,
        [CardType.Treasure],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainCoins(2)))
        ]
    ),
    new(
        7, "Gold", 6, 0,
        [CardType.Treasure],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainCoins(3)))
        ]
    ),
    new(
        8, "Estate", 2, 1,
        [CardType.Victory],
        []
    ),
    new(
        9, "Duchy", 5, 3,
        [CardType.Victory],
        []
    ),
    new(
        10, "Province", 8, 6,
        [CardType.Victory],
        []
    ),
    new(
        11, "Curse", 0, -1,
        [CardType.Curse],
        []
    ),
    new(
        12, "Cellar", 1, 0,
        [CardType.Action],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.GainActions(1)))
          .ThenSelect(new CardFilter { From = CardZone.Hand })
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Discard, ctx.PlayerId, cards).UpdatePlayer(ctx.PlayerId, player => player.DrawCards(cards.Length)))
        ]
    ),
    new(
        13, "Artisan", 6, 0,
        [CardType.Action],
        [
          SelectCards(new CardFilter { From = CardZone.Supply, MaxCost = 5, MinCount = 1, MaxCount = 1})
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Hand, ctx.PlayerId, cards))
          .ThenSelect(new CardFilter { From = CardZone.Hand, MinCount = 1, MaxCount = 1})
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Deck, ctx.PlayerId, cards))
        ]
    ),
    new (
        14, "Chapel", 2, 0,
        [CardType.Action],
        [
          SelectCards(new CardFilter { From = CardZone.Hand, MaxCount = 4 })
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Trash, ctx.PlayerId, cards))
        ]
    ),
    new (
        15, "Witch", 5, 0,
        [CardType.Action, CardType.Attack],
        [
          Do((state, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.DrawCards(2))),
          ForEach(EffectTarget.Opps, Do((state, ctx) => state.GainCardFromSupply(CardZone.Discard, ctx.PlayerId, CardIDs.Curse)))
        ]
    ),
    new (
        16, "Workshop", 3, 0,
        [CardType.Action],
        [
          SelectCards(new CardFilter { From = CardZone.Supply, MaxCost = 4, MinCount = 1, MaxCount = 1 })
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Discard, ctx.PlayerId, cards))
        ]
    ),
    new (
        17, "Bandit", 5, 0,
        [CardType.Action, CardType.Attack],
        [
            Do((state, ctx) => state.GainCardFromSupply(CardZone.Discard, ctx.PlayerId, CardIDs.Gold)),
            ForEach(EffectTarget.Opps,
              Do((state, ctx) => state.MoveBetweenZones(CardZone.Deck, CardZone.Reveal, ctx.PlayerId, state.GetPlayer(ctx.PlayerId).Deck.Take(2).ToArray()))
              .ThenSelect(new CardFilter{ From = CardZone.Reveal, NotId = CardIDs.Copper, Types = [CardType.Treasure], MinCount = 1, MaxCount = 1 })
              .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Trash, ctx.PlayerId, cards).MoveAllFromZone(CardZone.Reveal, CardZone.Discard, ctx.PlayerId)))
        ]
    ),
    new (
      18, "Forge", 7, 0,
      [CardType.Action],
      [
        SelectCards(new CardFilter { From = CardZone.Hand })
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Trash, ctx.PlayerId, cards))
          .ThenSelect((state, filter, cards, ctx) => new CardFilter { From = CardZone.Supply, MinCost = cards.Aggregate(0, (acc, card) => acc + card.Card.Cost), MaxCost = cards.Aggregate(0, (acc, card) => acc + card.Card.Cost) })
          .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Discard, ctx.PlayerId, cards))
      ]
    )
  }.ToDictionary(card => card.Id);

}