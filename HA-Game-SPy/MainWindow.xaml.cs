using HA_Game_Spy;
using Newtonsoft.Json;
using System;
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

        public MainWindow()
        {
            InitializeComponent();
            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }
            settings = new Settings();
            Loaded += async (sender, args) =>
            {
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
                // Additional initialization using settings if needed
            };

            Closing += async (sender, args) =>
            {
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
            settings.EncryptedHAToken = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(txtHAToken.Text),
    null,
    DataProtectionScope.CurrentUser));

            settings.EncryptedMqttPassword = Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(txtMqttPassword.Password),
                null,
                DataProtectionScope.CurrentUser));
            settings.StartMinimized = chkStartMinimized.IsChecked.Value;
            settings.StartWithWindows = chkStartWithWindows.IsChecked.Value;
            settings.Theme = isDarkTheme ? "Dark" : "Light";
            settings.MqttAddress = txtMqttAddress.Text;
            settings.MqttUsername = txtMqttUsername.Text;

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


    }
}