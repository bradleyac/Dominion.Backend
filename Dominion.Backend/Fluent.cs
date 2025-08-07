using System.Linq;
using Dominion.Backend;
using Stateless.Graph;

namespace Fluent;

public static class Fluent
{
  public static EffectSequence React(EffectSequence.ReactDelegate react) => new EffectSequence(react);
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

public record PendingEffectResumeState(int EffectIndex, bool ReactionsTriggered, bool PostReactionsTriggered);
public record EffectSequenceResumeState(string[] PlayerIds, int PlayerIndex, int SubeffectIndex, PlayerChoice? LastChoice, PlayerChoiceResult? LastResult);

public record PendingEffect
{
  public string Id { get; } = Guid.NewGuid().ToString();
  public required string OwnerId { get; init; }
  public required EffectSequence[] Effects { get; init; }
  public string? TriggeringEffectId { get; init; }
  public CardInstance? PlayedCard { get; init; }
  public CardZone? PlayedCardLocation { get; init; }
  public CardInstance? ReactingCard { get; init; }
  public CardZone? ReactingCardLocation { get; init; }
  protected PendingEffectResumeState ResumeState { get; init; } = new PendingEffectResumeState(0, false, false);

  public (GameState Game, PendingEffect? NewEffect, bool Restart) Resolve(GameState game, PlayerChoiceResult? result)
  {
    var resumeState = ResumeState;

    if (!resumeState.ReactionsTriggered)
    {
      resumeState = resumeState with { ReactionsTriggered = true };

      // Check for reactions
      if (PlayedCard is not null)
      {
        game = game.AddCardPlayedReactions(PlayedCard, PlayedCardLocation!.Value, Id, OwnerId);
        return (game, this with { ResumeState = resumeState }, true);
      }
    }

    if (!resumeState.PostReactionsTriggered)
    {
      resumeState = resumeState with { PostReactionsTriggered = true };

      if (PlayedCard is not null)
      {
        game = game.AddCardPlayedPostReactionTriggers(PlayedCard, OwnerId);
        return (game, this with { ResumeState = resumeState }, true);
      }
    }

    for (int i = resumeState.EffectIndex; i < Effects.Length; i++)
    {
      resumeState = resumeState with { EffectIndex = i };
      (game, var newEffect, bool restart) = Effects[i].Resolve(game, OwnerId, Id, TriggeringEffectId, ReactingCard, ReactingCardLocation, result);
      if (newEffect is not null)
      {
        return (game, this with { ResumeState = resumeState, Effects = [.. Effects.Take(i), newEffect, .. Effects.Skip(i + 1)] }, restart);
      }
    }

    return (game, null, false);
  }
}

public record EffectSequence
{
  public delegate GameState DoDelegate(GameState gameState, EffectContext context);
  public delegate GameState ThenDelegate(GameState gameState, PlayerChoice choice, PlayerChoiceResult result, EffectContext context);
  public delegate GameState ThenAfterSelectDelegate(GameState gameState, PlayerSelectChoice choice, PlayerSelectChoiceResult result, EffectContext context);
  public delegate GameState ReactDelegate(GameState gameState, string triggeringEffectId, string triggeredCardInstanceId, CardZone triggeredCardLocation, EffectContext context);
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

  public EffectSequence(ReactDelegate react)
  {
    Effects.Add(react);
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

  public EffectSequence Add(ReactDelegate react)
  {
    Effects.Add(react);
    return this;
  }

  protected EffectSequenceResumeState? ResumeState { get; init; }

  // TODO: Major refactor. This has gotten out of hand.
  public (GameState, EffectSequence?, bool Restart) Resolve(GameState game, string ownerId, string currentEffectId, string? triggeringEffectId, CardInstance? reactingCard, CardZone? reactingCardLocation, PlayerChoiceResult? result)
  {
    bool veryFirstTime = ResumeState is null;
    var resumeState = ResumeState ?? new EffectSequenceResumeState(game.GetTargets(ownerId, Target), 0, 0, null, null);

    if (result is not null)
    {
      resumeState = resumeState with { LastResult = result };
    }

    if (resumeState.LastChoice is PlayerChoice choice && (resumeState.LastResult?.IsDeclined ?? false))
    {
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[resumeState.PlayerIndex] };
      game = choice.OnDecline(game, choice, resumeState.LastResult, context);
      bool continueWithCurrentPlayer = LoopCondition(game, context);
      // If we're in a loop and should continue with the current player, then leave the playerIndex alone.
      // Set SubeffectIndex to -1 because it will be incremented by 1 later.
      resumeState = resumeState with { LastChoice = null, PlayerIndex = resumeState.PlayerIndex + (continueWithCurrentPlayer ? 0 : 1), SubeffectIndex = -1 };
    }

    for (int playerIndex = resumeState.PlayerIndex; playerIndex < resumeState.PlayerIds.Length; playerIndex++)
    {
      EffectContext context = new EffectContext { PlayerId = resumeState.PlayerIds[playerIndex] };

      // If the active player is immune to this effect, continue to the next player.
      var activePlayer = game.GetPlayer(context.PlayerId);
      if (activePlayer.ImmuneToEffectIds.Contains(currentEffectId))
      {
        continue;
      }

      bool loopedOnce = false;

      do
      {
        game = game with { ActivePlayerId = resumeState.PlayerIds[playerIndex] };
        resumeState = resumeState with { PlayerIndex = playerIndex, SubeffectIndex = loopedOnce || veryFirstTime ? 0 : resumeState.SubeffectIndex + 1 };

        loopedOnce = true;

        for (int subeffectIndex = resumeState.SubeffectIndex; subeffectIndex < Effects.Count; subeffectIndex++)
        {
          resumeState = resumeState with { SubeffectIndex = subeffectIndex };
          object currentEffect = Effects[subeffectIndex];
          if (currentEffect is DoDelegate @do)
          {
            game = @do(game, context);

            // If this effect isn't at the top of the stack anymore (because Reactions or similar) then leave this here and go back to the top.
            if (game.EffectStack.Last().Id != currentEffectId)
            {
              return (game, this with { ResumeState = resumeState }, true);
            }
          }
          else if (currentEffect is ThenDelegate then)
          {
            game = then(game, resumeState.LastChoice!, resumeState.LastResult, context);

            // If this effect isn't at the top of the stack anymore (because Reactions or similar) then leave this here and go back to the top.
            if (game.EffectStack.Last().Id != currentEffectId)
            {
              return (game, this with { ResumeState = resumeState }, true);
            }
          }
          else if (currentEffect is ThenSelectDelegate thenSelect)
          {
            var newChoice = thenSelect(game, resumeState.LastChoice!, resumeState.LastResult, context);
            resumeState = resumeState with { LastChoice = newChoice, LastResult = null };
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState }, false);
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, currentEffectId, triggeringEffectId, reactingCard, reactingCardLocation, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is ThenSelectThunkDelegate choiceFunc)
          {
            var newChoice = choiceFunc(game, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.DoAnyCardsMatch(newChoice.Filter, context.PlayerId))
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState }, false);
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, currentEffectId, triggeringEffectId, reactingCard, reactingCardLocation, new PlayerSelectChoiceResult { SelectedCards = [] });
            }
          }
          else if (currentEffect is DoCategorizeDelegate doCategorizeFunc)
          {
            var newChoice = doCategorizeFunc(game, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState }, false);
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, currentEffectId, triggeringEffectId, reactingCard, reactingCardLocation, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is DoArrangeDelegate doArrangeFunc)
          {
            var newChoice = doArrangeFunc(game, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState }, false);
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, currentEffectId, triggeringEffectId, reactingCard, reactingCardLocation, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else if (currentEffect is ThenCategorizeDelegate thenCategorizeFunc)
          {
            var newChoice = thenCategorizeFunc(game, resumeState.LastChoice!, resumeState.LastResult, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToCategorize, context.PlayerId).Length > 0)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState }, false);
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, currentEffectId, triggeringEffectId, reactingCard, reactingCardLocation, new PlayerCategorizeChoiceResult { CategorizedCards = [] });
            }
          }
          else if (currentEffect is ThenArrangeDelegate thenArrangeFunc)
          {
            var newChoice = thenArrangeFunc(game, resumeState.LastChoice!, resumeState.LastResult, context);
            resumeState = resumeState with { LastChoice = newChoice };
            if (game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId).Length > 1)
            {
              return (game.UpdatePlayerChoice(context, newChoice), this with { ResumeState = resumeState }, false);
            }
            else
            {
              return (this with { ResumeState = resumeState }).Resolve(game, ownerId, currentEffectId, triggeringEffectId, reactingCard, reactingCardLocation, new PlayerArrangeChoiceResult { ArrangedCards = game.CardsInZone(newChoice.ZoneToArrange, context.PlayerId) });
            }
          }
          else if (currentEffect is PlayerReactChoice reactChoice)
          {
            if (resumeState.LastResult is PlayerReactChoiceResult reactResult)
            {
              game = game with { EffectStack = [.. game.EffectStack, new PendingEffect { ReactingCard = reactResult.ChosenReaction, ReactingCardLocation = reactChoice.EffectReferences.FirstOrDefault(effectRef => effectRef.CardInstance.InstanceId == reactResult.ChosenReaction.Id)?.CardInstance.Location, OwnerId = context.PlayerId, TriggeringEffectId = reactChoice.TriggeringEffectId, Effects = reactResult.ChosenReaction.Card.ReactionEffects }] };
              var newChoice = reactChoice with { Id = Guid.NewGuid().ToString(), EffectReferences = [.. reactChoice.EffectReferences.Where(e => e.CardInstance.InstanceId != reactResult.ChosenReaction.Id)] };
              return (game, this with
              {
                ResumeState = resumeState with { LastResult = null }, // Clear last result since we consumed it already and we'll be back.
                Effects = [.. Effects, newChoice]
              }, true);
            }
            else
            {
              // Remove reactions whose cards have moved.
              reactChoice = reactChoice with { EffectReferences = [.. reactChoice.EffectReferences.Where(eref => game.IsCardInZone(context.PlayerId, eref.CardInstance.InstanceId, eref.CardInstance.Location))] };
              if (reactChoice.EffectReferences.Any())
              {
                resumeState = resumeState with { LastChoice = reactChoice };
                return (game.UpdatePlayerChoice(context, reactChoice), this with { ResumeState = resumeState, Effects = [.. Effects, reactChoice with { Id = Guid.NewGuid().ToString() }] }, false);
              }
              else
              {
                return (game, null, false);
              }
            }
          }
          else if (currentEffect is ReactDelegate react)
          {
            game = react(game, triggeringEffectId, reactingCard.Id, reactingCardLocation.Value, context);
          }
          else
          {
            throw new NotImplementedException();
          }
        }
      } while (LoopCondition(game, context));
    }
    return (game, null, false);
  }
}