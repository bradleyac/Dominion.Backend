using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using Fluent;
using static Dominion.Backend.CardZone;

namespace Dominion.Backend;

public static partial class GameStateExtensions
{
  public static PlayerState GetPlayer(this GameState @this, string playerId) => @this.Players.Single(p => p.Id == playerId);
  public static GameState RemoveFromCardZone(this GameState @this, CardZone from, string playerId, CardInstance cardInstance) => @this.RemoveFromCardZone(from, playerId, [cardInstance]);
  public static GameState RemoveFromCardZone(this GameState @this, CardZone from, string playerId, CardInstance[] cards)
  {
    HashSet<string> cardInstanceIds = cards.Select(card => card.Id).ToHashSet();
    HashSet<int> cardIds = cards.Select(card => card.Card.Id).ToHashSet();

    return from switch
    {
      Trash => @this with { Trash = [.. @this.Trash.Where(card => !cardInstanceIds.Contains(card.Id))] },
      Reveal => @this with { Reveal = [.. @this.Reveal.Where(card => !cardInstanceIds.Contains(card.Id))] },
      Supply => @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => cardIds.Contains(kc.Card.Id) ? kc with { Remaining = kc.Remaining - 1 } : kc)] },
      _ => @this.UpdatePlayer(playerId, player => from switch
      {
        Deck => player with { Deck = [.. player.Deck.Where(card => !cardInstanceIds.Contains(card.Id))] },
        Discard => player with { Discard = [.. player.Discard.Where(card => !cardInstanceIds.Contains(card.Id))] },
        Hand => player with { Hand = [.. player.Hand.Where(card => !cardInstanceIds.Contains(card.Id))] },
        Play => player with { Play = [.. player.Play.Where(card => !cardInstanceIds.Contains(card.Id))] },
        PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal.Where(card => !cardInstanceIds.Contains(card.Id))] },
        _ => player
      })
    };
  }

  public static GameState AddToCardZone(this GameState @this, CardZone to, string playerId, CardInstance cardInstance, bool skipReactions = false) => @this.AddToCardZone(to, playerId, [cardInstance], skipReactions);
  public static GameState AddToCardZone(this GameState @this, CardZone to, string playerId, IEnumerable<CardInstance> cards, bool skipReactions = false)
  {
    @this = to switch
    {
      Trash => @this with { Trash = [.. @this.Trash, .. cards] },
      Reveal => @this with { Reveal = [.. @this.Reveal, .. cards] },
      Supply => @this with { KingdomCards = [.. @this.KingdomCards.Select(kc => cards.CardIds().Contains(kc.Card.Id) ? kc with { Remaining = kc.Remaining + 1 } : kc)] },
      _ => @this.UpdatePlayer(playerId, player => to switch
      {
        Deck => player with { Deck = [.. cards, .. player.Deck] },
        Discard => player with { Discard = [.. player.Discard, .. cards] },
        Hand => player with { Hand = [.. player.Hand, .. cards] },
        Play => player with { Play = [.. player.Play, .. cards] },
        PrivateReveal => player with { PrivateReveal = [.. player.PrivateReveal, .. cards] },
        _ => player
      })
    };

    return skipReactions ? @this : to switch
    {
      Trash => @this.AddCardsMovedReactions(ReactionTrigger.Trash, [.. cards], Trash, playerId),
      Discard => @this.AddCardsMovedReactions(ReactionTrigger.Discard, [.. cards], Discard, playerId),
      _ => @this
    };
  }

  public static GameState GainCardFromSupply(this GameState @this, int cardId, string? playerId = null, CardZone to = Discard)
  {
    playerId ??= @this.ActivePlayerId!;
    var cardPile = @this.KingdomCards.FirstOrDefault(kc => kc.Card.Id == cardId && kc.Remaining > 0);
    if (cardPile is not null)
    {
      var cardInstance = CardInstance.CreateByCardId(cardId);
      return (@this with
      {
        KingdomCards = [.. @this.KingdomCards.Select(kc => kc.Card.Id == cardId ? kc with { Remaining = kc.Remaining - 1 } : kc)],
      }).AddToCardZone(to, playerId, cardInstance, skipReactions: true)
      .AddCardMovedReactions(ReactionTrigger.Gain, cardInstance, to, playerId);
    }
    return @this;
  }

  public static GameState GainCardsFromSupply(this GameState @this, IEnumerable<int> cardIds, string? playerId = null, CardZone to = Discard) => cardIds.Aggregate(@this, (stateAccumulator, cardId) => stateAccumulator.GainCardFromSupply(cardId, playerId, to));
  public static GameState MoveBetweenZones(this GameState @this, CardZone from, CardZone to, string playerId, CardInstance[] cards) => @this.RemoveFromCardZone(from, playerId, cards).AddToCardZone(to, playerId, cards);
  public static GameState MoveAllFromZone(this GameState @this, CardZone from, CardZone to, string playerId) => @this.AddToCardZone(to, playerId, @this.CardsInZone(from, playerId)).RemoveFromCardZone(from, playerId, @this.CardsInZone(from, playerId));
  public static GameState TrashCards(this GameState @this, CardZone from, string playerId, CardInstance[] cards) => @this.MoveBetweenZones(from, Trash, playerId, cards);
  public static CardInstance[] CardsInZone(this GameState @this, CardZone from, string playerId) => from switch
  {
    Deck => @this.GetPlayer(playerId).Deck,
    Discard => @this.GetPlayer(playerId).Discard,
    Hand => @this.GetPlayer(playerId).Hand,
    Play => @this.GetPlayer(playerId).Play,
    Reveal => @this.Reveal,
    PrivateReveal => @this.GetPlayer(playerId).PrivateReveal,
    Trash => @this.Trash,
    _ => throw new NotImplementedException(from.ToString())
  };

  public static GameState TakeCardsFromDeck(this GameState @this, string playerId, int count, out List<CardInstance> cards)
  {
    var player = @this.GetPlayer(playerId);

    cards = [];
    List<CardInstance> deck = [.. player.Deck];
    List<CardInstance> discard = [.. player.Discard];

    while (cards.Count < count)
    {
      if (deck is [var card, .. var rest])
      {
        cards.Add(card);
        deck = rest;
      }
      else if (discard.Count > 0)
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

    return @this.UpdatePlayer(playerId, player => player with { Deck = [.. deck], Discard = [.. discard] });
  }

  public static GameState RevealCardsFromDeck(this GameState @this, string playerId, int count) => @this.TakeCardsFromDeck(playerId, count, out var cards).AddToCardZone(Reveal, playerId, cards);
  public static GameState PrivatelyRevealCardsFromDeck(this GameState @this, string playerId, int count) => @this.TakeCardsFromDeck(playerId, count, out var cards).AddToCardZone(PrivateReveal, playerId, cards);
  public static GameState DiscardCardsFromDeck(this GameState @this, string playerId, int count) => @this.TakeCardsFromDeck(playerId, count, out var cards).AddToCardZone(Discard, playerId, cards);

  public static GameState UpdatePlayer(this GameState @this, string playerId, Func<PlayerState, PlayerState> updateFunc) => @this with
  {
    Players = [.. @this.Players.Select(player => player.Id == playerId ? updateFunc(player) : player)]
  };

  public static string[] GetTargets(this GameState @this, string ownerId, EffectTarget target) => target switch
  {
    EffectTarget.All => @this.Players.Rotate(@this.GetPlayer(ownerId).Index).Select(p => p.Id).ToArray(),
    EffectTarget.Opps => @this.Players.Rotate(@this.GetPlayer(ownerId).Index).Where(p => p.Id != ownerId).Select(p => p.Id).ToArray(),
    EffectTarget.Me => [ownerId],
    _ => throw new NotImplementedException()
  };

  public static bool DoAnyCardsMatch(this GameState @this, CardFilter filter, string playerId)
  {
    if (filter.MaxCount < 1)
    {
      return false; // No cards can match if max count is less than 1.
    }

    var player = @this.GetPlayer(playerId);

    return filter.From switch
    {
      Deck => player.Deck.Any(matches),
      Discard => player.Discard.Any(matches),
      Hand => player.Hand.Any(matches),
      Play => player.Play.Any(matches),
      PrivateReveal => player.PrivateReveal.Any(matches),
      Reveal => @this.Reveal.Any(matches),
      Supply => @this.KingdomCards.Any(cardPile => cardPile.Card.Matches(filter)),
      Trash => @this.Trash.Any(matches),
      _ => throw new NotImplementedException()
    };

    bool matches(CardInstance card) => card.Card.Matches(filter);
  }

  public static GameState UpdatePlayerChoice(this GameState @this, EffectContext context, PlayerChoice newChoice) => @this.UpdatePlayer(context.PlayerId, player => player with { ActiveChoice = newChoice }) with { ActivePlayerId = context.PlayerId };

  public static GameState AddCardsMovedReactions(this GameState @this, ReactionTrigger trigger, CardInstance[] movedCards, CardZone movedCardLocation, string ownerId)
  {
    foreach (var movedCard in movedCards)
    {
      @this = @this.AddCardMovedReactions(trigger, movedCard, movedCardLocation, ownerId);
    }
    return @this;
  }

  public static GameState AddCardMovedReactions(this GameState @this, ReactionTrigger trigger, CardInstance movedCard, CardZone movedCardLocation, string ownerId)
  {
    (string OwnerId, PlayerChoice Choice)[] choices = @this.Players.Rotate(@this.GetPlayer(ownerId).Index)
      .Select(player => GetReactChoiceForPlayer(player, trigger, "", ownerId, movedCard, movedCardLocation))
      .Where(ownerChoice => ownerChoice.Choice is not null)
      .Select(ownerChoice => (ownerChoice.OwnerId, (PlayerChoice)ownerChoice.Choice!))
      .ToArray();

    // Reverse the order of the reactions so that the one that should go first is on top of the stack.
    return @this with { EffectStack = [.. @this.EffectStack, .. choices.Select(c => new PendingEffect { Effects = [new EffectSequence(c.Choice)], OwnerId = c.OwnerId }).Reverse()] };
  }

  public static GameState AddCardPlayedReactions(this GameState @this, CardInstance playedCard, CardZone playedCardLocation, string triggeringEffectId, string ownerId)
  {
    (string OwnerId, PlayerChoice Choice)[] choices = @this.Players.Rotate(@this.GetPlayer(ownerId).Index)
      .Select(player => GetReactChoiceForPlayer(player, ReactionTrigger.Play, triggeringEffectId, ownerId, playedCard, playedCardLocation))
      .Where(ownerChoice => ownerChoice.Choice is not null)
      .Select(ownerChoice => (ownerChoice.OwnerId, (PlayerChoice)ownerChoice.Choice!))
      .ToArray();

    // Reverse the order of the reactions so that the one that should go first is on top of the stack.
    return @this with { EffectStack = [.. @this.EffectStack, .. choices.Select(c => new PendingEffect { Effects = [new EffectSequence(c.Choice)], OwnerId = c.OwnerId }).Reverse()] };
  }

  public static GameState AddCardPlayedPostReactions(this GameState @this, CardInstance playedCard, CardZone playedCardLocation, string triggeringEffectId, string ownerId)
  {
    (string OwnerId, PlayerChoice Choice)[] choices = @this.Players.Rotate(@this.GetPlayer(ownerId).Index)
      .Select(player => GetReactChoiceForPlayer(player, ReactionTrigger.Play, triggeringEffectId, ownerId, playedCard, playedCardLocation))
      .Where(ownerChoice => ownerChoice.Choice is not null)
      .Select(ownerChoice => (ownerChoice.OwnerId, (PlayerChoice)ownerChoice.Choice!))
      .ToArray();

    // Reverse the order of the reactions so that the one that should go first is on top of the stack.
    return @this with { EffectStack = [.. @this.EffectStack, .. choices.Select(c => new PendingEffect { Effects = [new EffectSequence(c.Choice)], OwnerId = c.OwnerId }).Reverse()] };
  }

  public static GameState AddStartOfTurnReactions(this GameState @this)
  {
    var currentPlayer = @this.Players[@this.CurrentPlayer];

    return GetReactChoiceForPlayer(currentPlayer, ReactionTrigger.StartOfTurn, "", currentPlayer.Id, null, null) switch
    {
      (_, null) => @this,
      (string ownerId, PlayerReactChoice choice) => @this with { EffectStack = [.. @this.EffectStack, new PendingEffect { Effects = [new EffectSequence(choice)], OwnerId = ownerId }] },
    };
  }

  public static GameState AddCardPlayedPostReactionTriggers(this GameState @this, CardInstance playedCard, string playerId)
  {
    var triggers = GetPostReactionTriggersForPlayer(@this, playerId, ReactionTrigger.Play, playerId, playedCard);
    var effectList = triggers.Select(t => new PendingEffect { Effects = [.. t.Effects], OwnerId = playerId });
    var consumedTriggers = triggers.Where(t => t.ConsumedOnUse);

    return (@this with
    {
      EffectStack = [.. @this.EffectStack, .. effectList]
    }).UpdatePlayer(playerId, player => player with
    {
      AmbientTriggers = [.. player.AmbientTriggers.Except(consumedTriggers)]
    });
  }

  public static bool IsCardInZone(this GameState @this, string playerId, string cardInstanceId, CardZone zone) => @this.CardsInZone(zone, playerId).Any(c => c.Id == cardInstanceId);

  private static (string OwnerId, PlayerReactChoice? Choice) GetReactChoiceForPlayer(PlayerState player, ReactionTrigger trigger, string triggeringEffectId, string triggerOwnerId, CardInstance? triggerCard, CardZone? triggerCardLocation)
  {
    CardInstance[] cardsToCheck = triggerCard is not null ? [.. player.Hand, triggerCard] : player.Hand;

    var reactionContext = new ReactionContext { ReactingPlayerId = player.Id, Trigger = trigger, TriggerOwnerId = triggerOwnerId, TriggerCard = triggerCard };
    var reactionCards = DedupeReactions(cardsToCheck.Where(c => c.Card.CanReact(reactionContext with { ReactingCardId = c.Id })));

    return reactionCards switch
    {
      null or [] => (OwnerId: player.Id, Choice: null),
      var list => (OwnerId: player.Id, Choice: new PlayerReactChoice
      {
        IsForced = false,
        Prompt = $"Choose a reaction{(triggerCard is not null ? $" (responding to {triggerCard.Card.Name})" : "")}",
        EffectReferences = reactionCards.Select(card => new EffectReference(card.ToDto((card.Id == (triggerCard?.Id ?? "")) ? triggerCardLocation!.Value : Hand), card.Card.ReactionPrompt)).ToArray(),
        TriggeringEffectId = triggeringEffectId,
        TriggerOwnerId = triggerOwnerId
      }),
    };
  }

  private static AmbientTrigger[] GetPostReactionTriggersForPlayer(GameState state, string playerId, ReactionTrigger trigger, string triggerOwnerId, CardInstance? triggerCard)
  {
    var player = state.GetPlayer(playerId);
    AmbientTrigger[] triggers = player.AmbientTriggers;

    var reactionContext = new ReactionContext { ReactingPlayerId = player.Id, Trigger = trigger, TriggerOwnerId = triggerOwnerId, TriggerCard = triggerCard };
    var triggeredTriggers = triggers.Where(t => t.CanTrigger(state, reactionContext)).ToArray();

    return triggeredTriggers;
  }

  private static CardInstance[] DedupeReactions(IEnumerable<CardInstance> reactionCards) => reactionCards
    .GroupBy(r => (r.Card.Id, r.Card.ReactionIsIdempotent))
    .SelectMany(grouping => grouping.Key.ReactionIsIdempotent switch
    {
      true => [grouping.First()],
      false => grouping.AsEnumerable()
    }).ToArray();
}