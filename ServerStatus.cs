using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ServerStatus", "KR_WOLF", "1.0.5")]
    [Description("Server Status Check for discord Webhook")]
    class ServerStatus : RustPlugin
    {
        private Configuration _config;
        [ConsoleCommand("qquit")]
        private void QuitCommand(ConsoleSystem.Arg arg)
        {
            SendMessage(_config.Offline, _config.QuitReason);
            timer.Once(1, () =>
            {
                rust.RunServerCommand($"quit {_config.QuitReason}");
            });
            
        }
        private void OnServerCommand(ConsoleSystem.Arg arg)
        {
            //if ( arg.Args.Length == 0 || arg.Args == null) return;

            if (_config.ServerStatus == "online")
            {

                if (arg.cmd.Name == "restart")
                {
                    if(arg.Args[0] == "-1")
                    {
                        SendMessage(_config.Restart + $" Cancel Restart !", _config.CancelRestartReason);
                        Puts("Cancel Restart!");
                        return;
                    }
                    SendMessage(_config.Restart + $" Restarts after {arg.Args[0]} seconds!", _config.RestartReason);
                }
            }
        }

        void OnServerShutdown()
        {
            if (_config.ServerStatus == "online")
            {
                _config.ServerStatus = "offline";
                SaveConfig();
            }

        }
        private void OnServerInitialized()
        {
            if (_config.ServerStatus == "offline")
            {

                SendMessage(_config.Online, _config.OnlineReason);
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
            public string webhook { get; set; } = "webhookurl";

            [JsonProperty("Embed Title")]
            public string Title { get; set; } = "Server Status 💫";

            [JsonProperty("Embed Fields Online")]
            public string Online { get; set; } = "📡 Server is Online | ✅";

            [JsonProperty("Embed Fields Offline")]
            public string Offline { get; set; } = "📡 Server is Offline | ❌";

            [JsonProperty("Embed Fields Restart")]
            public string Restart { get; set; } = "📡 Server is Restart | ❌";

            [JsonProperty("Embed Fields Online Reason")]
            public string OnlineReason { get; set; } = "Server OPEN!!!";

            [JsonProperty("Embed Fields Quit Reason")]
            public string QuitReason { get; set; } = "Shutdown Save Map & Data";

            [JsonProperty("Embed Fields Cancel Restart Reason")]
            public string CancelRestartReason { get; set; } = "Cancel restart";

            [JsonProperty("Embed Fields Restart Reason")]
            public string RestartReason { get; set; } = "Restarting Server";

            [JsonProperty("Embed Fields Status")]
            public string Status { get; set; } = "Status";

            [JsonProperty("Embed Fields Time")]
            public string Time { get; set; } = "Time";

            [JsonProperty("Embed Fields Time Format")]
            public string TimeFormat { get; set; } = "MM/dd/yy HH:mm:ss";

            [JsonProperty("Embed Fields Reason")]
            public string Reason { get; set; } = "Reason";

            [JsonProperty("Server Status (don't change)")]
            public string ServerStatus { get; set; } = "offline";
        }
        #endregion

        #region Discord
        private void SendMessage(string status, string reason)
        {

            var embed = new Embed()
                .AddField(_config.Status, status, true)
                .AddField(_config.Time, $"{DateTime.Now.ToString(_config.TimeFormat)}", false)
                .AddField(_config.Reason, reason, false);

            webrequest.Enqueue(_config.webhook, new DiscordMessage("", embed).ToJson(), (code, response) => {
            }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
            });
        }

        private class DiscordMessage
        {
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = content;
                Embeds = embeds.ToList();
            }

            [JsonProperty("content")] public string Content { get; set; }
            [JsonProperty("embeds")] public List<Embed> Embeds { get; set; }

            public string ToJson() => JsonConvert.SerializeObject(this);
        }

        private class Embed
        {
            [JsonProperty("fields")] public List<Field> Fields { get; set; } = new List<Field>();

            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, Regex.Replace(value, "<.*?>", string.Empty), inline));

                return this;
            }
        }

        private class Field
        {
            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }

            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("value")] public string Value { get; set; }
            [JsonProperty("inline")] public bool Inline { get; set; }
        }
        #endregion
    }
}
