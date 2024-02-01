using HA_Game_Spy;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using XamlAnimatedGif;

namespace HA_Game_SPy
{
    public partial class MainWindow : Window
    {
        #region Private Fields

        private string currentDetectedGame = "";
        private GameInfo currentgame = new GameInfo { GameName = "None", LogoUrl = "" };
        private string deviceId;
        private List<GameInfo> games;
        private bool isDarkTheme = false;
        private MqttClientWrapper mqttClientWrapper;
        private System.Timers.Timer mqttKeepAliveTimer;
        private Timer mqttPublishTimer;
        private Settings settings;

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the timer
            mqttKeepAliveTimer = new System.Timers.Timer(60000); // Set interval to 60 seconds (60000 ms)
            mqttKeepAliveTimer.Elapsed += OnTimedEvent;
            mqttKeepAliveTimer.AutoReset = true;
            mqttKeepAliveTimer.Enabled = true;

            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trans.ico");
            MyNotifyIcon.Icon = new System.Drawing.Icon(iconPath);
            deviceId = Environment.MachineName; // Or generate a unique ID

            if (Properties.Settings.Default.UpdateSettings)
            {
                // Upgrade settings if needed
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }

            settings = new Settings();
            InitializeMqttPublishTimer();
            Loaded += async (sender, args) =>
            {
                // Load games and settings asynchronously
                games = await LoadGamesAsync();
                settings = await LoadSettingsAsync();

                if (settings.StartMinimized)
                {
                    // Start the window minimized and hide it
                    WindowState = WindowState.Minimized;
                    Hide();
                    MyNotifyIcon.Visibility = Visibility.Visible; // Show the NotifyIcon in system tray
                }

                if (!string.IsNullOrEmpty(settings.MqttAddress))
                {
                    // Connect to MQTT broker if address is provided
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
                if (!string.IsNullOrWhiteSpace(settings.IdleImage))
                {
                    var idleGameInfo = new GameInfo { GameName = "None", LogoUrl = settings.IdleImage };
                    UpdateUIAndPublishGame(idleGameInfo);
                    currentgame = idleGameInfo; // Update currentgame to idle game info
                }
                else
                {
                    // Set to a default image or keep empty
                    UpdateUIAndPublishGame(new GameInfo { GameName = "None", LogoUrl = "" });
                }
                _ = CheckForRunningGamesAsync();
                // Additional initialization using settings if needed
            };

            Closing += async (sender, args) =>
            {
                // Send an empty state to MQTT broker
                if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
                {
                    string stateTopic = $"HAGameSpy/{deviceId}/state";
                    string attributesTopic = $"HAGameSpy/{deviceId}/attributes";
                    var attributes = new { gamename = "None", gamelogourl = settings.IdleImage };
                    string attributesPayload = JsonConvert.SerializeObject(attributes);

                    if (settings.IdleImage != "")
                    {
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

                // Disconnect from MQTT broker if connected
                if (mqttClientWrapper != null)
                {
                    await mqttClientWrapper.DisconnectAsync();
                }

                // Set application startup with Windows and save settings
                await SetStartupAsync(settings.StartWithWindows);
                await SaveSettingsAsync(settings);
            };
        }

        #endregion Public Constructors

        #region Protected Methods

        // This method is called when the form is closing
        protected override void OnClosing(CancelEventArgs e)
        {
            // Stop and dispose the MQTT keep alive timer if it exists
            mqttKeepAliveTimer?.Stop();
            mqttKeepAliveTimer?.Dispose();

            // Dispose the notify icon to clean up resources
            MyNotifyIcon.Dispose();

            // Call the base class's OnClosing method to perform any additional closing operations
            base.OnClosing(e);
        }

        // This method is called when the state of the window changes
        protected override void OnStateChanged(EventArgs e)
        {
            // Check if the window is minimized
            if (WindowState == WindowState.Minimized)
            {
                // Hide the window
                Hide();

                // Ensure the NotifyIcon is visible
                MyNotifyIcon.Visibility = Visibility.Visible;

                // Show a balloon tip to inform the user that the application is still running in
                // the background
                MyNotifyIcon.ShowBalloonTip("Application Minimized", "Your application is still running in the background.", BalloonIcon.Info);
            }
            else
            {
                // Optionally hide the icon when the window is normal/maximized
                MyNotifyIcon.Visibility = Visibility.Collapsed;
            }

            // Call the base implementation of the OnStateChanged method
            base.OnStateChanged(e);
        }

        #endregion Protected Methods

        #region Private Methods

        public async Task RefreshGamesListAsync()
        {
            games = await LoadGamesAsync();
        }

        //This method is called when the AddGame button is clicked
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            // Create a new instance of the GameAddWindow class
            GameAddWindow gameAddWindow = new GameAddWindow();

            // Show the gameAddWindow as a modal dialog
            gameAddWindow.ShowDialog();
            _ = RefreshGamesListAsync();
        }

        //Event handler for the button click event to connect to MQTT server
        private async void btnConnectMqtt_Click(object sender, RoutedEventArgs e)
        {
            // Check if already connected to MQTT server
            if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
            {
                MessageBox.Show("Already connected to MQTT server.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Create a new instance of MqttClientWrapper with the provided settings
                mqttClientWrapper = new MqttClientWrapper(
                    "HAGameSpy",
                    settings.MqttAddress,
                    settings.MqttUsername,
                    settings.EncryptedMqttPassword);

                // Connect to the MQTT server asynchronously
                await mqttClientWrapper.ConnectAsync();

                // Show success message if connected successfully
                MessageBox.Show("Connected to MQTT server successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Show error message if failed to connect to MQTT server
                MessageBox.Show($"Failed to connect to MQTT server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Update the MQTT status text based on the connection status
            if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
            {
                mqttStatusText.Text = "MQTT Status: Connected";
            }
            else
            {
                mqttStatusText.Text = "MQTT Status: Disconnected";
            }
        }

        private async Task CheckForRunningGamesAsync()
        {
            while (true)
            {
                var runningProcesses = Process.GetProcesses(); // Get all running processes
                var detectedGame = games.FirstOrDefault(game => // Find the first game in the list of games that matches a running process
                    runningProcesses.Any(p =>
                        p.ProcessName.Equals(Path.GetFileNameWithoutExtension(game.ExecutableName), StringComparison.OrdinalIgnoreCase)));

                if (detectedGame != null) // If a game is detected
                {
                    currentgame = detectedGame;
                    if (currentDetectedGame != detectedGame.GameName) // If the detected game is different from the current detected game
                    {
                        currentDetectedGame = detectedGame.GameName; // Update the current detected game
                        UpdateUIAndPublishGame(detectedGame); // Update the UI and publish the detected game
                    }
                }
                else if (!string.IsNullOrEmpty(currentDetectedGame)) // If no game is detected but there was a previously detected game
                {
                    currentDetectedGame = ""; // Reset the current detected game
                    if (settings.IdleImage != "") // If there is an idle image specified in the settings
                    {
                        currentgame = new GameInfo { GameName = "None", LogoUrl = settings.IdleImage };
                        var noGame = new GameInfo { GameName = "None", LogoUrl = settings.IdleImage }; // Create a GameInfo object for "None" with the idle image
                        UpdateUIAndPublishGame(noGame); // Update the UI and publish the "None" game
                    }
                    else // If there is no idle image specified in the settings
                    {
                        currentgame = new GameInfo { GameName = "None", LogoUrl = settings.IdleImage };
                        var noGame = new GameInfo { GameName = "None", LogoUrl = "" }; // Create a GameInfo object for "None" without a logo URL
                        UpdateUIAndPublishGame(noGame); // Update the UI and publish the "None" game
                    }
                }

                await Task.Delay(5000); // Wait for 5 seconds before checking again
            }
        }

        // This method checks the MQTT connection status and attempts to reconnect if necessary
        private async void CheckMqttConnection()
        {
            // Check if the MQTT client wrapper exists and is not connected
            if (mqttClientWrapper != null && !mqttClientWrapper.IsConnected)
            {
                try
                {
                    // Attempt to connect to the MQTT broker asynchronously
                    await mqttClientWrapper.ConnectAsync();
                }
                catch
                {
                    // Handle reconnection failure
                }
            }
        }

        private void chkStartMinimized_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void gameMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Only loop if the source is a GIF
            if (gameMediaElement.Source != null && gameMediaElement.Source.AbsoluteUri.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                gameMediaElement.Position = TimeSpan.Zero;
                gameMediaElement.Play();
            }
        }

        private void InitializeMqttPublishTimer()
        {
            mqttPublishTimer = new Timer(60000); // Set the interval to 60 seconds
            mqttPublishTimer.Elapsed += OnMqttPublishTimerElapsed;
            mqttPublishTimer.AutoReset = true; // Reset the timer after it elapses
            mqttPublishTimer.Enabled = true; // Enable the timer
        }

        // This method is called when the "ListGame" button is clicked
        private void ListGame_Click(object sender, RoutedEventArgs e)
        {
            // Create a new instance of the GameListWindow class
            GameListWindow gameListWindow = new GameListWindow();

            // Show the game list window as a modal dialog
            gameListWindow.ShowDialog();
            _ = RefreshGamesListAsync();
        }

        private async Task<List<GameInfo>> LoadGamesAsync()
        {
            // Get the directory where the application is running
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Combine the app directory with the file name to get the full file path
            string gamesFilePath = Path.Combine(appDirectory, "games.json");

            // Check if the file exists
            if (File.Exists(gamesFilePath))
            {
                // Read the contents of the file as a string
                string json = await File.ReadAllTextAsync(gamesFilePath);

                // Deserialize the JSON string into a list of GameInfo objects
                return JsonConvert.DeserializeObject<List<GameInfo>>(json);
            }

            // Return an empty list if the file doesn't exist
            return new List<GameInfo>();
        }

        // Method to load settings asynchronously
        private async Task<Settings> LoadSettingsAsync()
        {
            // Get the local application data folder path
            string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Combine the local folder path with the settings file name
            string settingsFilePath = Path.Combine(localFolder, "settings.json");

            Settings settings;
            // Check if the settings file exists
            if (File.Exists(settingsFilePath))
            {
                // Read the contents of the settings file
                string json = await File.ReadAllTextAsync(settingsFilePath);
                // Deserialize the JSON string into a Settings object
                settings = JsonConvert.DeserializeObject<Settings>(json);
                // Set the Home Assistant URL text box value

                // Check if the encrypted MQTT password exists
                if (settings.EncryptedMqttPassword != null)
                {
                    // Decrypt the encrypted MQTT password and set the MqttPassword password box value
                    txtMqttPassword.Password = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                        Convert.FromBase64String(settings.EncryptedMqttPassword),
                        null,
                        DataProtectionScope.CurrentUser));
                }

                // Set the StartMinimized check box value
                chkStartMinimized.IsChecked = settings.StartMinimized;
                // Set the StartWithWindows check box value
                chkStartWithWindows.IsChecked = settings.StartWithWindows;
                // Set the MqttUsername text box value
                txtMqttUsername.Text = settings.MqttUsername;
                // Set the IdleImageUrl text box value
                txtIdleImageUrl.Text = settings.IdleImage;
                // Set the MqttAddress text box value
                txtMqttAddress.Text = settings.MqttAddress;
                // Check if the theme is set to "Dark"
                if (settings.Theme == "Dark")
                {
                    // Call the ToggleThemeButton_Click event handler to toggle the theme
                    ToggleThemeButton_Click(null, null);
                }
            }
            else
            {
                // If the settings file doesn't exist, use defaults
                settings = new Settings { Theme = "Default" };
            }

            // Return the loaded settings
            return settings;
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // This method is called when the NotifyIcon is clicked
        private void MyNotifyIcon_Click(object sender, EventArgs e)
        {
            // Show the main window
            Show();

            // Set the window state to normal
            WindowState = WindowState.Normal;

            // Hide the NotifyIcon
            MyNotifyIcon.Visibility = Visibility.Collapsed;
        }

        private void OnMqttPublishTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (mqttClientWrapper != null && mqttClientWrapper.IsConnected)
            {
                // Publish your MQTT message here
                // Example: _ = mqttClientWrapper.PublishAsync("your/topic", "your message");
                UpdateUIAndPublishGame(currentgame);
            }
        }

        // This method is called when the timer event is triggered
        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Check the MQTT connection
            CheckMqttConnection();

            // Publish the sensor configuration
            UpdateUIAndPublishGame(currentgame);
        }

        // This method is used to publish the sensor configuration to the MQTT broker
        private async Task PublishSensorConfiguration()
        {
            // Create a dynamic object to hold the sensor configuration data
            var sensorConfig = new
            {
                name = $"HAGameSpy {deviceId}", // Set the name of the sensor
                state_topic = $"HAGameSpy/{deviceId}/state", // Set the topic for publishing the sensor state
                json_attributes_topic = $"HAGameSpy/{deviceId}/attributes", // Set the topic for publishing additional sensor attributes
                unique_id = $"hagamespy_{deviceId}", // Set a unique ID for the sensor
                device = new
                {
                    identifiers = new string[] { $"hagamespy_{deviceId}" }, // Set the identifiers for the device
                    name = "HAGameSpy", // Set the name of the device
                    manufacturer = "Jimmy White", // Set the manufacturer of the device
                    model = "0.0.1" // Set the model of the device
                },
                device_id = deviceId // Set a custom attribute for the device ID
            };

            // Set the topic for publishing the sensor configuration
            string sensorConfigTopic = $"homeassistant/sensor/{deviceId}/config";

            // Serialize the sensor configuration object to JSON
            string sensorConfigPayload = JsonConvert.SerializeObject(sensorConfig);

            // Publish the sensor configuration to the MQTT broker
            await mqttClientWrapper.PublishAsync(sensorConfigTopic, sensorConfigPayload);
        }

        // Method to save the settings to a JSON file asynchronously
        private async Task SaveSettingsAsync(Settings settings)
        {
            // Get the local application data folder path
            string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Combine the local folder path with the file name to create the settings file path
            string settingsFilePath = Path.Combine(localFolder, "settings.json");

            // Update the HomeAssistantUrl property of the settings object with the value from the
            // txtHAUrl TextBox

            // Encrypt the MqttPassword value from the txtMqttPassword PasswordBox and assign it to
            // the EncryptedMqttPassword property of the settings object
            settings.EncryptedMqttPassword = Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(txtMqttPassword.Password),
                null,
                DataProtectionScope.CurrentUser));

            // Assign the value of the chkStartMinimized CheckBox to the StartMinimized property of
            // the settings object
            settings.StartMinimized = chkStartMinimized.IsChecked.Value;

            // Assign the value of the chkStartWithWindows CheckBox to the StartWithWindows property
            // of the settings object
            settings.StartWithWindows = chkStartWithWindows.IsChecked.Value;

            // Assign the theme value based on the isDarkTheme boolean variable to the Theme
            // property of the settings object
            settings.Theme = isDarkTheme ? "Dark" : "Light";

            // Update the MqttAddress property of the settings object with the value from the
            // txtMqttAddress TextBox
            settings.MqttAddress = txtMqttAddress.Text;

            // Update the MqttUsername property of the settings object with the value from the
            // txtMqttUsername TextBox
            settings.MqttUsername = txtMqttUsername.Text;

            // Update the IdleImage property of the settings object with the value from the
            // txtIdleImageUrl TextBox
            settings.IdleImage = txtIdleImageUrl.Text;

            // Serialize the settings object to JSON format
            string json = JsonConvert.SerializeObject(settings);

            // Write the JSON string to the settings file asynchronously
            await File.WriteAllTextAsync(settingsFilePath, json);
        }

        // Method to set the application to start with Windows startup or not
        private async Task SetStartupAsync(bool startWithWindows)
        {
            await Task.Run(() =>
            {
                const string appName = "HA_Game_Spy"; // Your application's name
                string exePath = System.Windows.Forms.Application.ExecutablePath;

                // Open the registry key for the current user's startup programs
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (startWithWindows)
                    {
                        // Set the application to start with Windows startup by adding a registry value
                        key.SetValue(appName, exePath);
                    }
                    else
                    {
                        // Remove the registry value to prevent the application from starting with
                        // Windows startup
                        key.DeleteValue(appName, false);
                    }
                }
            });
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the theme
            isDarkTheme = !isDarkTheme;

            // Update the theme setting
            settings.Theme = isDarkTheme ? "Dark" : "Light";
            _ = SaveSettingsAsync(settings);

            // Define the URIs for the dark and light themes
            var darkThemeUri = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml");
            var lightThemeUri = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");

            // Determine the current theme URI based on the toggle
            var themeUri = isDarkTheme ? darkThemeUri : lightThemeUri;

            // Print the current merged dictionaries before the toggle
            System.Diagnostics.Debug.WriteLine("Before toggle:");
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                System.Diagnostics.Debug.WriteLine($" - {dictionary.Source}");
            }

            // Check if the new theme already exists in the merged dictionaries
            var existingTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == themeUri);
            if (existingTheme == null)
            {
                // If the new theme does not exist, add it to the merged dictionaries
                existingTheme = new ResourceDictionary() { Source = themeUri };
                Application.Current.Resources.MergedDictionaries.Add(existingTheme);
            }

            // Remove the current theme from the merged dictionaries
            var currentTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == (isDarkTheme ? lightThemeUri : darkThemeUri));
            if (currentTheme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(currentTheme);
            }

            // Print the current merged dictionaries after the toggle
            System.Diagnostics.Debug.WriteLine("After toggle:");
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                System.Diagnostics.Debug.WriteLine($" - {dictionary.Source}");
            }
        }

        // Method to update the UI and publish game information
        private void UpdateUIAndPublishGame(GameInfo game)
        {
            // only doi this if mqtt is connected
            if (mqttClientWrapper == null || !mqttClientWrapper.IsConnected)
            {
                return;
            }
            Dispatcher.Invoke(async () =>
            {
                detectedGameText.Text = $"Detected Game: {game.GameName}";

                try
                {
                    var uri = new Uri(game.LogoUrl, UriKind.Absolute);
                    if (uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(game.LogoUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        gamePic.Source = bitmap;
                        gamePic.Visibility = Visibility.Visible;
                        // Use XamlAnimatedGif for animated GIFs
                        AnimationBehavior.SetSourceUri(gamePic, uri);
                    }
                    else
                    {
                        // Use standard method for non-GIF images
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(game.LogoUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        gamePic.Source = bitmap;
                        gamePic.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image: {ex.Message}");
                    // Handle error, such as setting a default image
                }
            });
            // Create MQTT topics for state and attributes
            string stateTopic = $"HAGameSpy/{deviceId}/state";
            string attributesTopic = $"HAGameSpy/{deviceId}/attributes";

            // Create attributes object with game information and device ID
            var attributes = new { gamename = game.GameName, gamelogourl = game.LogoUrl, device_id = deviceId };
            string attributesPayload = JsonConvert.SerializeObject(attributes);

            // Publish game name as state
            _ = mqttClientWrapper.PublishAsync(stateTopic, game.GameName);

            // Publish attributes including device ID
            _ = mqttClientWrapper.PublishAsync(attributesTopic, attributesPayload);
        }

        #endregion Private Methods
    }
}