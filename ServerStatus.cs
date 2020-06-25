using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("ServerStatus", "KR_WOLF", "1.0.0")]
    [Description("KR_WOLF#5912")]
    class ServerStatus : RustPlugin
    {
        [PluginReference] private Plugin DiscordMessages;
        private Configuration _config;

        void OnServerShutdown()
        {
            if (_config.ServerStatus == "online")
            {
                object fields = new[]
                {
                  new {
                    name = _config.Status, value = _config.Offline, inline = true
                  },
                  new {
                    name = _config.Time, value = $"{DateTime.Now}", inline = false
                  }
                };
                string json = JsonConvert.SerializeObject(fields);
                DiscordMessages?.Call("API_SendFancyMessage", $"{_config.webhook}", $"{_config.Title}", 15158332, json);
                _config.ServerStatus = "offline";
                SaveConfig();
            }

        }
        private void OnServerInitialized()
        {
            if (_config.ServerStatus == "offline")
            {
                
                object fields = new[]
                {
                  new {
                    name = _config.Status, value = _config.Online, inline = true
                  },
                  new {
                    name = _config.Time, value = $"{DateTime.Now}", inline = false
                  }
                };
                string json = JsonConvert.SerializeObject(fields);
                DiscordMessages?.Call("API_SendFancyMessage", $"{_config.webhook}", $"{_config.Title}", 15158332, json);
                _config.ServerStatus = "online";
                SaveConfig();
            }
        }

        #region Config
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("Discord WebHook")]
            public string webhook { get; set; } = "";

            [JsonProperty("Embed Title")]
            public string Title { get; set; } = "Server Status 💫";

            [JsonProperty("Embed Fields Online")]
            public string Online { get; set; } = "📡 Server is Online | ✅";

            [JsonProperty("Embed Fields Offline")]
            public string Offline { get; set; } = "📡 Server is Offline | ❌";

            [JsonProperty("Embed Fields Status")]
            public string Status { get; set; } = "Status";

            [JsonProperty("Embed Fields Time")]
            public string Time { get; set; } = "Time";

            [JsonProperty("Server Status (don't change)")]
            public string ServerStatus { get; set; } = "offline";
        }
        #endregion
    }
}
