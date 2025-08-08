namespace Dominion.Backend.Test;

public class VillageTests
{
    [Fact]
    public void Village_WhenPlayed_GivesTwoActionsAndOneCard()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        // Create Village and Copper cards
        var villageCard = CardInstance.CreateByCardId(1); // Village
        var copperCard = CardInstance.CreateByCardId(5); // Copper for drawing

        // Add Village to player's hand and Copper to deck
        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [villageCard],
            Deck = [copperCard],
            Resources = p.Resources with { Actions = 1 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, villageCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(2, updatedPlayer.Resources.Actions); // Started with 1, 1 used to play the card, gained 2
        Assert.Single(updatedPlayer.Hand); // Should have drawn 1 card (Copper)
        Assert.Empty(updatedPlayer.Deck); // Deck should be empty after drawing
        Assert.Single(updatedPlayer.Play); // Village should be in play area
        Assert.Equal(1, updatedPlayer.Play[0].Card.Id); // Village card ID
    }

    [Fact]
    public void Village_WhenPlayed_StillGivesTwoActionsWhenDeckEmpty()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        // Create Village card
        var villageCard = CardInstance.CreateByCardId(1); // Village

        // Add Village to player's hand with empty deck and discard
        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [villageCard],
            Deck = [],
            Discard = [],
            Resources = p.Resources with { Actions = 1 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, villageCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(2, updatedPlayer.Resources.Actions); // Started with 1, 1 used to play the card, gained 2
        Assert.Empty(updatedPlayer.Hand); // No cards to draw
        Assert.Empty(updatedPlayer.Deck); // Deck should still be empty
        Assert.Single(updatedPlayer.Play); // Village should be in play area
        Assert.Equal(1, updatedPlayer.Play[0].Card.Id); // Village card ID
    }
}
