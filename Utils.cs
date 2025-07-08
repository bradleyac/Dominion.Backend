using System.Data;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Dominion.Backend;

public static class Utils
{
  public static PlayerState GetPlayer(this GameState @this, string playerId) => @this.Players.Single(p => p.Id == playerId);

  public static CardInstance[] GetCardZone(this CardZone from, string playerId, GameState state) => from switch
  {
    CardZone.Deck => state.Players.Single(player => player.Id == playerId).Deck,
    CardZone.Discard => state.Players.Single(player => player.Id == playerId).Discard,
    CardZone.Hand => state.Players.Single(player => player.Id == playerId).Hand,
    CardZone.Play => state.Players.Single(player => player.Id == playerId).Play,
    CardZone.Trash => state.Trash,
    _ => []
  };

  public static GameState RemoveFromCardZone(this GameState @this, CardZone from, string playerId, CardInstance cardInstance)
  {
    if (from == CardZone.Trash)
    {
      return @this with { Trash = [.. @this.Trash.Where(card => card.Id != cardInstance.Id)] };
    }

    if (from == CardZone.Supply)
    {
      return @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => kc.Card.Id == cardInstance.Card.Id ? kc with { Remaining = kc.Remaining - 1 } : kc)] };
    }

    var player = @this.Players.Single(player => player.Id == playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? from switch {
        CardZone.Deck => player with { Deck = [.. player.Deck.Where(card => card.Id != cardInstance.Id)]},
        CardZone.Discard => player with { Discard = [.. player.Discard.Where(card => card.Id != cardInstance.Id)]},
        CardZone.Hand => player with { Hand = [.. player.Hand.Where(card => card.Id != cardInstance.Id)]},
        CardZone.Play => player with { Play = [.. player.Play.Where(card => card.Id != cardInstance.Id)]},
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

    if (from == CardZone.Supply)
    {
      return @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => cardIds.Contains(kc.Card.Id) ? kc with { Remaining = kc.Remaining - 1 } : kc)] };
    }

    var player = @this.Players.Single(player => player.Id == playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? from switch {
        CardZone.Deck => player with { Deck = [.. player.Deck.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.Discard => player with { Discard = [.. player.Discard.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.Hand => player with { Hand = [.. player.Hand.Where(card => !cardInstanceIds.Contains(card.Id))]},
        CardZone.Play => player with { Play = [.. player.Play.Where(card => !cardInstanceIds.Contains(card.Id))]},
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

    var player = @this.Players.Single(player => player.Id == playerId);

    return @this with
    {
      Players = [.. @this.Players.Select(player => player.Id == playerId ? to switch {
        CardZone.Deck => player with { Deck = [..player.Deck, cardInstance]},
        CardZone.Discard => player with { Discard = [.. player.Discard, cardInstance]},
        CardZone.Hand => player with { Hand = [.. player.Hand, cardInstance]},
        CardZone.Play => player with { Play = [.. player.Play, cardInstance]},
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
        _ => player
      } : player)]
    };
  }

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

  public static PlayerState DrawCards(PlayerState player, int count)
  {
    int cardsDrawn = 0;

    List<CardInstance> hand = [.. player.Hand];
    List<CardInstance> deck = [.. player.Deck];
    List<CardInstance> discard = [.. player.Discard];

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

    return player with { Deck = [.. deck], Hand = [.. hand], Discard = [.. discard] };
  }
}