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

    public partial class GameAddWindow : Window
    {
        #region Public Constructors

        public GameAddWindow()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        private async void AddGame_Click(object sender, RoutedEventArgs e)
        {
            var newGame = new GameInfo
            {
                GameName = txtGameName.Text,
                ExecutableName = txtExecutableName.Text,
                LogoUrl = txtImageUrl.Text
            };

            var games = await LoadGamesAsync(); // Load existing games
            games.Add(newGame);

            await SaveGamesAsync(games); // Save updated games list
        }

        private void BrowseExecutable_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtExecutableName.Text = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
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

        private async Task SaveGamesAsync(List<GameInfo> games)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            string json = JsonConvert.SerializeObject(games, Formatting.Indented); // Using Formatting.Indented for readability
            await File.WriteAllTextAsync(gamesFilePath, json);
        }

        #endregion Private Methods
    }
}