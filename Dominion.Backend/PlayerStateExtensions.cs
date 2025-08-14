namespace Dominion.Backend;

public static class PlayerStateExtensions
{
  public static PlayerState GainActions(this PlayerState @this, int count) => @this with { Resources = @this.Resources with { Actions = @this.Resources.Actions + count } };
  public static PlayerState GainCoins(this PlayerState @this, int count) => @this with { Resources = @this.Resources with { Coins = @this.Resources.Coins + count } };
  public static PlayerState GainBuys(this PlayerState @this, int count) => @this with { Resources = @this.Resources with { Buys = @this.Resources.Buys + count } };
  public static PlayerState GainVillagers(this PlayerState @this, int count) => @this with { Resources = @this.Resources with { Villagers = @this.Resources.Villagers + count } };
  public static PlayerState GainCoffers(this PlayerState @this, int count) => @this with { Resources = @this.Resources with { Coffers = @this.Resources.Coffers + count } };
  public static PlayerState GainPoints(this PlayerState @this, int count) => @this with { Resources = @this.Resources with { Points = @this.Resources.Points + count } };
  public static PlayerState DrawCards(this PlayerState @this, int count)
  {
    int cardsDrawn = 0;

    List<CardInstance> hand = [.. @this.Hand];
    List<CardInstance> deck = [.. @this.Deck];
    List<CardInstance> discard = [.. @this.Discard];

    while (cardsDrawn < count)
    {
      if (deck is [var card, .. var rest])
      {
        cardsDrawn++;
        deck = rest;
        hand.Add(card);
      }
      else
      {
        if (discard.Count > 0)
        {
          deck = [.. discard.Shuffle()];
          discard = [];
          continue;
        }
        else
        {
          break;
        }
      }
    }

    return @this with { Deck = [.. deck], Hand = [.. hand], Discard = [.. discard] };
  }
  public static PlayerState GroupHand(this PlayerState @this) => @this with { Hand = @this.Hand.GroupBy(c => c.Card.Id).OrderBy(g => g.Key, Comparer<int>.Create(CompareCards)).SelectMany(x => x).ToArray() };
  public static bool HasActionsToPlay(this PlayerState @this) => @this.Hand.Any(c => c.Card.Types.Contains(CardType.Action));

  private static int CompareCards(int cardIdA, int cardIdB)
  {
    var cardA = MasterCardData.AllCards[cardIdA];
    var cardB = MasterCardData.AllCards[cardIdB];

    if (cardA.Types.Contains(CardType.Victory) && !cardB.Types.Contains(CardType.Victory))
    {
      return -1;
    }
    else if (!cardA.Types.Contains(CardType.Victory) && cardB.Types.Contains(CardType.Victory))
    {
      return 1;
    }
    else if (cardA.Types.Contains(CardType.Treasure) && !cardB.Types.Contains(CardType.Treasure))
    {
      return -1;
    }
    else if (!cardA.Types.Contains(CardType.Treasure) && cardB.Types.Contains(CardType.Treasure))
    {
      return 1;
    }
    else
    {
      int costCompare = cardA.Cost.CompareTo(cardB.Cost);
      return costCompare == 0 ? cardA.Name.CompareTo(cardB.Name) : costCompare;
    }
  }
}