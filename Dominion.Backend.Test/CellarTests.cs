namespace Dominion.Backend.Test;

public class CellarTests
{
  [Fact]
  public void Cellar_WhenPlayedWithNoDiscards_GainsOneActionAndNoCardsDrawn()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var cellar = CardInstance.CreateByCardId(12); // Cellar
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [cellar],
      Resources = p.Resources with { Actions = 1 }
    });
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, cellar.Id, Hand);

    // Since the player has no available cards to discard, the choice is skipped.
    var updatedPlayer = afterPlay.GetPlayer(playerId);
    var choice = updatedPlayer.ActiveChoice as PlayerSelectChoice;

    Assert.Null(choice);
    Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 to play
    Assert.Empty(updatedPlayer.Discard); // No discards
    Assert.Empty(updatedPlayer.Hand); // No cards drawn
  }

  [Fact]
  public void Cellar_WhenPlayedWithOneDiscard_DrawsOneCard()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var cellar = CardInstance.CreateByCardId(12);
    var copper = CardInstance.CreateByCardId(5);
    var estate = CardInstance.CreateByCardId(8);
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [cellar, copper, estate],
      Deck = [CardInstance.CreateByCardId(6)], // Silver to draw
      Resources = p.Resources with { Actions = 1 }
    });
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, cellar.Id, Hand);
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    // Simulate discarding copper
    var result = new PlayerSelectChoiceResult { SelectedCards = new[] { copper } };
    var afterChoice = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterChoice.GetPlayer(playerId);
    Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 to play
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 6); // Drew Silver
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 8); // Estate remains
    Assert.Contains(updatedPlayer.Discard, c => c.Card.Id == 5); // Copper discarded
  }

  [Fact]
  public void Cellar_WhenPlayedWithAllCardsDiscarded_DrawsThatManyCards()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var cellar = CardInstance.CreateByCardId(12);
    var copper = CardInstance.CreateByCardId(5);
    var estate = CardInstance.CreateByCardId(8);
    var silver = CardInstance.CreateByCardId(6);
    var gold = CardInstance.CreateByCardId(7);
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [cellar, copper, estate],
      Deck = [silver, gold],
      Resources = p.Resources with { Actions = 1 }
    });
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, cellar.Id, Hand);
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    // Simulate discarding both
    var result = new PlayerSelectChoiceResult { SelectedCards = new[] { copper, estate } };
    var afterChoice = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterChoice.GetPlayer(playerId);
    Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 to play
    Assert.Equivalent(updatedPlayer.Hand, new[] { silver, gold });
    Assert.Equivalent(updatedPlayer.Discard, new[] { copper, estate });
  }

  [Fact]
  public void Cellar_WhenDrawingMoreThanDeck_ReshufflesDiscard()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var cellar = CardInstance.CreateByCardId(12);
    var copper = CardInstance.CreateByCardId(5);
    var estate = CardInstance.CreateByCardId(8);
    // No cards in or discard deck, so we will have to draw the cards we just discarded
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [cellar, copper, estate],
      Deck = [], // Empty deck
      Resources = p.Resources with { Actions = 1 }
    });
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, cellar.Id, Hand);
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    // Simulate discarding both
    var result = new PlayerSelectChoiceResult { SelectedCards = new[] { copper, estate } };
    var afterChoice = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterChoice.GetPlayer(playerId);
    // Should have drawn the discarded Copper and Estate after reshuffling
    Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 to play
    Assert.Equal(2, updatedPlayer.Hand.Length); // Drew 2
    Assert.Empty(updatedPlayer.Discard);
    Assert.Equivalent(updatedPlayer.Hand, new[] { copper, estate });
  }
}