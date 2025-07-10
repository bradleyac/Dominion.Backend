using Dominion.Backend;
using Stateless.Graph;

namespace Fluent;

// SelectCards(new Filter(...)).Then(state, cards => {
//   state.GainCards(cards, to: hand)
// }),
// SelectCards(new Filter(...)).Then(State, cards => State.MoveCards(cards, from: hand, to: deck))
public static class Fluent
{
  public static EffectSequence Do(EffectSequence.DoDelegate @do) => new EffectSequence(@do);
  public static EffectSequence SelectCards(CardFilter filter) => new EffectSequence(filter);
  public static EffectSequence Then(this EffectSequence @this, EffectSequence.ThenDelegate then) => @this.Add(then);
  public static EffectSequence ThenSelect(this EffectSequence @this, EffectSequence.ThenSelectDelegate thenSelect) => @this.Add(thenSelect);
  public static EffectSequence ThenSelect(this EffectSequence @this, CardFilter filter) => @this.Add(filter);
  public static TargetedEffect ForEach(EffectTarget effectTarget, EffectSequence effect) => new TargetedEffect { Target = effectTarget, Effect = effect };

  public static EffectSequence Artisan = SelectCards(new CardFilter { From = CardZone.Supply, MaxCost = 5 })
    .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Hand, ctx.PlayerId, cards))
    .ThenSelect(new CardFilter { From = CardZone.Hand, MinCount = 1, MaxCount = 1 })
    .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Deck, ctx.PlayerId, cards));

  public static EffectSequence Forge = SelectCards(new CardFilter { From = CardZone.Hand })
    .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Trash, ctx.PlayerId, cards))
    .ThenSelect((state, filter, cards, ctx) => new CardFilter { From = CardZone.Supply, MinCost = cards.Aggregate(0, (acc, card) => acc + card.Card.Cost), MaxCost = cards.Aggregate(0, (acc, card) => acc + card.Card.Cost) })
    .Then((state, filter, cards, ctx) => state.MoveBetweenZones(filter.From, CardZone.Discard, ctx.PlayerId, cards));

  public static EffectSequence Mint = SelectCards(new CardFilter { From = CardZone.Hand, Types = [CardType.Treasure], })
    .Then((state, filter, cards, ctx) => state.MoveBetweenZones(CardZone.Supply, CardZone.Discard, ctx.PlayerId, cards));

  public static TargetedEffect Bandit = ForEach(EffectTarget.Opps, Do((state, ctx) => state.MoveBetweenZones(CardZone.Deck, CardZone.Reveal, ctx.PlayerId, state.GetPlayer(ctx.PlayerId).Deck.Take(2).ToArray()))
    .ThenSelect(new CardFilter { Types = [CardType.Treasure], From = CardZone.Reveal, NotId = MasterCardData.CardIDs.Copper })
    .Then((state, filter, cards, ctx) => state
      .MoveBetweenZones(filter.From, CardZone.Trash, ctx.PlayerId, cards)
      .MoveAllFromZone(filter.From, CardZone.Discard, ctx.PlayerId)));
}

public record EffectContext
{
  public required string PlayerId { get; set; }
}

public abstract class FluentEffect { };

public class TargetedEffect : FluentEffect
{
  public required EffectTarget Target { get; set; }
  public required EffectSequence Effect { get; set; }
}

public class EffectSequence : FluentEffect
{
  public delegate GameState DoDelegate(GameState gameState, EffectContext context);
  public delegate GameState ThenDelegate(GameState gameState, CardFilter filter, CardInstance[] cards, EffectContext context);
  public delegate CardFilter ThenSelectDelegate(GameState gameState, CardFilter filter, CardInstance[] cards, EffectContext context);

  public List<object> Effects { get; set; } = [];

  public EffectSequence(CardFilter filter)
  {
    Effects.Add(filter);
  }

  public EffectSequence(DoDelegate @do)
  {
    Effects.Add(@do);
  }

  public EffectSequence Add(ThenDelegate then)
  {
    Effects.Add(then);
    return this;
  }

  public EffectSequence Add(ThenSelectDelegate thenSelect)
  {
    Effects.Add(thenSelect);
    return this;
  }

  public EffectSequence Add(CardFilter filter)
  {
    Effects.Add(filter);
    return this;
  }

  public EffectSequence Add(DoDelegate @do)
  {
    Effects.Add(@do);
    return this;
  }
}