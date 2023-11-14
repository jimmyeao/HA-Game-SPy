using HA_Game_Spy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace HA_Game_SPy
{
    /// <summary>
    /// Interaction logic for GameListWindow.xaml
    /// </summary>
    public partial class GameListWindow : Window
    {
        public GameListWindow()
        {
            InitializeComponent();
            LoadGames();
        }

        private async void LoadGames()
        {
            var games = await LoadGamesAsync(); // Use the same LoadGamesAsync method
            gamesDataGrid.ItemsSource = games;
        }
         private async Task<List<GameInfo>> LoadGamesAsync()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            if (File.Exists(gamesFilePath))
            {
                string json = await File.ReadAllTextAsync(gamesFilePath);
                return JsonConvert.DeserializeObject<List<GameInfo>>(json);
            }

            return new List<GameInfo>(); // Return an empty list if file doesn't exist
        }
        private void AddNewGame_Click(object sender, RoutedEventArgs e)
        {
            GameAddWindow addWindow = new GameAddWindow();
            addWindow.ShowDialog();
            LoadGames(); // Reload games after adding
        }

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var games = gamesDataGrid.ItemsSource as List<GameInfo>;
            if (games != null)
            {
                await SaveGamesAsync(games);
            }
        }
        private async Task SaveGamesAsync(List<GameInfo> games)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            // Filter out games with null or empty GameName, ExecutableName, or LogoUrl
            var validGames = games.Where(game => !string.IsNullOrEmpty(game.GameName)
                                              && !string.IsNullOrEmpty(game.ExecutableName)).ToList();

            string json = JsonConvert.SerializeObject(validGames, Formatting.Indented);
            await File.WriteAllTextAsync(gamesFilePath, json);
        }


    }

}
