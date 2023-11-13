using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HA_Game_Spy
{
    public class GameWatcher
    {
        private GameList gameList;

        public GameWatcher(GameList gameList)
        {
            this.gameList = gameList;
        }

        public void WatchGames()
        {
            // Implement logic to watch for new processes and check against the gameList
        }
    }
}

