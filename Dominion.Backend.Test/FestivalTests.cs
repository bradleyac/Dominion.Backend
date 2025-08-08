namespace Dominion.Backend.Test;

public class FestivalTests
{
    [Fact]
    public void Festival_WhenPlayed_GivesTwoActionsOneBuyTwoCoins()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        // Create Festival card
        var festivalCard = CardInstance.CreateByCardId(3); // Festival

        // Add Festival to player's hand
        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [festivalCard],
            Resources = p.Resources with { Actions = 1, Buys = 1, Coins = 0 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, festivalCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(2, updatedPlayer.Resources.Actions); // +2 actions, -1 for playing, net +1
        Assert.Equal(2, updatedPlayer.Resources.Buys);    // +1 buy
        Assert.Equal(2, updatedPlayer.Resources.Coins);   // +2 coins
        Assert.Empty(updatedPlayer.Hand);                 // No cards drawn
        Assert.Single(updatedPlayer.Play);                // Festival should be in play area
        Assert.Equal(3, updatedPlayer.Play[0].Card.Id);   // Festival card ID
    }

    [Fact]
    public void Festival_WhenPlayed_WithEmptyDeck_StillGivesBonuses()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        var festivalCard = CardInstance.CreateByCardId(3); // Festival

        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [festivalCard],
            Deck = [],
            Resources = p.Resources with { Actions = 1, Buys = 1, Coins = 0 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, festivalCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(2, updatedPlayer.Resources.Actions); // +2 actions, -1 for playing, net +1
        Assert.Equal(2, updatedPlayer.Resources.Buys);    // +1 buy
        Assert.Equal(2, updatedPlayer.Resources.Coins);   // +2 coins
        Assert.Empty(updatedPlayer.Hand);                 // No cards drawn
        Assert.Single(updatedPlayer.Play);                // Festival should be in play area
        Assert.Equal(3, updatedPlayer.Play[0].Card.Id);   // Festival card ID
    }
}