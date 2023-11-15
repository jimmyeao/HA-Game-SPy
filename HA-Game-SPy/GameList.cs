using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Newtonsoft.Json;

namespace HA_Game_Spy
{
    // This class represents a list of games
    public class GameList
    {
        private List<GameInfo> games; // Private field to store the list of games

        // Constructor that takes a file path and loads the games from the file
        public GameList(string filePath)
        {
            LoadGames(filePath);
        }

        // Method to load the games from a JSON file
        private void LoadGames(string filePath)
        {
            string json = File.ReadAllText(filePath); // Read the contents of the file
            games = JsonConvert.DeserializeObject<List<GameInfo>>(json); // Deserialize the JSON into a list of GameInfo objects
        }

        // Method to check if a game with a given executable name exists in the list
        public bool IsGameKnown(string executableName)
        {
            return games.Exists(game => game.ExecutableName.Equals(executableName, StringComparison.OrdinalIgnoreCase));
        }
    }
}


