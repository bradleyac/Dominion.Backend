namespace Dominion.Backend.Test;

public class WorkshopTests
{
  [Fact]
  public void Workshop_GainsCardUpToCostFourToDiscard()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;
    var workshop = CardInstance.CreateByCardId(16); // Workshop
                                                    // Add Workshop to hand
    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [workshop],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Workshop
    var (afterPlay, _) = GameLogic.PlayCard(gameState, playerId, workshop.Id, Hand);
    // Simulate selecting Estate (cost 2) from supply
    var choice = afterPlay.GetPlayer(playerId).ActiveChoice as PlayerSelectChoice;
    Assert.NotNull(choice);
    var estate = CardInstance.CreateByCardId(8);
    var result = new PlayerSelectChoiceResult { SelectedCards = new[] { estate } };
    var afterGain = GameLogic.ProcessEffectStack(afterPlay, result);
    var updatedPlayer = afterGain.GetPlayer(playerId);
    // Estate should be in discard
    Assert.Contains(updatedPlayer.Discard, c => c.Card.Id == 8);
  }
}
