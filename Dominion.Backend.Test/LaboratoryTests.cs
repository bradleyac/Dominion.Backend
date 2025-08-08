namespace Dominion.Backend.Test;

public class LaboratoryTests
{
    [Fact]
    public void Laboratory_WhenPlayed_GivesOneActionAndDrawsTwoCards()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        // Create Laboratory and two Copper cards
        var labCard = CardInstance.CreateByCardId(2); // Laboratory
        var copper1 = CardInstance.CreateByCardId(5);
        var copper2 = CardInstance.CreateByCardId(5);

        // Add Laboratory to hand and two Coppers to deck
        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [labCard],
            Deck = [copper1, copper2],
            Resources = p.Resources with { Actions = 1 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, labCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 for playing, net 1
        Assert.Equal(2, updatedPlayer.Hand.Length); // Drew 2 cards
        Assert.Empty(updatedPlayer.Deck); // Deck should be empty
        Assert.Single(updatedPlayer.Play); // Laboratory should be in play area
        Assert.Equal(2, updatedPlayer.Play[0].Card.Id); // Laboratory card ID
    }

    [Fact]
    public void Laboratory_WhenPlayed_WithOneCardInDeck_DrawsOneCard()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        var labCard = CardInstance.CreateByCardId(2); // Laboratory
        var copper1 = CardInstance.CreateByCardId(5);

        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [labCard],
            Deck = [copper1],
            Resources = p.Resources with { Actions = 1 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, labCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 for playing, net 1
        Assert.Single(updatedPlayer.Hand); // Only 1 card drawn
        Assert.Empty(updatedPlayer.Deck); // Deck should be empty
        Assert.Single(updatedPlayer.Play); // Laboratory should be in play area
        Assert.Equal(2, updatedPlayer.Play[0].Card.Id); // Laboratory card ID
    }

    [Fact]
    public void Laboratory_WhenPlayed_WithEmptyDeck_DrawsNoCards()
    {
        // Arrange
        var gameState = GameFactory.CreateGameState("player1");
        gameState = GameLogic.StartGame(gameState);
        var playerId = gameState.Players[0].Id;

        var labCard = CardInstance.CreateByCardId(2); // Laboratory

        gameState = gameState.UpdatePlayer(playerId, p => p with
        {
            Hand = [labCard],
            Deck = [],
            Resources = p.Resources with { Actions = 1 }
        });

        // Act
        var (newState, _) = GameLogic.PlayCard(gameState, playerId, labCard.Id, Hand);
        var updatedPlayer = newState.GetPlayer(playerId);

        // Assert
        Assert.Equal(1, updatedPlayer.Resources.Actions); // +1 action, -1 for playing, net 1
        Assert.Empty(updatedPlayer.Hand); // No cards drawn
        Assert.Empty(updatedPlayer.Deck); // Deck should still be empty
        Assert.Single(updatedPlayer.Play); // Laboratory should be in play area
        Assert.Equal(2, updatedPlayer.Play[0].Card.Id); // Laboratory card ID
    }
}