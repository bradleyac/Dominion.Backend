namespace Dominion.Backend.Test;

public class ChapelTests
{
  [Fact]
  public void Chapel_TrashesSelectedCards()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var chapel = CardInstance.CreateByCardId(14); // Chapel
    var copper1 = CardInstance.CreateByCardId(5);
    var copper2 = CardInstance.CreateByCardId(5);
    var estate = CardInstance.CreateByCardId(8);
    // Add Chapel, 2 Copper, 1 Estate to hand
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Deck = [],
      Hand = [chapel, copper1, copper2, estate],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Chapel
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, chapel.Id, Hand);
    // Simulate trashing both Coppers
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    var result = new PlayerSelectChoiceResult { SelectedCards = [copper1, copper2] };
    var afterTrash = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterTrash.GetPlayer(playerId);
    // Both Coppers should be gone from hand, not in discard, not in deck, and in Trash
    Assert.DoesNotContain(updatedPlayer.Hand, c => c.Card.Id == 5);
    Assert.DoesNotContain(updatedPlayer.Discard, c => c.Card.Id == 5);
    Assert.DoesNotContain(updatedPlayer.Deck, c => c.Card.Id == 5);
    Assert.Equivalent(afterTrash.Trash, new[] { copper1, copper2 });
    // Estate should remain in hand
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 8);
  }

  [Fact]
  public void Chapel_TrashesUpToFourCards()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var chapel = CardInstance.CreateByCardId(14); // Chapel
    var copper1 = CardInstance.CreateByCardId(5);
    var copper2 = CardInstance.CreateByCardId(5);
    var copper3 = CardInstance.CreateByCardId(5);
    var copper4 = CardInstance.CreateByCardId(5);
    // Add Chapel and 4 Coppers to hand
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Deck = [],
      Hand = [chapel, copper1, copper2, copper3, copper4],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Chapel
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, chapel.Id, Hand);
    // Simulate trashing all 4 Coppers
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    var result = new PlayerSelectChoiceResult { SelectedCards = [copper1, copper2, copper3, copper4] };
    var afterTrash = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterTrash.GetPlayer(playerId);
    // All Coppers should be gone from hand, not in discard, not in deck, and in Trash
    Assert.DoesNotContain(updatedPlayer.Hand, c => c.Card.Id == 5);
    Assert.DoesNotContain(updatedPlayer.Discard, c => c.Card.Id == 5);
    Assert.DoesNotContain(updatedPlayer.Deck, c => c.Card.Id == 5);
    Assert.Equivalent(afterTrash.Trash, new[] { copper1, copper2, copper3, copper4 });
  }

  [Fact]
  public void Chapel_TrashesNoCardsIfNoneSelected()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var chapel = CardInstance.CreateByCardId(14); // Chapel
    var copper = CardInstance.CreateByCardId(5);
    var estate = CardInstance.CreateByCardId(8);
    // Add Chapel, Copper, Estate to hand
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [chapel, copper, estate],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Chapel
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, chapel.Id, Hand);
    // Simulate trashing no cards
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    var result = new PlayerSelectChoiceResult { SelectedCards = [] };
    var afterTrash = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterTrash.GetPlayer(playerId);
    // All cards should remain in hand except Chapel (now in play), Trash should be empty
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 5); // Copper
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 8); // Estate
    Assert.Empty(afterTrash.Trash);
  }
}