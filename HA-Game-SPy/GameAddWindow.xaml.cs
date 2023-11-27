using HA_Game_Spy;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace HA_Game_SPy
{
    // This class represents a window for adding a new game
    public partial class GameAddWindow : Window
    {
        #region Public Constructors

        // Constructor for the GameAddWindow class
        public GameAddWindow()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        // Event handler for the AddGame button click event
        private async void AddGame_Click(object sender, RoutedEventArgs e)
        {
            // Create a new GameInfo object with the values entered in the text boxes
            var newGame = new GameInfo
            {
                GameName = txtGameName.Text,
                ExecutableName = txtExecutableName.Text,
                LogoUrl = txtImageUrl.Text
            };

            var games = await LoadGamesAsync(); // Load existing games
            games.Add(newGame); // Add the new game to the list

            await SaveGamesAsync(games); // Save the updated games list
                                         // After adding a game
            
           

        }

        // Event handler for the BrowseExecutable button click event
        private void BrowseExecutable_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtExecutableName.Text = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
        }

        // Asynchronously load the list of games from a JSON file
        private async Task<List<GameInfo>> LoadGamesAsync()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            if (File.Exists(gamesFilePath))
            {
                string json = await File.ReadAllTextAsync(gamesFilePath);
                return JsonConvert.DeserializeObject<List<GameInfo>>(json);
            }

            return new List<GameInfo>(); // Return an empty list if the file doesn't exist
        }

        // Asynchronously save the list of games to a JSON file
        private async Task SaveGamesAsync(List<GameInfo> games)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            string json = JsonConvert.SerializeObject(games, Formatting.Indented); // Using Formatting.Indented for readability
            await File.WriteAllTextAsync(gamesFilePath, json);
            // After adding a game


            this.Close(); // Close the window after saving
        }

        #endregion Private Methods
    }
}