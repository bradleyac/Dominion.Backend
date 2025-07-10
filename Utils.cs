using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Dominion.Backend;

public static class Utils
{
  public static PlayerState GetPlayer(this GameState @this, string playerId) => @this.Players.Single(p => p.Id == playerId);

  public static CardInstance[] GetCardZone(this CardZone from, string playerId, GameState state) => from switch
  {
    CardZone.Deck => state.GetPlayer(playerId).Deck,
    CardZone.Discard => state.GetPlayer(playerId).Discard,
    CardZone.Hand => state.GetPlayer(playerId).Hand,
    CardZone.Play => state.GetPlayer(playerId).Play,
    CardZone.Trash => state.Trash,
    CardZone.PrivateReveal => state.GetPlayer(playerId).PrivateReveal,
    CardZone.Reveal => state.Reveal,
    _ => throw new NotImplementedException()
  };

  public static GameState RemoveFromCardZone(this GameState @this, CardZone from, string playerId, CardInstance cardInstance)
  {
    if (from == CardZone.Trash)
    {
      return @this with { Trash = [.. @this.Trash.Where(card => card.Id != cardInstance.Id)] };
    }

    if (from == CardZone.Reveal)
    {
      return @this with { Reveal = [.. @this.Reveal.Where(card => card.Id != cardInstance.Id)] };
    }

    if (from == CardZone.Supply)
    {
      return @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => kc.Card.Id == cardInstance.Card.Id ? kc with { Remaining = kc.Remaining - 1 } : kc)] };
    }

    var player = @this.GetPlayer(playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? from switch {
        CardZone.Deck => player with { Deck = [.. player.Deck.Where(card => card.Id != cardInstance.Id)]},
        CardZone.Discard => player with { Discard = [.. player.Discard.Where(card => card.Id != cardInstance.Id)]},
        CardZone.Hand => player with { Hand = [.. player.Hand.Where(card => card.Id != cardInstance.Id)]},
        CardZone.Play => player with { Play = [.. player.Play.Where(card => card.Id != cardInstance.Id)]},
        CardZone.PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal.Where(card => card.Id != cardInstance.Id)]},
        _ => player
      } : player)]
    };
  }

  public static GameState RemoveFromCardZone(this GameState @this, CardZone from, string playerId, CardInstance[] cards)
  {
    HashSet<string> cardInstanceIds = cards.Select(card => card.Id).ToHashSet();
    HashSet<int> cardIds = cards.Select(card => card.Card.Id).ToHashSet();

    if (from == CardZone.Trash)
    {
      return @this with { Trash = [.. @this.Trash.Where(card => !cardInstanceIds.Contains(card.Id))] };
    }

    if (from == CardZone.Reveal)
    {
      return @this with { Reveal = [.. @this.Reveal.Where(card => !cardInstanceIds.Contains(card.Id))] };
    }

    if (from == CardZone.Supply)
    {
      return @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => cardIds.Contains(kc.Card.Id) ? kc with { Remaining = kc.Remaining - 1 } : kc)] };
    }

    var player = @this.GetPlayer(playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? from switch {
        CardZone.Deck => player with { Deck = [.. player.Deck.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.Discard => player with { Discard = [.. player.Discard.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.Hand => player with { Hand = [.. player.Hand.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.Play => player with { Play = [.. player.Play.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal.Where(card => !cardInstanceIds.Contains(card.Id))]},
        _ => player
      } : player)]
    };
  }

  public static GameState AddToCardZone(this GameState @this, CardZone to, string playerId, CardInstance cardInstance)
  {
    if (to == CardZone.Trash)
    {
      return @this with { Trash = [.. @this.Trash, cardInstance] };
    }

    if (to == CardZone.Supply)
    {
      return @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => kc.Card.Id == cardInstance.Card.Id ? kc with { Remaining = kc.Remaining + 1 } : kc)] };
    }

    var player = @this.GetPlayer(playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? to switch {
        CardZone.Deck => player with { Deck = [..player.Deck, cardInstance]},
        CardZone.Discard => player with { Discard = [.. player.Discard, cardInstance]},
        CardZone.Hand => player with { Hand = [.. player.Hand, cardInstance]},
        CardZone.Play => player with { Play = [.. player.Play, cardInstance]},
        CardZone.PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal, cardInstance]},
        _ => player
      } : player)]
    };
  }

  public static GameState AddToCardZone(this GameState @this, CardZone to, string playerId, CardInstance[] cards)
  {
    HashSet<string> cardInstanceIds = cards.Select(card => card.Id).ToHashSet();
    HashSet<int> cardIds = cards.Select(card => card.Card.Id).ToHashSet();

    if (to == CardZone.Trash)
    {
      return @this with { Trash = [.. @this.Trash, .. cards] };
    }

    if (to == CardZone.Reveal)
    {
      return @this with { Reveal = [.. @this.Reveal, .. cards] };
    }

    if (to == CardZone.Supply)
    {
      return @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => cardIds.Contains(kc.Card.Id) ? kc with { Remaining = kc.Remaining + 1 } : kc)] };
    }

    var player = @this.Players.Single(player => player.Id == playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? to switch {
        CardZone.Deck => player with { Deck = [..player.Deck, .. cards]},
        CardZone.Discard => player with { Discard = [.. player.Discard, .. cards]},
        CardZone.Hand => player with { Hand = [.. player.Hand, .. cards]},
        CardZone.Play => player with { Play = [.. player.Play, .. cards]},
        CardZone.PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal, .. cards]},
        _ => player
      } : player)]
    };
  }

  public static GameState GainCardFromSupply(this GameState @this, CardZone to, string playerId, int cardId)
  {
    var cardPile = @this.KingdomCards.FirstOrDefault(kc => kc.Card.Id == cardId && kc.Remaining > 0);
    if (cardPile is not null)
    {
      return (@this with
      {
        KingdomCards = [.. @this.KingdomCards.Select(kc => kc.Card.Id == cardId ? kc with { Remaining = kc.Remaining - 1 } : kc)],
      }).AddToCardZone(to, playerId, CardInstance.CreateByCardId(cardId));
    }
    return @this;
  }
  public static GameState MoveBetweenZones(this GameState @this, CardZone from, CardZone to, string playerId, CardInstance[] cards) => @this.RemoveFromCardZone(from, playerId, cards).AddToCardZone(to, playerId, cards);
  // TODO: Brittle?
  public static GameState MoveAllFromZone(this GameState @this, CardZone from, CardZone to, string playerId) => @this.AddToCardZone(to, playerId, @this.CardsInZone(from, playerId)).RemoveFromCardZone(from, playerId, @this.CardsInZone(from, playerId));
  public static GameState TrashCards(this GameState @this, CardZone from, string playerId, CardInstance[] cards) => @this.MoveBetweenZones(from, CardZone.Trash, playerId, cards);
  public static CardInstance[] CardsInZone(this GameState @this, CardZone from, string playerId) => from switch
  {
    CardZone.Deck => @this.GetPlayer(playerId).Deck,
    CardZone.Discard => @this.GetPlayer(playerId).Discard,
    CardZone.Hand => @this.GetPlayer(playerId).Hand,
    CardZone.Play => @this.GetPlayer(playerId).Play,
    CardZone.Reveal => @this.Reveal,
    CardZone.PrivateReveal => @this.GetPlayer(playerId).PrivateReveal,
    CardZone.Trash => @this.Trash,
    _ => throw new NotImplementedException(from.ToString())
  };

  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> @this)
  {
    List<T> shuffled = [.. @this];
    for (int i = shuffled.Count - 1; i > 0; i--)
    {
      var j = (int)Math.Floor(Random.Shared.NextSingle() * i);
      (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
    }
    return shuffled;
  }

  public static bool IsBuyPhase(this Phase @this) => @this == Phase.Buy || @this == Phase.BuyOrPlay;
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
      if (deck.Count > 0)
      {
        cardsDrawn++;
        var drawnCard = deck[0];
        deck.RemoveAt(0);
        hand.Add(drawnCard);
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

  public static GameState UpdatePlayer(this GameState @this, string playerId, Func<PlayerState, PlayerState> updateFunc) => @this with
  {
    Players = [.. @this.Players.Select(player => player.Id == playerId ? updateFunc(player) : player)]
  };

  public static string[] GetTargets(this GameState @this, EffectTarget target) => target switch
  {
    EffectTarget.All => [.. @this.Players.Skip(@this.CurrentPlayer).Select(p => p.Id), .. @this.Players.Take(@this.CurrentPlayer).Select(p => p.Id)],
    EffectTarget.Opps => [.. @this.Players.Skip(@this.CurrentPlayer + 1).Select(p => p.Id), .. @this.Players.Take(@this.CurrentPlayer).Select(p => p.Id)],
    _ => throw new NotImplementedException()
  };

  public static bool DoAnyCardsMatch(this GameState @this, CardFilter filter, string playerId)
  {
    var player = @this.GetPlayer(playerId);

    return filter.From switch
    {
      CardZone.Deck => player.Deck.Any(card => DoesCardMatchFilter(card.Card, filter)),
      CardZone.Discard => player.Discard.Any(card => DoesCardMatchFilter(card.Card, filter)),
      CardZone.Hand => player.Hand.Any(card => DoesCardMatchFilter(card.Card, filter)),
      CardZone.Play => player.Play.Any(card => DoesCardMatchFilter(card.Card, filter)),
      CardZone.PrivateReveal => player.PrivateReveal.Any(card => DoesCardMatchFilter(card.Card, filter)),
      CardZone.Reveal => @this.Reveal.Any(card => DoesCardMatchFilter(card.Card, filter)),
      CardZone.Supply => @this.KingdomCards.Any(cardPile => DoesCardMatchFilter(cardPile.Card, filter)),
      CardZone.Trash => @this.Trash.Any(card => DoesCardMatchFilter(card.Card, filter)),
      _ => throw new NotImplementedException()
    };
  }

  // Doesn't take Zone into consideration--assumes in same zone.
  public static bool DoesCardMatchFilter(CardData card, CardFilter filter)
  {
    return (filter.NotId is null || card.Id != filter.NotId)
      && (filter.Types is null or [] || card.Types.Any(c => filter.Types!.Contains(c)))
      && (filter.MinCost is null || card.Cost >= filter.MinCost)
      && (filter.MaxCost is null || card.Cost <= filter.MaxCost);
  }
}