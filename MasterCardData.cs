namespace Dominion.Backend;

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
          new SimpleEffect{ Effect = "2action" },
          new SimpleEffect{ Effect = "1card" }
        ]
    ),
    new(
        2, "Laboratory", 5, 0,
        [CardType.Action],
        [
          new SimpleEffect { Effect = "2card" },
          new SimpleEffect { Effect = "1action" }
        ]
    ),
    new(
        3, "Festival", 5, 0,
        [CardType.Action],
        [
          new SimpleEffect { Effect = "2action" },
          new SimpleEffect { Effect = "1buy" },
          new SimpleEffect { Effect = "2coin" }
        ]
    ),
    new(
        4, "Market", 5, 0,
        [CardType.Action],
        [
          new SimpleEffect { Effect = "1card" },
          new SimpleEffect { Effect = "1action" },
          new SimpleEffect { Effect = "1buy" },
          new SimpleEffect { Effect = "1coin" }
        ]
    ),
    new(
        5, "Copper", 0, 0,
        [CardType.Treasure],
        [
          new SimpleEffect { Effect = "1coin" }
        ]
    ),
    new(
        6, "Silver", 3, 0,
        [CardType.Treasure],
        [
          new SimpleEffect { Effect = "2coin" }
        ]
    ),
    new(
        7, "Gold", 6, 0,
        [CardType.Treasure],
        [
          new SimpleEffect { Effect = "3coin" }
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
          new SimpleEffect { Effect = "1action" },
          new MoveCardsEffect{
            Target = EffectTarget.Me,
            To = CardZone.Discard,
            Filter = new CardFilter{ From = CardZone.Hand }
          },
          new SimpleEffect{ Effect = "4card"},
        ]
    ),
    new(
        13, "Artisan", 6, 0,
        [CardType.Action],
        [
          new MoveCardsEffect{
            Target = EffectTarget.Me,
            To = CardZone.Hand,
            Filter = new CardFilter{ From = CardZone.Supply, MaxCost = 5, MinCount = 1, MaxCount = 1 }
          },
          new MoveCardsEffect{
            Target = EffectTarget.Me,
            To = CardZone.Deck,
            Filter = new CardFilter{ From = CardZone.Hand, MinCount = 1, MaxCount = 1 }
          }
        ]
    ),
    new (
        14, "Chapel", 2, 0,
        [CardType.Action],
        [
          new MoveCardsEffect {
            Target = EffectTarget.Me,
            To = CardZone.Trash,
            Filter = new CardFilter{ From = CardZone.Hand, MinCount = 0, MaxCount = 4 }
          }
        ]
    ),
    new (
        15, "Witch", 5, 0,
        [CardType.Action, CardType.Attack],
        [
          new SimpleEffect { Effect = "2card" },
          new MoveCardsEffect{
            Target = EffectTarget.Opps,
            To = CardZone.Discard,
            Filter = new CardFilter { From = CardZone.Supply, CardId = CardIDs.Curse, MinCount = 1, MaxCount = 1 }
          }
        ]
    ),
    new (
        16, "Workshop", 3, 0,
        [CardType.Action],
        [
          new MoveCardsEffect{
            Target = EffectTarget.Me,
            To = CardZone.Discard,
            Filter = new CardFilter { From = CardZone.Supply, MaxCost = 4, MinCount = 1, MaxCount = 1 }
          }
        ]
    ),
    new (
        17, "Card Ten", 1, 0,
        [CardType.Action],
        [
            new SimpleEffect { Effect = "1action" },
            new SimpleEffect { Effect = "1card" }
        ]
    )
  }.ToDictionary(card => card.Id);

}