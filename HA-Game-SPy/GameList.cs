using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Newtonsoft.Json;

namespace HA_Game_Spy
{
    public class GameList
    {
        private List<GameInfo> games;

        public GameList(string filePath)
        {
            LoadGames(filePath);
        }

        private void LoadGames(string filePath)
        {
            string json = File.ReadAllText(filePath);
            games = JsonConvert.DeserializeObject<List<GameInfo>>(json);
        }

        public bool IsGameKnown(string executableName)
        {
            return games.Exists(game => game.ExecutableName.Equals(executableName, StringComparison.OrdinalIgnoreCase));
        }
    }
}

