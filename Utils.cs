using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Fluent;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using static Fluent.EffectSequence;

namespace Dominion.Backend;

public static class Utils
{
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

  public static DoDelegate DoActivePlayer(Func<PlayerState, PlayerState> update) => (GameState state, EffectContext ctx) => state.UpdatePlayer(ctx.PlayerId, update);
  public static ThenAfterSelectDelegate ThenActivePlayer(Func<PlayerState, PlayerState> update) => (GameState state, PlayerSelectChoice choice, PlayerSelectChoiceResult result, EffectContext ctx) => state.UpdatePlayer(ctx.PlayerId, update);
  public static DoDelegate RevealTopN(int n, bool privateReveal = false) => (GameState state, EffectContext ctx) => privateReveal ? state.PrivatelyRevealCardsFromDeck(ctx.PlayerId, n) : state.RevealCardsFromDeck(ctx.PlayerId, n);

  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> @this)
  {
    T[] shuffled = [.. @this];
    for (int i = shuffled.Length - 1; i > 0; i--)
    {
      var j = (int)Math.Floor(Random.Shared.NextSingle() * i);
      (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
    }
    return shuffled;
  }

  public static bool IsBuyPhase(this Phase @this) => @this == Phase.Buy || @this == Phase.BuyOrPlay;

  // Doesn't take Zone into consideration--assumes in same zone.
  public static bool Matches(this CardPileState @this, CardFilter filter) => @this.Card.Matches(filter);
  public static bool Matches(this CardInstance @this, CardFilter filter) => @this.Card.Matches(filter);
  public static bool Matches(this CardData @this, CardFilter filter) =>
    (filter.NotId is null || @this.Id != filter.NotId)
    && (filter.CardId is null || @this.Id == filter.CardId)
    && (filter.Types is null or [] || @this.Types.Any(c => filter.Types!.Contains(c)))
    && (filter.MinCost is null || @this.Cost >= filter.MinCost)
    && (filter.MaxCost is null || @this.Cost <= filter.MaxCost);

  public static int SumBy<T>(this IEnumerable<T> @this, Func<T, int> valueSelector) => @this.Aggregate(0, (acc, val) => acc + valueSelector(val));

  public static ConcurrentDictionary<TKey, TValue> RemoveIf<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> @this, Func<TKey, bool> predicate) where TKey : notnull
  {
    foreach (var key in @this.Keys.ToArray())
    {
      if (predicate(key))
      {
        @this.Remove(key, out _);
      }
    }

    return @this;
  }
}