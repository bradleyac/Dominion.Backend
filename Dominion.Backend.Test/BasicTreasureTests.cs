namespace Dominion.Backend.Test;

public class TreasureTests
{
  [Fact]
  public void Copper_WhenPlayed_GivesOneCoin()
  {
    // Arrange
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;

    var copper = CardInstance.CreateByCardId(5); // Copper

    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [copper],
      Resources = p.Resources with { Coins = 0 }
    });

    // Act
    var (newState, _) = GameLogic.PlayCard(gameState, playerId, copper.Id, Hand);
    var updatedPlayer = newState.GetPlayer(playerId);

    // Assert
    Assert.Equal(1, updatedPlayer.Resources.Coins); // +1 coin
    Assert.Empty(updatedPlayer.Hand);
    Assert.Single(updatedPlayer.Play);
    Assert.Equal(5, updatedPlayer.Play[0].Card.Id); // Copper card ID
  }

  [Fact]
  public void Silver_WhenPlayed_GivesTwoCoins()
  {
    // Arrange
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;

    var silver = CardInstance.CreateByCardId(6); // Silver

    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [silver],
      Resources = p.Resources with { Coins = 0 }
    });

    // Act
    var (newState, _) = GameLogic.PlayCard(gameState, playerId, silver.Id, Hand);
    var updatedPlayer = newState.GetPlayer(playerId);

    // Assert
    Assert.Equal(2, updatedPlayer.Resources.Coins); // +2 coins
    Assert.Empty(updatedPlayer.Hand);
    Assert.Single(updatedPlayer.Play);
    Assert.Equal(6, updatedPlayer.Play[0].Card.Id); // Silver card ID
  }

  [Fact]
  public void Gold_WhenPlayed_GivesThreeCoins()
  {
    // Arrange
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameLogic.StartGame(gameState);
    var playerId = gameState.Players[0].Id;

    var gold = CardInstance.CreateByCardId(7); // Gold

    gameState = gameState.UpdatePlayer(playerId, p => p with
    {
      Hand = [gold],
      Resources = p.Resources with { Coins = 0 }
    });

    // Act
    var (newState, _) = GameLogic.PlayCard(gameState, playerId, gold.Id, Hand);
    var updatedPlayer = newState.GetPlayer(playerId);

    // Assert
    Assert.Equal(3, updatedPlayer.Resources.Coins); // +3 coins
    Assert.Empty(updatedPlayer.Hand);
    Assert.Single(updatedPlayer.Play);
    Assert.Equal(7, updatedPlayer.Play[0].Card.Id); // Gold card ID
  }
}