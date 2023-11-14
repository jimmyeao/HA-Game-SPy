using HA_Game_Spy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HA_Game_SPy
{
    public partial class MainWindow : Window
    {
        private bool isDarkTheme = false;
        private Settings settings;
        private MqttClientWrapper mqttClientWrapper;
        private List<GameInfo> games;
        private string currentDetectedGame = "";
        private string deviceId;
        public MainWindow()
        {
            InitializeComponent();
            deviceId = Environment.MachineName; // Or generate a unique ID

            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }
            settings = new Settings();
            Loaded += async (sender, args) =>
            {
                games = await LoadGamesAsync();
                settings = await LoadSettingsAsync();
                if (!string.IsNullOrEmpty(settings.MqttAddress))
                {
                    mqttClientWrapper = new MqttClientWrapper(
                        "HAGameSpy",
                        settings.MqttAddress,
                        settings.MqttUsername,
                        settings.EncryptedMqttPassword);

                    try
                    {
                        await mqttClientWrapper.ConnectAsync();
                        if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
                        {
                            mqttStatusText.Text = "MQTT Status: Connected";
                            await PublishSensorConfiguration();
                           // await mqttClientWrapper.PublishAsync(sensorConfigTopic, sensorConfigPayload);


                        }
                        else
                        {
                            mqttStatusText.Text = "MQTT Status: Disconnected";
                        }
                    }
                    catch
                    {
                        // Optionally handle connection failure on startup
                    }
                }
                _ = CheckForRunningGamesAsync();
                // Additional initialization using settings if needed
            };

            Closing += async (sender, args) =>
            {
                //send an empty state
                if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
                {
                    string stateTopic = $"HAGameSpy/{deviceId}/state";
                    string attributesTopic = $"HAGameSpy/{deviceId}/attributes";
                    var attributes = new { gamename = "None", gamelogourl = settings.IdleImage };
                    string attributesPayload = JsonConvert.SerializeObject(attributes);
                    if (settings.IdleImage != "")
                    {
                         attributes = new { gamename = "None", gamelogourl = settings.IdleImage };
                         attributesPayload = JsonConvert.SerializeObject(attributes);

                        await mqttClientWrapper.PublishAsync(stateTopic, "None");
                        await mqttClientWrapper.PublishAsync(attributesTopic, attributesPayload);
                    }
                    else
                    {
                         attributes = new { gamename = "None", gamelogourl = "https://i.gifer.com/4ZOQ.gif" };
                         attributesPayload = JsonConvert.SerializeObject(attributes);

                        await mqttClientWrapper.PublishAsync(stateTopic, "None");
                        await mqttClientWrapper.PublishAsync(attributesTopic, attributesPayload);
                    }   


                }
                //check if mqtt is connected and disconnect
                if (mqttClientWrapper != null)
                {
                    await mqttClientWrapper.DisconnectAsync();
                }
                await SaveSettingsAsync(settings);
            };
        }

        private async Task SaveSettingsAsync(Settings settings)
        {
            string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string settingsFilePath = Path.Combine(localFolder, "settings.json");
            settings.HomeAssistantUrl = txtHAUrl.Text;
            settings.EncryptedHAToken = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(txtHAToken.Text),null,    DataProtectionScope.CurrentUser));

            settings.EncryptedMqttPassword = Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(txtMqttPassword.Password),
                null,
                DataProtectionScope.CurrentUser));
            settings.StartMinimized = chkStartMinimized.IsChecked.Value;
            settings.StartWithWindows = chkStartWithWindows.IsChecked.Value;
            settings.Theme = isDarkTheme ? "Dark" : "Light";
            settings.MqttAddress = txtMqttAddress.Text;
            settings.MqttUsername = txtMqttUsername.Text;
            settings.IdleImage = txtIdleImageUrl.Text;
            string json = JsonConvert.SerializeObject(settings);
            await File.WriteAllTextAsync(settingsFilePath, json);
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;

            settings.Theme = isDarkTheme ? "Dark" : "Light";
            _ = SaveSettingsAsync(settings);

            var darkThemeUri = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml");
            var lightThemeUri = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");

            var themeUri = isDarkTheme ? darkThemeUri : lightThemeUri;

            System.Diagnostics.Debug.WriteLine("Before toggle:");
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                System.Diagnostics.Debug.WriteLine($" - {dictionary.Source}");
            }

            var existingTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == themeUri);
            if (existingTheme == null)
            {
                existingTheme = new ResourceDictionary() { Source = themeUri };
                Application.Current.Resources.MergedDictionaries.Add(existingTheme);
            }

            // Remove the current theme
            var currentTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == (isDarkTheme ? lightThemeUri : darkThemeUri));
            if (currentTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(currentTheme);
            }

            System.Diagnostics.Debug.WriteLine("After toggle:");
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                System.Diagnostics.Debug.WriteLine($" - {dictionary.Source}");
            }
            //_ = SaveSettingsAsync(settings);
        }

        private void chkStartMinimized_Checked(object sender, RoutedEventArgs e)
        {
        }

        private async Task<Settings> LoadSettingsAsync()
        {
            string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string settingsFilePath = Path.Combine(localFolder, "settings.json");

            Settings settings;
            if (File.Exists(settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(settingsFilePath);
                settings = JsonConvert.DeserializeObject<Settings>(json);
                txtHAUrl.Text = settings.HomeAssistantUrl;
                if (settings.EncryptedHAToken != null)
                {
                    txtHAToken.Text = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                        Convert.FromBase64String(settings.EncryptedHAToken),
                        null,
                        DataProtectionScope.CurrentUser));
                }

                if (settings.EncryptedMqttPassword != null)
                {
                    txtMqttPassword.Password = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                        Convert.FromBase64String(settings.EncryptedMqttPassword),
                        null,
                        DataProtectionScope.CurrentUser));
                }

                chkStartMinimized.IsChecked = settings.StartMinimized;
                chkStartWithWindows.IsChecked = settings.StartWithWindows;
                txtMqttUsername.Text = settings.MqttUsername;
                txtIdleImageUrl.Text = settings.IdleImage;
                txtMqttAddress.Text = settings.MqttAddress;
                if (settings.Theme == "Dark")
                {
                    ToggleThemeButton_Click(null, null);
                }
            }
            else
            {
                // If the settings file doesn't exist, use defaults
                settings = new Settings { Theme = "Default" };
            }

            return settings;
        }

        private async void btnConnectMqtt_Click(object sender, RoutedEventArgs e)
        {
            if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
            {
                MessageBox.Show("Already connected to MQTT server.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                mqttClientWrapper = new MqttClientWrapper(
                    "HAGameSpy",
                    settings.MqttAddress,
                    settings.MqttUsername,
                    settings.EncryptedMqttPassword);

                await mqttClientWrapper.ConnectAsync();
                MessageBox.Show("Connected to MQTT server successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to MQTT server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
            {
                mqttStatusText.Text = "MQTT Status: Connected";
            }
            else
            {
                mqttStatusText.Text = "MQTT Status: Disconnected";
            }
        }

        private async Task<List<GameInfo>> LoadGamesAsync()
        {
            // Get the directory where the application is running
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            if (File.Exists(gamesFilePath))
            {
                string json = await File.ReadAllTextAsync(gamesFilePath);
                return JsonConvert.DeserializeObject<List<GameInfo>>(json);
            }

            return new List<GameInfo>(); // Return an empty list if file doesn't exist
        }

        private async Task CheckForRunningGamesAsync()
        {
            while (true)
            {
                var runningProcesses = Process.GetProcesses();
                var detectedGame = games.FirstOrDefault(game =>
                    runningProcesses.Any(p =>
                        p.ProcessName.Equals(Path.GetFileNameWithoutExtension(game.ExecutableName), StringComparison.OrdinalIgnoreCase)));

                if (detectedGame != null)
                {
                    if (currentDetectedGame != detectedGame.GameName)
                    {
                        currentDetectedGame = detectedGame.GameName;
                        UpdateUIAndPublishGame(detectedGame); // Pass the GameInfo object
                    }
                }
                else if (!string.IsNullOrEmpty(currentDetectedGame))
                {
                    currentDetectedGame = "";
                    var noGame = new GameInfo { GameName = "None", LogoUrl = "" };
                    UpdateUIAndPublishGame(noGame); // Pass a GameInfo object for "None"
                }

                await Task.Delay(5000); // Check every 5 seconds
            }
        }


        private void UpdateUIAndPublishGame(GameInfo game)
        {
            Dispatcher.Invoke(() =>
            {
                // Debug statement to check if this block is executed
                Debug.WriteLine($"Updating UI for game: {game.GameName}");

                detectedGameText.Text = $"Detected Game: {game.GameName}";
                // Other UI updates
            });
            string stateTopic = $"HAGameSpy/{deviceId}/state";
            string attributesTopic = $"HAGameSpy/{deviceId}/attributes";

            var attributes = new { gamename = game.GameName, gamelogourl = game.LogoUrl, device_id = deviceId };
            string attributesPayload = JsonConvert.SerializeObject(attributes);

            _ = mqttClientWrapper.PublishAsync(stateTopic, game.GameName); // Publish game name as state
            _ = mqttClientWrapper.PublishAsync(attributesTopic, attributesPayload); // Publish attributes including device_id
        }


        private async Task PublishSensorConfiguration()
        {
            var sensorConfig = new
            {
                name = $"HAGameSpy {deviceId}",
                state_topic = $"HAGameSpy/{deviceId}/state",
                json_attributes_topic = $"HAGameSpy/{deviceId}/attributes",
                unique_id = $"hagamespy_{deviceId}",
                device = new
                {
                    identifiers = new string[] { $"hagamespy_{deviceId}" },
                    name = "HAGameSpy",
                    manufacturer = "Jimmy White",
                    model = "0.0.1"
                },
                device_id = deviceId // Custom attribute for device ID

            };

            string sensorConfigTopic = $"homeassistant/sensor/{deviceId}/config";
            string sensorConfigPayload = JsonConvert.SerializeObject(sensorConfig);
            await mqttClientWrapper.PublishAsync(sensorConfigTopic, sensorConfigPayload);
        }

        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            GameAddWindow gameAddWindow = new GameAddWindow();
            gameAddWindow.ShowDialog();
        }

        private void ListGame_Click(object sender, RoutedEventArgs e)
        {
            GameListWindow gameListWindow = new GameListWindow();
            gameListWindow.ShowDialog();
        }
    }
}