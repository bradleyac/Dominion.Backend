namespace Dominion.Backend.Test;

public class MarketTests
{
    [Fact]
    public void Market_WhenPlayed_GivesAllBonusesAndDrawsOneCard()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        // Create Market and Copper card
        var marketCard = CardInstance.CreateByCardId(4); // Market
        var copperCard = CardInstance.CreateByCardId(5); // Copper for drawing

        // Add Market to hand and Copper to deck
        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [marketCard],
            Deck = [copperCard],
            Resources = p.Resources with { Actions = 1, Buys = 1, Coins = 0 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, marketCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 for playing, net 1
        Assert.Equal(2, updatedPlayer.Resources.Buys);    // +1 buy
        Assert.Equal(1, updatedPlayer.Resources.Coins);   // +1 coin
        Assert.Single(updatedPlayer.Hand);                // Drew 1 card (Copper)
        Assert.Empty(updatedPlayer.Deck);                 // Deck should be empty
        Assert.Single(updatedPlayer.Play);                // Market should be in play area
        Assert.Equal(4, updatedPlayer.Play[0].Card.Id);   // Market card ID
    }

    [Fact]
    public void Market_WhenPlayed_WithEmptyDeck_GivesBonusesNoCardDraw()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        var marketCard = CardInstance.CreateByCardId(4); // Market

        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [marketCard],
            Deck = [],
            Resources = p.Resources with { Actions = 1, Buys = 1, Coins = 0 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, marketCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 for playing, net 1
        Assert.Equal(2, updatedPlayer.Resources.Buys);    // +1 buy
        Assert.Equal(1, updatedPlayer.Resources.Coins);   // +1 coin
        Assert.Empty(updatedPlayer.Hand);                 // No cards drawn
        Assert.Empty(updatedPlayer.Deck);                 // Deck should still be empty
        Assert.Single(updatedPlayer.Play);                // Market should be in play area
        Assert.Equal(4, updatedPlayer.Play[0].Card.Id);   // Market card ID
    }
}