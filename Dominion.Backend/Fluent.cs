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
  public static EffectSequence SelectCards(EffectSequence.ThenSelectThunkDelegate choiceFunc) => new EffectSequence(choiceFunc);
  public static EffectSequence Then(this EffectSequence @this, EffectSequence.DoDelegate @do) => @this.Add(@do);
  public static EffectSequence Then(this EffectSequence @this, EffectSequence.ThenDelegate then) => @this.Add(then);
  public static EffectSequence ThenAfterSelect(this EffectSequence @this, Func<GameState, PlayerSelectChoice, PlayerSelectChoiceResult, EffectContext, GameState> then) => @this.Add((gameState, choice, result, context) => then(gameState, (PlayerSelectChoice)choice, (PlayerSelectChoiceResult)result, context));
  public static EffectSequence ThenSelect(this EffectSequence @this, EffectSequence.ThenSelectDelegate thenSelect) => @this.Add(thenSelect);
  public static EffectSequence ThenSelect(this EffectSequence @this, EffectSequence.ThenSelectThunkDelegate choiceFunc) => @this.Add(choiceFunc);

  public static EffectSequence ThenCategorize(this EffectSequence @this, EffectSequence.DoCategorizeDelegate categorizeFunc) => @this.Add(categorizeFunc);
  public static EffectSequence ThenCategorize(this EffectSequence @this, EffectSequence.ThenCategorizeDelegate categorizeFunc) => @this.Add(categorizeFunc);

  public static EffectSequence ThenArrange(this EffectSequence @this, EffectSequence.DoArrangeDelegate arrangeFunc) => @this.Add(arrangeFunc);
  public static EffectSequence ThenArrange(this EffectSequence @this, EffectSequence.ThenArrangeDelegate arrangeFunc) => @this.Add(arrangeFunc);

  public static LoopEffect Loop(Func<GameState, EffectContext, bool> loopCondition, EffectSequence effect) => new LoopEffect { LoopCondition = loopCondition, Effect = effect };
  public static TargetedEffect ForEach(EffectTarget effectTarget, EffectSequence effect) => new TargetedEffect { Target = effectTarget, Effect = effect };
}

public record EffectContext
{
  public required string PlayerId { get; init; }
}

public record PendingEffect
{
  public string Id { get; } = Guid.NewGuid().ToString();
  public required string OwnerId { get; init; }
  public required FluentEffect[] Effects { get; init; }
}

public abstract class FluentEffect { };

public class TargetedEffect : FluentEffect
{
  public required EffectTarget Target { get; set; }
  public required EffectSequence Effect { get; set; }
}

public class LoopEffect : FluentEffect
{
  public required Func<GameState, EffectContext, bool> LoopCondition { get; set; }
  public required EffectSequence Effect { get; set; }
}

public class EffectSequence : FluentEffect
{
  public delegate GameState DoDelegate(GameState gameState, EffectContext context);
  public delegate GameState ThenDelegate(GameState gameState, PlayerChoice choice, PlayerChoiceResult result, EffectContext context);
  public delegate GameState ThenAfterSelectDelegate(GameState gameState, PlayerSelectChoice choice, PlayerSelectChoiceResult result, EffectContext context);
  public delegate PlayerSelectChoice ThenSelectDelegate(GameState gameState, PlayerChoice choice, PlayerChoiceResult result, EffectContext context);
  public delegate PlayerSelectChoice ThenSelectThunkDelegate(GameState gameState, EffectContext context);
  public delegate PlayerCategorizeChoice DoCategorizeDelegate(GameState gameState, EffectContext context);
  public delegate PlayerCategorizeChoice ThenCategorizeDelegate(GameState gameState, PlayerChoice choice, PlayerChoiceResult result, EffectContext context);
  public delegate PlayerArrangeChoice DoArrangeDelegate(GameState gameState, EffectContext context);
  public delegate PlayerArrangeChoice ThenArrangeDelegate(GameState gameState, PlayerChoice choice, PlayerChoiceResult result, EffectContext context);

  public List<object> Effects { get; set; } = [];

  public EffectSequence(PlayerChoice choice)
  {
    Effects.Add(choice);
  }

  public EffectSequence(DoDelegate @do)
  {
    Effects.Add(@do);
  }

  public EffectSequence(ThenSelectThunkDelegate choiceFunc)
  {
    Effects.Add(choiceFunc);
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

  public EffectSequence Add(ThenSelectThunkDelegate thenSelect)
  {
    Effects.Add(thenSelect);
    return this;
  }

  public EffectSequence Add(DoCategorizeDelegate thenCategorize)
  {
    Effects.Add(thenCategorize);
    return this;
  }

  public EffectSequence Add(ThenCategorizeDelegate thenCategorize)
  {
    Effects.Add(thenCategorize);
    return this;
  }

  public EffectSequence Add(DoArrangeDelegate thenArrange)
  {
    Effects.Add(thenArrange);
    return this;
  }

  public EffectSequence Add(ThenArrangeDelegate thenArrange)
  {
    Effects.Add(thenArrange);
    return this;
  }


  public EffectSequence Add(PlayerChoice choice)
  {
    Effects.Add(choice);
    return this;
  }

  public EffectSequence Add(DoDelegate @do)
  {
    Effects.Add(@do);
    return this;
  }
}