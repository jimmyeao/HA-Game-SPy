using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HA_Game_SPy
{
    // This class represents the settings for the application

    public class Settings
    {
        // The URL of the Home Assistant server
        public string HomeAssistantUrl { get; set; }

        // The encrypted token for accessing the Home Assistant API
        public string EncryptedHAToken { get; set; }

        // The address of the MQTT broker
        public string MqttAddress { get; set; }

        // The username for connecting to the MQTT broker
        public string MqttUsername { get; set; }

        // The encrypted password for connecting to the MQTT broker
        public string EncryptedMqttPassword { get; set; }

        // Whether the application should start with Windows
        public bool StartWithWindows { get; set; }

        // Whether the application should start minimized
        public bool StartMinimized { get; set; }

        // The theme of the application
        public string Theme { get; set; }

        // Whether the settings should be updated
        public bool UpdateSettings { get; set; }

        // The path to the idle image
        public string IdleImage { get; set; }
    }

}
