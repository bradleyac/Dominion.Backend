using System.Data;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Fluent;

namespace Dominion.Backend;

public abstract class CardEffectHandler
{
  public abstract bool CanHandleEffect(FluentEffect effect);
  public abstract (GameState, bool) HandleEffect(GameState game, string activePlayerId, FluentEffect effect);
  public abstract (GameState, bool) ResumeHandleEffect(GameState game, string activePlayerId, FluentEffect effect, ChosenCards chosenCards);
}

public record PlayCardResumeState(CardInstance CardInstance, int EffectIndex, EffectResumeState? EffectResumeState);
public record EffectResumeState(string[] PlayerIds, int PlayerIndex, int EffectIndex, CardFilter? LastFilter);

public class ChosenCards
{
  public required CardZone From { get; init; }
  public required CardInstance[] CardInstances { get; init; }
}

public class FluentEffectHandler : CardEffectHandler
{
  public override bool CanHandleEffect(FluentEffect effect) => effect is FluentEffect;

  public override (GameState, bool) HandleEffect(GameState game, string activePlayerId, FluentEffect effect)
  {
    EffectResumeState resumeState;
    EffectSequence effectSequence;
    if (effect is TargetedEffect targeted)
    {
      resumeState = new EffectResumeState(game.GetTargets(targeted.Target), 0, 0, null);
      effectSequence = targeted.Effect;
    }
    else if (effect is EffectSequence sequence)
    {
      resumeState = new EffectResumeState([activePlayerId], 0, 0, null);
      effectSequence = sequence;
    }
    else
    {
      throw new NotImplementedException();
    }

    for (int playerIndex = 0; playerIndex < resumeState.PlayerIds.Length; playerIndex++)
    {
      game = game with { ActivePlayerId = resumeState.PlayerIds[playerIndex] };
      resumeState = resumeState with { PlayerIndex = playerIndex };
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[playerIndex] };
      for (int effectIndex = 0; effectIndex < effectSequence.Effects.Count; effectIndex++)
      {
        resumeState = resumeState with { EffectIndex = effectIndex };
        object currentEffect = effectSequence.Effects[effectIndex];
        if (currentEffect is EffectSequence.DoDelegate @do)
        {
          game = @do(game, context);
        }
        else if (currentEffect is EffectSequence.ThenDelegate then)
        {
          // Can't handle this in a first go-round.
          throw new NotImplementedException();
        }
        else if (currentEffect is EffectSequence.ThenSelectDelegate thenSelect)
        {
          // Or this.
          throw new NotImplementedException();
        }
        else if (currentEffect is CardFilter newFilter)
        {
          game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveFilter = newFilter }) with { ActivePlayerId = context.PlayerId, ResumeState = game.ResumeState! with { EffectResumeState = resumeState with { LastFilter = newFilter } } };
          if (game.DoAnyCardsMatch(newFilter, context.PlayerId))
          {
            return (game, false);
          }
          else
          {
            game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveFilter = null });
            return ResumeHandleEffect(game, context.PlayerId, effect, new ChosenCards { CardInstances = [], From = newFilter.From });
          }
        }
        else
        {
          throw new NotImplementedException();
        }
      }
    }
    return (game, true);
  }

  public override (GameState, bool) ResumeHandleEffect(GameState game, string activePlayerId, FluentEffect effect, ChosenCards chosenCards)
  {
    EffectSequence effectSequence;
    if (effect is TargetedEffect targeted)
    {
      effectSequence = targeted.Effect;
    }
    else if (effect is EffectSequence sequence)
    {
      effectSequence = sequence;
    }
    else
    {
      throw new NotImplementedException();
    }

    EffectResumeState resumeState = game.ResumeState!.EffectResumeState!;

    for (int playerIndex = resumeState.PlayerIndex; playerIndex < resumeState.PlayerIds.Length; playerIndex++)
    {
      game = game with { ActivePlayerId = resumeState.PlayerIds[playerIndex] };
      resumeState = resumeState with { PlayerIndex = playerIndex };
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[playerIndex] };
      for (int effectIndex = resumeState.EffectIndex + 1; effectIndex < effectSequence.Effects.Count; effectIndex++)
      {
        resumeState = resumeState with { EffectIndex = effectIndex };
        object currentEffect = effectSequence.Effects[effectIndex];
        if (currentEffect is EffectSequence.DoDelegate @do)
        {
          game = @do(game, context);
        }
        else if (currentEffect is EffectSequence.ThenDelegate then)
        {
          game = then(game, resumeState.LastFilter!, chosenCards.CardInstances, context);
        }
        else if (currentEffect is EffectSequence.ThenSelectDelegate thenSelect)
        {
          var newFilter = thenSelect(game, resumeState.LastFilter!, chosenCards.CardInstances, context);
          game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveFilter = newFilter }) with { ActivePlayerId = context.PlayerId, ResumeState = game.ResumeState! with { EffectResumeState = resumeState with { LastFilter = newFilter } } };
          if (game.DoAnyCardsMatch(newFilter, context.PlayerId))
          {
            return (game, false);
          }
          else
          {
            game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveFilter = null });
            return ResumeHandleEffect(game, context.PlayerId, effect, new ChosenCards { CardInstances = [], From = newFilter.From });
          }
        }
        else if (currentEffect is CardFilter newFilter)
        {
          game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveFilter = newFilter }) with { ActivePlayerId = context.PlayerId, ResumeState = game.ResumeState! with { EffectResumeState = resumeState with { LastFilter = newFilter } } };
          if (game.DoAnyCardsMatch(newFilter, context.PlayerId))
          {
            return (game, false);
          }
          else
          {
            game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveFilter = null });
            return ResumeHandleEffect(game, context.PlayerId, effect, new ChosenCards { CardInstances = [], From = newFilter.From });
          }
        }
        else
        {
          throw new NotImplementedException();
        }
      }
    }
    return (game, true);
  }
}