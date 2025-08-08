namespace Dominion.Backend.Test;

public class ArtisanTests
{
  [Fact]
  public void Artisan_GainsCardToHand_ThenPutsCardOnDeck()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var artisan = CardInstance.CreateByCardId(13); // Artisan
    var copper = CardInstance.CreateByCardId(5);
    var estate = CardInstance.CreateByCardId(8);
    // Add Artisan, Copper, Estate to hand
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [artisan, copper, estate],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Artisan
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, artisan.Id, Hand);
    // First choice: gain Silver (id 6) from supply to hand
    var gainChoice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(gainChoice);
    var gainResult = new PlayerSelectChoiceResult { SelectedCards = new[] { CardInstance.CreateByCardId(6) } };
    var afterGain = GameLogic.ProcessEffectStack(afterPlay, gainResult);
    // Second choice: put Estate on top of deck
    var putChoice = afterGain.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(putChoice);
    var putResult = new PlayerSelectChoiceResult { SelectedCards = new[] { estate } };
    var afterPut = GameLogic.ProcessEffectStack(afterGain, putResult);
    var updatedPlayer = afterPut.GetPlayer(playerId);
    // Silver should be in hand, Estate should be on top of deck
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 6); // Silver
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 5); // Copper
    Assert.DoesNotContain(updatedPlayer.Hand, c => c.Card.Id == 8); // Estate
    Assert.Equal(8, updatedPlayer.Deck.First().Card.Id); // Estate on top
  }

  [Fact]
  public void Artisan_GainsCardToHand_ThenPutsGainedCardOnDeck()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var artisan = CardInstance.CreateByCardId(13); // Artisan
    var copper = CardInstance.CreateByCardId(5);
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [artisan, copper],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Artisan
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, artisan.Id, Hand);
    // First choice: gain Silver (id 6) from supply to hand
    var gainChoice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(gainChoice);
    var gainResult = new PlayerSelectChoiceResult { SelectedCards = new[] { CardInstance.CreateByCardId(6) } };
    var afterGain = GameLogic.ProcessEffectStack(afterPlay, gainResult);
    // Second choice: put Silver (gained) on top of deck
    var putChoice = afterGain.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(putChoice);
    var silver = afterGain.GetPlayer(playerId).Hand.First(c => c.Card.Id == 6);
    var putResult = new PlayerSelectChoiceResult { SelectedCards = new[] { silver } };
    var afterPut = GameLogic.ProcessEffectStack(afterGain, putResult);
    var updatedPlayer = afterPut.GetPlayer(playerId);
    // Silver should be on top of deck, Copper in hand
    Assert.Contains(updatedPlayer.Hand, c => c.Card.Id == 5); // Copper
    Assert.DoesNotContain(updatedPlayer.Hand, c => c.Card.Id == 6); // Silver
    Assert.Equal(6, updatedPlayer.Deck.First().Card.Id); // Silver on top
  }

  [Fact]
  public void Artisan_OnlyGainedCardInHand_MustPutItOnDeck()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var artisan = CardInstance.CreateByCardId(13); // Artisan
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [artisan],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Artisan
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, artisan.Id, Hand);
    // First choice: gain Silver (id 6) from supply to hand
    var gainChoice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(gainChoice);
    var gainResult = new PlayerSelectChoiceResult { SelectedCards = new[] { CardInstance.CreateByCardId(6) } };
    var afterGain = GameLogic.ProcessEffectStack(afterPlay, gainResult);
    // Second choice: must put Silver on top of deck
    var putChoice = afterGain.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(putChoice);
    var silver = afterGain.GetPlayer(playerId).Hand.First(c => c.Card.Id == 6);
    var putResult = new PlayerSelectChoiceResult { SelectedCards = new[] { silver } };
    var afterPut = GameLogic.ProcessEffectStack(afterGain, putResult);
    var updatedPlayer = afterPut.GetPlayer(playerId);
    // Silver should be on top of deck, hand should be empty
    Assert.DoesNotContain(updatedPlayer.Hand, c => c.Card.Id == 6);
    Assert.Equal(6, updatedPlayer.Deck.First().Card.Id); // Silver on top
  }
}
