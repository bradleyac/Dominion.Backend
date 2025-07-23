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

  public static EffectSequence Loop(Func<GameState, EffectContext, bool> loopCondition, EffectSequence effect) => effect with { LoopCondition = loopCondition };
  public static EffectSequence ForEach(EffectTarget effectTarget, EffectSequence effect) => effect with { Target = effectTarget };
}

public record EffectContext
{
  public required string PlayerId { get; init; }
}

public record PendingEffectResumeState(int EffectIndex, bool IsNew);
public record EffectSequenceResumeState(string[] PlayerIds, int PlayerIndex, int SubeffectIndex, PlayerChoice? LastChoice);

public record PendingEffect
{
  public string Id { get; } = Guid.NewGuid().ToString();
  public required string OwnerId { get; init; }
  public required EffectSequence[] Effects { get; init; }
  public required bool IsPlayCard { get; init; }
  protected PendingEffectResumeState ResumeState { get; init; } = new PendingEffectResumeState(0, true);

  public (GameState Game, PendingEffect? NewEffect) Resolve(GameState game, PlayerChoiceResult? result)
  {
    var resumeState = ResumeState;

    if (resumeState.IsNew)
    {
      if (IsPlayCard)
      {
        // Check for reactions

        resumeState = resumeState with { IsNew = false };
        return (game, this with { ResumeState = resumeState });
      }
      resumeState = resumeState with { IsNew = false };
    }

    for (int i = resumeState.EffectIndex; i < Effects.Length; i++)
    {
      resumeState = resumeState with { EffectIndex = i };
      (game, var newEffect) = Effects[i].Resolve(game, OwnerId, result);
      if (newEffect is not null)
      {
        return (game, this with { ResumeState = resumeState, Effects = [.. Effects.Take(i), newEffect, .. Effects.Skip(i + 1)] });
      }
    }

    return (game, null);
  }
}

public record EffectSequence
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

  public Func<GameState, EffectContext, bool> LoopCondition { get; init; } = static (game, ctx) => false;
  public EffectTarget Target { get; init; } = EffectTarget.Me;
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

  protected EffectSequenceResumeState? ResumeState { get; init; }

  public (GameState, EffectSequence?) Resolve(GameState game, string ownerId, PlayerChoiceResult? result)
  {
    bool firstTime = ResumeState is null;
    var resumeState = ResumeState ?? new EffectSequenceResumeState(game.GetTargets(ownerId, Target), 0, 0, null);

    if (resumeState.LastChoice is PlayerChoice choice && result!.IsDeclined)
    {
      game = choice.OnDecline(game, choice, result, new EffectContext { PlayerId = resumeState.PlayerIds[resumeState.PlayerIndex] });
      resumeState = resumeState with { LastChoice = null, PlayerIndex = resumeState.PlayerIndex + 1, SubeffectIndex = 0 };
    }

    for (int playerIndex = resumeState.PlayerIndex; playerIndex < resumeState.PlayerIds.Length; playerIndex++)
    {
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[playerIndex] };
      bool loopedOnce = false;

      do
      {
        game = game with { ActivePlayerId = resumeState.PlayerIds[playerIndex] };
        resumeState = resumeState with { PlayerIndex = playerIndex, SubeffectIndex = (resumeState.PlayerIndex == playerIndex && !loopedOnce && !firstTime) ? resumeState.SubeffectIndex + 1 : 0 };

        loopedOnce = true;

        for (int subeffectIndex = resumeState.SubeffectIndex; subeffectIndex < Effects.Count; subeffectIndex++)
        {
          resumeState = resumeState with { SubeffectIndex = subeffectIndex };
          object currentEffect = Effects[subeffectIndex];
          if (currentEffect is DoDelegate @do)
          {
            game = @do(game, context);
          }
          else if (currentEffect is ThenDelegate then)
          {
            game = then(game, resumeState.LastChoice!, result, context);
          }
          else if (currentEffect is ThenSelectDelegate thenSelect)
          {
            var newChoice = thenSelect(game, resumeState.LastChoice!, result, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState });
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is ThenSelectThunkDelegate choiceFunc)
          {
            var newChoice = choiceFunc(game, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState });
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is DoCategorizeDelegate doCategorizeFunc)
          {
            var newChoice = doCategorizeFunc(game, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState });
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is DoArrangeDelegate doArrangeFunc)
          {
            var newChoice = doArrangeFunc(game, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState });
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else if (currentEffect is ThenCategorizeDelegate thenCategorizeFunc)
          {
            var newChoice = thenCategorizeFunc(game, resumeState.LastChoice!, result, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState });
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is ThenArrangeDelegate thenArrangeFunc)
          {
            var newChoice = thenArrangeFunc(game, resumeState.LastChoice!, result, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState });
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else
          {
            throw new NotImplementedException();
          }
        }
      } while (LoopCondition(game, context));
    }
    return (game, null);
  }
}