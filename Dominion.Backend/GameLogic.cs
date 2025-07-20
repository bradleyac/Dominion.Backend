using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using Fluent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dominion.Backend;

public static class GameLogic
{
  public static GameState StartGame(GameState game)
  {
    if (game.GameStarted)
    {
      return game;
    }

    PlayerState[] players = [.. game.Players.Shuffle().Select((player, i) =>
    {
      var shuffledDeck = player.Deck.Shuffle();
      return player with
      {
        Index = i,
        Hand = [.. shuffledDeck.Take(5)],
        Deck = [.. shuffledDeck.Skip(5)],
        Resources = i == 0 ? new PlayerResources(1, 1, 0, 0, 0, 0) : PlayerResources.Empty
      };
    })];

    return game with { GameStarted = true, Players = players, CurrentPlayer = 0, ActivePlayerId = players[0].Id };
  }

  public static GameState EndTurn(GameState game)
  {
    if (CheckForGameEnd(game))
    {
      // Move all cards to each player's deck.
      game = game with { Players = [.. game.Players.Select(p => p with { Deck = [.. p.Deck, .. p.Discard, .. p.Hand, .. p.Play, .. p.PrivateReveal], Discard = [], Hand = [], Play = [], PrivateReveal = [] })] };
      var scores = CalculateScores(game);
      var winners = scores.GroupBy(kvp => kvp.Value).MaxBy(group => group.Key)!.Select(score => score.Key);
      GameResult result = new GameResult([.. winners], scores);
      return game with { GameResult = result };
    }
    else
    {
      int nextPlayerIndex = (game.CurrentPlayer + 1) % game.Players.Length;
      int nextTurn = game.CurrentTurn + (nextPlayerIndex == 0 ? 1 : 0);

      const int nextPlayer = 1, thisPlayer = 0, otherPlayer = -1;
      int whichPlayer(int i) => i == game.CurrentPlayer ? thisPlayer : i == nextPlayerIndex ? nextPlayer : otherPlayer;
      var newPlayers = game.Players.Select((player, i) => whichPlayer(i) switch
      {
        thisPlayer => EndPlayerTurn(player),
        nextPlayer => StartPlayerTurn(player),
        _ => player
      }).ToArray();

      return game with { Phase = Phase.Action, Players = newPlayers, CurrentTurn = nextTurn, CurrentPlayer = nextPlayerIndex, ActivePlayerId = newPlayers[nextPlayerIndex].Id };
    }
  }

  // Assumes all cards are back in deck at this point.
  // .5 points for not going yet breaks one level of ties.
  private static Dictionary<string, double> CalculateScores(GameState game) => game.Players.ToDictionary(
    p => p.Id,
    p => p.Deck.SumBy(c => c.Card.ValueFunc?.Invoke(game, p.Id) ?? c.Card.Value)
      + p.Resources.Points
      + (p.Index > game.CurrentPlayer ? .5 : 0));

  private static bool CheckForGameEnd(GameState game) => game.KingdomCards.Any(kc => kc.Card.Id == MasterCardData.CardIDs.Province && kc.Remaining == 0) || game.KingdomCards.Count(kc => kc.Remaining == 0) > 2;

  public static GameState EndActionPhase(GameState game, string playerId)
  {
    if (game.Players[game.CurrentPlayer].Id == playerId && game.ActivePlayerId == playerId && game.Phase == Phase.Action)
    {
      return game with { Phase = Phase.BuyOrPlay };
    }
    return game;
  }

  public static GameState BuyCard(GameState game, string playerId, int cardId)
  {
    if (IsActivePlayer(game, playerId, out var thisPlayer))
    {
      if (game.Phase.IsBuyPhase())
      {
        var cardPile = game.KingdomCards.FirstOrDefault(pile => pile.Card.Id == cardId);
        if (cardPile is not null && cardPile.Remaining > 0 && thisPlayer.Resources.Buys > 0 && thisPlayer.Resources.Coins >= cardPile.Card.Cost)
        {
          var cardInstance = new CardInstance(cardPile.Card);
          var newCardPile = cardPile with { Remaining = cardPile.Remaining - 1 };
          var newPlayer = thisPlayer with
          {
            Discard = [.. thisPlayer.Discard, cardInstance],
            Resources = thisPlayer.Resources with
            {
              Buys = thisPlayer.Resources.Buys - 1,
              Coins = thisPlayer.Resources.Coins - cardPile.Card.Cost
            }
          };

          game = game with
          {
            Phase = Phase.Buy,
            Players = game.Players.Select(player => player.Id == newPlayer.Id ? newPlayer : player).ToArray(),
            KingdomCards = [.. game.KingdomCards.Select(pile => pile.Card.Id == newCardPile.Card.Id ? newCardPile : pile)]
          };

          if (newPlayer.Resources.Buys == 0)
          {
            return EndTurn(game);
          }
        }
      }
    }

    return game;
  }

  public static (GameState, bool Updated) PlayCard(GameState game, string playerId, string cardInstanceId, CardZone from = CardZone.Hand, bool ignoreCostsAndPhases = false, bool moveCard = true, int count = 1, bool afterCurrentEffect = false)
  {
    if (IsActivePlayer(game, playerId, out var thisPlayer))
    {
      if (HasCardInZone(game, playerId, cardInstanceId, from, out var cardInstance))
      {
        if (!ignoreCostsAndPhases
          && game.Phase == Phase.Action
          && cardInstance.Card.Types.Contains(CardType.Treasure)
          && !cardInstance.Card.Types.Contains(CardType.Action))
        {
          game = game with { Phase = Phase.BuyOrPlay };
        }

        if (ignoreCostsAndPhases
          || (game.Phase == Phase.Action && cardInstance.Card.Types.Contains(CardType.Action) && thisPlayer.Resources.Actions > 0)
          || (game.Phase == Phase.BuyOrPlay && cardInstance.Card.Types.Contains(CardType.Treasure)))
        {
          if (moveCard)
          {
            game = game.MoveBetweenZones(from, CardZone.Play, playerId, [cardInstance]);
          }

          if (!ignoreCostsAndPhases && game.Phase == Phase.Action && cardInstance.Card.Types.Contains(CardType.Action))
          {
            game = game.UpdatePlayer(playerId, player => player with
            {
              Resources = thisPlayer.Resources with { Actions = thisPlayer.Resources.Actions - (game.Phase == Phase.Action ? 1 : 0) }
            });
          }

          game = game with { EffectStack = [.. game.EffectStack, .. Enumerable.Range(0, count).Select(i => new PendingEffect { Effects = cardInstance.Card.Effects, OwnerId = playerId })] };
          return (afterCurrentEffect ? game : ProcessEffectStack(game), true);
        }
      }
    }

    return (game, false);
  }

  public static GameState ProcessEffectStack(GameState game)
  {
    PendingEffect? currentEffect;

    while ((currentEffect = game.EffectStack.LastOrDefault()) is not null)
    {
      game = game with
      {
        ResumeState = new PlayCardResumeState(0, null)
      };

      for (int i = 0; i < currentEffect.Effects.Length; i++)
      {
        game = game with { ResumeState = game.ResumeState! with { EffectIndex = i, EffectResumeState = null } };
        (game, bool completed) = FluentEffectHandler.HandleEffect(game, currentEffect.OwnerId, currentEffect.Effects[i]);
        if (!completed)
        {
          return game;
        }
      }

      // Remove the effect we just processed. It might not be at the top of the stack anymore.
      game = game with { EffectStack = [.. game.EffectStack.Where(effect => effect.Id != currentEffect.Id)] };
    }

    return game with { ResumeState = null, ActivePlayerId = game.Players[game.CurrentPlayer].Id };
  }


  public static GameState ResumeProcessEffectStack(GameState game, string playerId, PlayerChoice lastChoice, PlayerChoiceResult lastResult)
  {
    PendingEffect? currentEffect = game.EffectStack.LastOrDefault();

    if (currentEffect is null) return game;

    var resumeState = game.ResumeState;
    var resumedEffect = currentEffect.Effects[resumeState.EffectIndex];

    game = game with { Players = [.. game.Players.Select(p => p with { ActiveChoice = null })] };

    (game, bool completed) = FluentEffectHandler.ResumeHandleEffect(game, playerId, resumedEffect, lastResult);
    if (!completed)
    {
      return game;
    }

    for (int i = resumeState.EffectIndex + 1; i < currentEffect.Effects.Length; i++)
    {
      game = game with { ResumeState = game.ResumeState! with { EffectIndex = i, EffectResumeState = null } };
      (game, completed) = FluentEffectHandler.HandleEffect(game, currentEffect.OwnerId, currentEffect.Effects[i]);
      if (!completed)
      {
        return game;
      }
    }

    // Remove the effect we just processed. It might not be at the top of the stack anymore.
    game = game with { EffectStack = [.. game.EffectStack.Where(effect => effect.Id != currentEffect.Id)] };

    while ((currentEffect = game.EffectStack.LastOrDefault()) is not null)
    {
      game = game with
      {
        ResumeState = new PlayCardResumeState(0, null)
      };

      for (int i = 0; i < currentEffect.Effects.Length; i++)
      {
        game = game with { ResumeState = game.ResumeState! with { EffectIndex = i, EffectResumeState = null } };

        (game, completed) = FluentEffectHandler.HandleEffect(game, currentEffect.OwnerId, currentEffect.Effects[i]);
        if (!completed)
        {
          return game;
        }
      }

      // TODO: This feels clumsy.
      // Remove the effect we just processed. It might not be at the top of the stack anymore.
      game = game with { EffectStack = [.. game.EffectStack.Where(effect => effect.Id != currentEffect.Id)] };
    }

    return game with { ResumeState = null, ActivePlayerId = game.Players[game.CurrentPlayer].Id };
  }

  private static PlayerState StartPlayerTurn(PlayerState player) => player with { Resources = PlayerResources.NewTurn(player.Resources) };

  private static PlayerState EndPlayerTurn(PlayerState player) => (player with
  {
    Discard = [.. player.Discard, .. player.Hand, .. player.Play],
    Play = [],
    Hand = [],
    Resources = PlayerResources.EndTurn(player.Resources)
  }).DrawCards(5);

  private static bool IsActivePlayer(GameState game, string playerId, [NotNullWhen(true)] out PlayerState? player)
    => (player = game.ActivePlayerId == playerId && game.GetPlayer(playerId) is var p ? p : null) is not null;

  private static bool HasCardInZone(GameState game, string playerId, string cardInstanceId, CardZone from, [NotNullWhen(true)] out CardInstance? cardInstance)
    => (cardInstance = game.CardsInZone(from, playerId).FirstOrDefault(card => card.Id == cardInstanceId)) is not null;
}