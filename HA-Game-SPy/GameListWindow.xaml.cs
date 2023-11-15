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
        // This class represents a window that displays a list of games
        public GameListWindow()
        {
            InitializeComponent();
            LoadGames(); // Load the games when the window is initialized
        }

        // This method loads the games asynchronously
        private async void LoadGames()
        {
            var games = await LoadGamesAsync(); // Use the LoadGamesAsync method to load the games
            gamesDataGrid.ItemsSource = games; // Set the items source of the data grid to the loaded games
        }

        // This method loads the games asynchronously from a JSON file
        private async Task<List<GameInfo>> LoadGamesAsync()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory; // Get the base directory of the application
            string gamesFilePath = Path.Combine(appDirectory, "games.json"); // Combine the base directory with the file name

            if (File.Exists(gamesFilePath)) // Check if the file exists
            {
                string json = await File.ReadAllTextAsync(gamesFilePath); // Read the contents of the file
                return JsonConvert.DeserializeObject<List<GameInfo>>(json); // Deserialize the JSON into a list of GameInfo objects
            }

            return new List<GameInfo>(); // Return an empty list if the file doesn't exist
        }

        // This method is called when the "Add New Game" button is clicked
        private void AddNewGame_Click(object sender, RoutedEventArgs e)
        {
            GameAddWindow addWindow = new GameAddWindow(); // Create a new instance of the GameAddWindow
            addWindow.ShowDialog(); // Show the window as a dialog
            LoadGames(); // Reload the games after adding a new game
        }

        // This method is called when the "Save Changes" button is clicked
        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var games = gamesDataGrid.ItemsSource as List<GameInfo>; // Get the games from the data grid
            if (games != null)
            {
                await SaveGamesAsync(games); // Save the games asynchronously
            }
        }

        // This method saves the games asynchronously to a JSON file
        private async Task SaveGamesAsync(List<GameInfo> games)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory; // Get the base directory of the application
            string gamesFilePath = Path.Combine(appDirectory, "games.json"); // Combine the base directory with the file name

            // Filter out games with null or empty GameName, ExecutableName, or LogoUrl
            var validGames = games.Where(game => !string.IsNullOrEmpty(game.GameName)
                                              && !string.IsNullOrEmpty(game.ExecutableName)).ToList();

            string json = JsonConvert.SerializeObject(validGames, Formatting.Indented); // Serialize the valid games into JSON
            await File.WriteAllTextAsync(gamesFilePath, json); // Write the JSON to the file
        }


    }

}
