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
          deck = [.. discard];
          deck = deck.Shuffle().ToList();
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
}