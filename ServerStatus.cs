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
    [Info("ServerStatus", "KR_WOLF", "1.1.0")]
    [Description("Server Status Check for discord Webhook")]
    class ServerStatus : RustPlugin
    {
        private Configuration _config;
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return true;
            if (_config.ServerStatus == true)
            {
                if (null != arg && null != arg.cmd && null != arg.cmd.Name)
                {
                    if ("restart".Equals(arg.cmd.Name))
                    {
                        if (arg.Args[0] == "-1")
                        {
                            SendMessage(Lang("Restart Cancel", null), Lang("Restart Cancel Descriptions", null));
                            Puts("Cancel Restart!");
                            return true;
                        }
                        SendMessage(Lang("Restart", null), Lang("Restart Descriptions", null, arg.HasArgs(1) ? arg.Args[0] : "300", arg.HasArgs(2) ? arg.Args[1] : Lang("Unknown", null)));
                    }
                }
                
            }
            return null;
        }

        void OnServerShutdown()
        {
            if (_config.ServerStatus == true)
            {
                SendMessage(Lang("Offline", null), Lang("Offline Descriptions", null));
                _config.ServerStatus = false;
                SaveConfig();
            }
        }
        private void OnServerInitialized()
        {
            if (_config.ServerStatus == false)
            {
                SendMessage(Lang("Online", null), Lang("Online Descriptions", null));
                _config.ServerStatus = true;
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

            [JsonProperty("Embed Fields Time Format")]
            public string TimeFormat { get; set; } = "MM/dd/yy HH:mm:ss";

            [JsonProperty("Server Status (don't change)")]
            public bool ServerStatus { get; set; } = false;
        }
        #endregion
        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Title"] = "Server Status 💫",
                ["Online"] = "📡 Server is online | ✅",
                ["Quit"] = "📡 Server is offline | ❌",
                ["Restart"] = "📡 The server has started restarting | ⏳",
                ["Restart Cancel"] = "📡 The server has canceled the restart | ⏳",
                ["Time"] = "Time:",
                ["Descriptions"] = "Descriptions:",
                ["Online Descriptions"] = "🎈 Server is Online",
                ["Quit Descriptions"] = "🎈 Server is Offline",
                ["Restart Descriptions"] = "🎈 The server shuts down after {0} seconds.\n\n🎈 Reason: {1}",
                ["Restart Cancel Descriptions"] = "🎈 Server is Cancel Restart",
                ["Unknown"] = "Unknown"

            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [""] = "서버 상태 💫",
            }, this, "kr");
        }

        private string Lang(string key, string id = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, id), args);
        }
        #endregion
        #region Discord
        private void SendMessage(string status, string reason)
        {
            var embed = new Embed()
                .AddField(Lang("Title"), status, true)
                .AddField(Lang("Time"), $"{DateTime.Now.ToString(_config.TimeFormat)}", false)
                .AddField(Lang("Descriptions"), reason, false);

            webrequest.Enqueue(_config.webhook, new DiscordMessage("", embed).ToJson(), (code, response) => {
            }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
            });
        }

        private class DiscordMessage
        {
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = "@everyone" + content;
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
