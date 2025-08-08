using System;

namespace Dominion.Backend;

public static class WhimsicalNameFactory
{
  private static readonly string[] Adjectives =
  [
    "Amused", "Bubbly", "Clumsy", "Dapper", "Dizzy", "Drowsy", "Fluffy", "Frolicsome", "Funky", "Giggly",
    "Goofy", "Grinning", "Jolly", "Jumpy", "Loopy", "Lucky", "Mirthful", "Oddball", "Breezy", "Perky",
    "Waggish", "Jazzy", "Dandy", "Snazzy", "Snuggly", "Spunky", "Spry", "Periwinkle", "Snappy", "Twinkly"
  ];

  private static readonly string[] Colors =
  [
    "Periwinkle", "Chartreuse", "Cerulean", "Mauve", "Coral", "Amber", "Mint", "Lavender", "Turquoise", "Magenta",
    "Tangerine", "Azure", "Fuchsia", "Saffron", "Teal", "Indigo", "Peach", "Vermilion", "Celadon", "Lilac",
    "Salmon", "Jade", "Marigold", "Cyan", "Plum", "Rose", "Lemon", "Emerald", "Aqua", "Blush"
  ];

  private static readonly string[] Animals =
  [
    "Alpaca", "Armadillo", "Badger", "Bat", "Capybara", "Chinchilla", "Dodo", "Duckling", "Ferret",
    "Flamingo", "Gecko", "Giraffe", "Hedgehog", "Hippo", "Kitten", "Lemur", "Llama", "Manatee", "Meerkat", "Narwhal",
    "Otter", "Pangolin", "Platypus", "Puffin", "Quokka", "Panda", "Sloth", "Squirrel", "Tapir", "Wombat"
  ];

  public static string GetName() => $"{Adjectives.GetRandomElement()} {Colors.GetRandomElement()} {Animals.GetRandomElement()}";
};
