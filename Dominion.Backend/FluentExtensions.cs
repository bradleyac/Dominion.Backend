using Fluent;
using Microsoft.AspNetCore.Mvc;
using static Fluent.EffectSequence;

namespace Dominion.Backend;

public static class FluentExtensions
{
  public static EffectSequence MoveSelectedCardsTo(this EffectSequence @this, CardZone to) => @this.ThenAfterSelect((state, choice, result, ctx) =>
    state.MoveBetweenZones(choice.Filter.From, to, ctx.PlayerId, result.SelectedCards));

  public static EffectSequence MoveRevealedCardsTo(this EffectSequence @this, CardZone to, bool privateReveal = false) => @this.ThenAfterSelect((state, choice, result, ctx) => state.MoveAllFromZone(privateReveal ? CardZone.PrivateReveal : CardZone.Reveal, to, ctx.PlayerId));

  public static EffectSequence GainSelectedCards(this EffectSequence @this, CardZone to = CardZone.Discard) => @this.ThenAfterSelect((state, choice, result, ctx) => state.GainCardsFromSupply(result.SelectedCards.CardIds(), ctx.PlayerId, to));

  public static EffectSequence ThenIfAnySelected(this EffectSequence @this, ThenAfterSelectDelegate then) => @this.ThenAfterSelect((state, choice, result, ctx) => result.SelectedCards.Length == 0 ? state : then(state, choice, result, ctx));
}