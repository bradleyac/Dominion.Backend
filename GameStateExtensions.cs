using Fluent;
using static Fluent.EffectSequence;

namespace Dominion.Backend;

public static partial class GameStateExtensions
{
  public static PlayerState GetPlayer(this GameState @this, string playerId) => @this.Players.Single(p => p.Id == playerId);
  public static GameState RemoveFromCardZone(this GameState @this, CardZone from, string playerId, CardInstance cardInstance) => @this.RemoveFromCardZone(from, playerId, [cardInstance]);
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

  public static GameState AddToCardZone(this GameState @this, CardZone to, string playerId, CardInstance cardInstance) => @this.AddToCardZone(to, playerId, [cardInstance]);
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
        CardZone.Deck => player with { Deck = [.. cards, ..player.Deck]},
        CardZone.Discard => player with { Discard = [.. player.Discard, .. cards]},
        CardZone.Hand => player with { Hand = [.. player.Hand, .. cards]},
        CardZone.Play => player with { Play = [.. player.Play, .. cards]},
        CardZone.PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal, .. cards]},
        _ => player
      } : player)]
    };
  }

  public static GameState GainCardFromSupply(this GameState @this, int cardId, string? playerId = null, CardZone to = CardZone.Discard)
  {
    playerId ??= @this.ActivePlayerId!;
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

  public static GameState GainCardsFromSupply(this GameState @this, IEnumerable<int> cardIds, string? playerId = null, CardZone to = CardZone.Discard)
  {
    GameState newState = @this;
    foreach (var cardId in cardIds)
    {
      newState = newState.GainCardFromSupply(cardId, playerId, to);
    }
    return newState;
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

  public static GameState RevealCardsFromDeck(this GameState @this, string playerId, int count)
  {
    int cardsRevealed = 0;

    var player = @this.GetPlayer(playerId);

    List<CardInstance> reveal = [.. @this.Reveal];
    List<CardInstance> deck = [.. player.Deck];
    List<CardInstance> discard = [.. player.Discard];

    while (cardsRevealed < count)
    {
      if (deck is [var card, .. var rest])
      {
        cardsRevealed++;
        deck = rest;
        reveal.Add(card);
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

    return @this.UpdatePlayer(playerId, player => player with { Deck = [.. deck], Discard = [.. discard] }) with { Reveal = [.. reveal], };
  }

  public static GameState PrivatelyRevealCardsFromDeck(this GameState @this, string playerId, int count)
  {
    int cardsRevealed = 0;

    var player = @this.GetPlayer(playerId);

    List<CardInstance> reveal = [.. player.PrivateReveal];
    List<CardInstance> deck = [.. player.Deck];
    List<CardInstance> discard = [.. player.Discard];

    while (cardsRevealed < count)
    {
      if (deck is [var card, .. var rest])
      {
        cardsRevealed++;
        deck = rest;
        reveal.Add(card);
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

    return @this.UpdatePlayer(playerId, player => player with { Deck = [.. deck], Discard = [.. discard], PrivateReveal = [.. reveal] });
  }

  public static GameState DiscardCardsFromDeck(this GameState @this, string playerId, int count)
  {
    int cardsRevealed = 0;

    var player = @this.GetPlayer(playerId);

    List<CardInstance> discard = [.. player.Discard];
    List<CardInstance> deck = [.. player.Deck];

    while (cardsRevealed < count)
    {
      if (deck is [var card, .. var rest])
      {
        cardsRevealed++;
        deck = rest;
        discard.Add(card);
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

    return @this.UpdatePlayer(playerId, player => player with { Deck = [.. deck], Discard = [.. discard] });
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

    if (filter.MaxCount < 1)
    {
      return false; // No cards can match if max count is less than 1.
    }

    return filter.From switch
    {
      CardZone.Deck => player.Deck.Any(card => card.Card.Matches(filter)),
      CardZone.Discard => player.Discard.Any(card => card.Card.Matches(filter)),
      CardZone.Hand => player.Hand.Any(card => card.Card.Matches(filter)),
      CardZone.Play => player.Play.Any(card => card.Card.Matches(filter)),
      CardZone.PrivateReveal => player.PrivateReveal.Any(card => card.Card.Matches(filter)),
      CardZone.Reveal => @this.Reveal.Any(card => card.Card.Matches(filter)),
      CardZone.Supply => @this.KingdomCards.Any(cardPile => cardPile.Card.Matches(filter)),
      CardZone.Trash => @this.Trash.Any(card => card.Card.Matches(filter)),
      _ => throw new NotImplementedException()
    };
  }
}