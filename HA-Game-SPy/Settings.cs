using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HA_Game_SPy
{
    public class Settings
    {
        public string HomeAssistantUrl { get; set; }
        public string EncryptedHAToken { get; set; }
        public string MqttAddress { get; set; }
        public string MqttUsername { get; set; }
        public string EncryptedMqttPassword { get; set; }
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; }
        public string Theme { get; set; }

        public bool UpdateSettings { get; set; }
    }

}
