using Fluent;
using Microsoft.VisualBasic;

namespace Dominion.Backend;

public record PlayCardResumeState(int EffectIndex, EffectResumeState? EffectResumeState);
public record EffectResumeState(string[] PlayerIds, int PlayerIndex, int SubeffectIndex, PlayerChoice? LastChoice);

public class ChosenCards
{
  public required CardZone From { get; init; }
  public required CardInstance[] CardInstances { get; init; }
}

public static class FluentEffectHandler
{
  public static (GameState, bool) HandleEffect(GameState game, string activePlayerId, FluentEffect effect)
  {
    EffectResumeState resumeState;
    EffectSequence effectSequence;
    Func<GameState, EffectContext, bool> loopCondition = (game, ctx) => false;

    if (effect is TargetedEffect targeted)
    {
      resumeState = new EffectResumeState(game.GetTargets(targeted.Target), 0, 0, null);
      effectSequence = targeted.Effect;
    }
    else if (effect is LoopEffect loop)
    {
      resumeState = new EffectResumeState([activePlayerId], 0, 0, null);
      effectSequence = loop.Effect;
      loopCondition = loop.LoopCondition;
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
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[playerIndex] };
      do
      {
        game = game with { ActivePlayerId = resumeState.PlayerIds[playerIndex] };
        resumeState = resumeState with { PlayerIndex = playerIndex, SubeffectIndex = 0 };
        for (int subeffectIndex = 0; subeffectIndex < effectSequence.Effects.Count; subeffectIndex++)
        {
          resumeState = resumeState with { SubeffectIndex = subeffectIndex };
          object currentEffect = effectSequence.Effects[subeffectIndex];
          if (currentEffect is EffectSequence.DoDelegate @do)
          {
            game = @do(game, context);
          }
          else if (currentEffect is EffectSequence.ThenSelectThunkDelegate choiceFunc)
          {
            var newChoice = choiceFunc(game, context);
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is EffectSequence.DoCategorizeDelegate categorizeFunc)
          {
            var newChoice = categorizeFunc(game, context);
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is EffectSequence.DoArrangeDelegate arrangeFunc)
          {
            var newChoice = arrangeFunc(game, context);
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else
          {
            // Can't handle delegates expecting previous choice results here.
            throw new NotImplementedException();
          }
        }
      } while (loopCondition(game, context));
    }
    return (game, true);
  }

  public static (GameState, bool) ResumeHandleEffect(GameState game, string activePlayerId, FluentEffect effect, PlayerChoiceResult result)
  {
    EffectSequence effectSequence;
    Func<GameState, EffectContext, bool> loopCondition = (game, ctx) => false;
    if (effect is TargetedEffect targeted)
    {
      effectSequence = targeted.Effect;
    }
    else if (effect is LoopEffect loop)
    {
      effectSequence = loop.Effect;
      loopCondition = loop.LoopCondition;
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

    if (resumeState.LastChoice is PlayerChoice choice && result.IsDeclined)
    {
      game = choice.OnDecline(game, choice, result, new EffectContext { PlayerId = activePlayerId });
      resumeState = resumeState with { LastChoice = null, PlayerIndex = resumeState.PlayerIndex + 1, SubeffectIndex = 0 };
    }

    // Continue looping through the players' effects, but skip the current player if they declined.
    for (int playerIndex = resumeState.PlayerIndex; playerIndex < resumeState.PlayerIds.Length; playerIndex++)
    {
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[playerIndex] };
      bool loopedOnce = false;

      do
      {
        game = game with { ActivePlayerId = resumeState.PlayerIds[playerIndex] };
        resumeState = resumeState with { PlayerIndex = playerIndex, SubeffectIndex = (resumeState.PlayerIndex == playerIndex && !loopedOnce) ? resumeState.SubeffectIndex + 1 : 0 };

        loopedOnce = true;

        for (int effectIndex = resumeState.SubeffectIndex; effectIndex < effectSequence.Effects.Count; effectIndex++)
        {
          resumeState = resumeState with { SubeffectIndex = effectIndex };
          object currentEffect = effectSequence.Effects[effectIndex];
          if (currentEffect is EffectSequence.DoDelegate @do)
          {
            game = @do(game, context);
          }
          else if (currentEffect is EffectSequence.ThenDelegate then)
          {
            game = then(game, resumeState.LastChoice!, result, context);
          }
          else if (currentEffect is EffectSequence.ThenSelectDelegate thenSelect)
          {
            var newChoice = thenSelect(game, resumeState.LastChoice!, result, context);
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              game = game.UpdatePlayer(context.PlayerId, player => player with { ActiveChoice = newChoice }) with { ActivePlayerId = context.PlayerId, ResumeState = game.ResumeState! with { EffectResumeState = resumeState with { LastChoice = newChoice } } };
              return (game, false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is EffectSequence.ThenSelectThunkDelegate choiceFunc)
          {
            var newChoice = choiceFunc(game, context);
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is EffectSequence.DoCategorizeDelegate doCategorizeFunc)
          {
            var newChoice = doCategorizeFunc(game, context);
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is EffectSequence.DoArrangeDelegate doArrangeFunc)
          {
            var newChoice = doArrangeFunc(game, context);
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else if (currentEffect is EffectSequence.ThenCategorizeDelegate thenCategorizeFunc)
          {
            var newChoice = thenCategorizeFunc(game, resumeState.LastChoice!, result, context);
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is EffectSequence.ThenArrangeDelegate thenArrangeFunc)
          {
            var newChoice = thenArrangeFunc(game, resumeState.LastChoice!, result, context);
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice, resumeState), false);
            }
            else
            {
              return ResumeHandleEffect(game, context.PlayerId, effect, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else
          {
            throw new NotImplementedException();
          }
        }
      } while (loopCondition(game, context));
    }
    return (game, true);
  }
}