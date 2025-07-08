using System.Data;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Dominion.Backend;

public abstract class CardEffectHandler
{
  public abstract bool CanHandleEffect(CardEffect effect);
  public abstract (GameState, bool) HandleEffect(GameState game, string activePlayerId, CardEffect effect);
  public abstract (GameState, bool) ResumeHandleEffect(GameState game, string activePlayerId, CardEffect effect, ChosenCards chosenCards);
}

public class SimpleCardEffectHandler : CardEffectHandler
{
  private Regex SimpleEffectRegex = new Regex(@"(?<count>\d+)(?<resource>action|buy|card|coin)");

  public override bool CanHandleEffect(CardEffect effect) => effect is SimpleEffect;

  public override (GameState, bool) HandleEffect(GameState game, string activePlayerId, CardEffect effect)
  {
    var player = game.Players.Single(player => player.Id == activePlayerId);

    SimpleEffect simple = (SimpleEffect)effect;
    var results = SimpleEffectRegex.Match(simple.Effect);
    if (results.Success)
    {
      int count = Convert.ToInt32(results.Groups["count"].Captures.First().Value);
      string resource = results.Groups["resource"].Captures.First().Value;

      switch (resource)
      {
        case "coin": player = player with { Resources = player.Resources with { Coins = player.Resources.Coins + count } }; break;
        case "buy": player = player with { Resources = player.Resources with { Buys = player.Resources.Buys + count } }; break;
        case "action": player = player with { Resources = player.Resources with { Actions = player.Resources.Actions + count } }; break;
        case "card": player = Utils.DrawCards(player, count); break;
      }
    }

    PlayerState[] newPlayers = [.. game.Players.Select(eachPlayer => eachPlayer.Id == player.Id ? player : eachPlayer)];

    return (game with { Players = newPlayers }, true);
  }

  public override (GameState, bool) ResumeHandleEffect(GameState game, string activePlayerId, CardEffect effect, ChosenCards chosenCards)
  {
    return (game, true);
  }
}

public class ChosenCards
{
  public required CardZone From { get; init; }
  public required CardInstance[] CardInstances { get; init; }
}

public class MoveCardsEffectHandler : CardEffectHandler
{
  public override bool CanHandleEffect(CardEffect effect) => effect.GetType() == typeof(MoveCardsEffect);

  public override (GameState, bool) HandleEffect(GameState game, string activePlayerId, CardEffect effect)
  {
    var moveCardsEffect = (MoveCardsEffect)effect;
    var resumeState = MakeResumeState(game, activePlayerId, moveCardsEffect);

    for (int i = 0; i < resumeState.PlayerIds.Length; i++)
    {
      resumeState = resumeState with { PlayerIndex = i };

      (game, bool completed) = MoveCards(game, resumeState.PlayerIds[i], moveCardsEffect, resumeState);

      if (!completed)
      {
        return (game, false);
      }
    }

    return (game, true);
  }

  public override (GameState, bool) ResumeHandleEffect(GameState game, string activePlayerId, CardEffect effect, ChosenCards chosenCards)
  {
    var moveCardsEffect = (MoveCardsEffect)effect;
    var resumeState = game.ResumeState.EffectResumeState;

    var effectPlayerId = resumeState.PlayerIds[resumeState.PlayerIndex];
    game = ResumeMoveCards(game, effectPlayerId, moveCardsEffect, chosenCards);

    for (int i = resumeState.PlayerIndex + 1; i < resumeState.PlayerIds.Length; i++)
    {
      resumeState = resumeState with { PlayerIndex = i };

      (game, bool completed) = MoveCards(game, resumeState.PlayerIds[i], moveCardsEffect, resumeState);

      if (!completed)
      {
        return (game, false);
      }
    }

    return (game, true);
  }

  public (GameState, bool) MoveCards(GameState game, string targetPlayerId, MoveCardsEffect effect, EffectResumeState resumeState)
  {
    (game, var cards) = ChooseCards(game, targetPlayerId, effect.Filter, resumeState);

    if (cards != null)
    {
      foreach (var card in cards.CardInstances)
      {
        game = game
          .RemoveFromCardZone(cards.From, targetPlayerId, card)
          .AddToCardZone(effect.To, targetPlayerId, card);
      }

      return (game, true);
    }
    else
    {
      return (game, false);
    }
  }

  public GameState ResumeMoveCards(GameState game, string targetPlayerId, MoveCardsEffect effect, ChosenCards chosenCards)
  {
    foreach (var card in chosenCards.CardInstances)
    {
      game = game
        .RemoveFromCardZone(chosenCards.From, targetPlayerId, card)
        .AddToCardZone(effect.To, targetPlayerId, card);
    }

    return game;
  }

  public (GameState, ChosenCards?) ChooseCards(GameState game, string playerId, CardFilter filter, EffectResumeState resumeState)
  {
    if (filter.CardId is null)
    {
      game = game with
      {
        Players = [.. game.Players.Select(player => player.Id == playerId ? player with { ActiveFilter = filter } : player)],
        ResumeState = game.ResumeState with { EffectResumeState = resumeState }
      };

      return (game, null);
    }
    else
    {
      // One specific cardId from supply
      var chosenCards = new ChosenCards
      {
        // MinCount is null (= 1) or MinCount == MaxCount == count to add
        CardInstances = [.. Enumerable.Repeat(0, filter.MinCount is not null && filter.MinCount == filter.MaxCount ? filter.MinCount.Value : 1).Select(_ => CardInstance.CreateByCardId(filter.CardId.Value))],
        From = filter.From
      };
      return (game, chosenCards);
    }
  }

  public EffectResumeState MakeResumeState(GameState game, string activePlayerId, MoveCardsEffect moveCardsEffect)
  {
    if (moveCardsEffect.Target == EffectTarget.Me)
    {
      return new EffectResumeState([activePlayerId], 0);
    }
    else if (moveCardsEffect.Target == EffectTarget.All)
    {
      PlayerState[] skipped = [.. game.Players.TakeWhile(player => player.Id != activePlayerId)];
      return new EffectResumeState(game.Players.Skip(skipped.Length).Concat(skipped).Select(p => p.Id).ToArray(), 0);
    }
    else
    {
      PlayerState[] skipped = [.. game.Players.TakeWhile(player => player.Id != activePlayerId)];
      return new EffectResumeState(game.Players.Skip(skipped.Length + 1).Concat(skipped).Select(p => p.Id).ToArray(), 0);
    }
  }
}

public record PlayCardResumeState(CardInstance CardInstance, int EffectIndex, EffectResumeState? EffectResumeState);
public record EffectResumeState(string[] PlayerIds, int PlayerIndex);