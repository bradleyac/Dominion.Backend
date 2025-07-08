namespace Dominion.Backend;

public static class EffectBuilder
{
  public static SelectBuilder Select(EffectTarget target, CardFilter filter)
  {
    return new SelectBuilder(target, filter);
  }
}

public class SelectBuilder(EffectTarget target, CardFilter filter)
{
  public SelectEffect Then(Func<GameState, CardBatch, GameState> action)
  {
    return new SelectEffect(target, filter, action);
  }
}

public class SelectEffect(EffectTarget target, CardFilter filter, Func<GameState, CardBatch, GameState> then)
{
  public EffectTarget target { get; set; } = target;
  public CardFilter Filter { get; set; } = filter;
  public Func<GameState, CardBatch, GameState> Then { get; set; } = then;
};

public static partial class GameStateExtensions
{
  public static PlayerSpecificEffect Gain(CardBatch cards, CardZone to = CardZone.Discard)
  {
    return (game, playerId) => game.RemoveFromCardZone(cards.Provenance, playerId, cards.Cards).AddToCardZone(to, playerId, cards.Cards);
  }

  // public static GameState Me(PlayerSpecificEffect effect)
  // {

  // }
}

// public static class PlayerStateExtensions
// {
//   public static PlayerState Gain(this PlayerState playerState, CardBatch cards)
//   {

//   }
// }

public delegate GameState PlayerSpecificEffect(GameState game, string playerId);

public record CardBatch(CardZone Provenance, CardInstance[] Cards);