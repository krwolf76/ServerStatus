using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System.Linq;
using ConVar;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins
{
    [Info("Server Status", "KR_WOLF", "1.1.15")]
    [Description("Server Status Check for discord Webhook")]
    class ServerStatus : RustPlugin
    {
        private Configuration _config;
        private string status = "offline";
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            
            if (status == "online")
            {
                if(arg.cmd != null)
                {

                    if (null != arg && null != arg.cmd && null != arg.cmd.Name)
                    {
                        if ("restart".Equals(arg.cmd.Name))
                        {
                            if (arg.Args[0] == null)
                            {
                                PrintWarning("Please enter seconds.");
                                return null;
                            }
                            if (arg.Args[0] == "-1")
                            {
                                SendMessage(Lang("Restart Cancel", null), Lang("Restart Cancel Descriptions", null));
                                Puts("Cancel Restart!");
                                return false;
                            }
                            SendMessage(Lang("Restart", null), Lang("Restart Descriptions", null, arg.HasArgs(1) ? arg.Args[0] : "300", arg.HasArgs(2) ? arg.Args[1] : Lang("Unknown", null)));
                            return null;
                        }
                    }
                }
                
                
            }
            return null;
        }

        void OnServerShutdown()
        {
            if (status == "online")
            {
                SendMessage(Lang("Quit", null), Lang("Quit Descriptions", null));
                status = "offline";
                SaveConfig();
            }
        }
        private void OnServerInitialized()
        {
            if (_config.webhook == "webhookurl" || _config.webhook == null || _config.webhook == string.Empty)
            {
                PrintWarning("Change WebHook URL");
                return;
            }

            if (status == "offline")
            {
                SendMessage(Lang("Online", null), Lang("Online Descriptions", null));
                status = "online";
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
            public string TimeFormat { get; set; } = "MM/dd/yyyy HH:mm:ss";

            [JsonProperty("@everyone Mention Disable")]
            public bool EveryoneMention { get; set; } = false;
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
                ["Title"] = "서버 상태 💫",
                ["Online"] = "📡 서버 시작 | ✅",
                ["Quit"] = "📡 서버 중지. | ❌",
                ["Restart"] = "📡 서버 재시작 | ⏳",
                ["Restart Cancel"] = "📡 재시작 취소| ⏳",
                ["Time"] = "시간:",
                ["Descriptions"] = "설명:",
                ["Online Descriptions"] = "🎈 서버가 시작되었습니다.",
                ["Quit Descriptions"] = "🎈 서버가 중지되었습니다.",
                ["Restart Descriptions"] = "🎈 서버가  {0} 초 후에 재시작 됩니다.\n\n🎈 이유: {1}",
                ["Restart Cancel Descriptions"] = "🎈 서버 재시작이 취소되었습니다.",
                ["Unknown"] = "알수없음"
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
                .AddField(Lang("Title", null, ConVar.Server.hostname), status, true)
                .AddField(Lang("Time"), $"{DateTime.Now.ToString(_config.TimeFormat)}", false)
                .AddField(Lang("Descriptions"), reason, false);
            if(_config.EveryoneMention == false)
            {
                webrequest.Enqueue(_config.webhook, new DiscordMessage("@everyone", embed).ToJson(), (code, response) => {
                }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
                });
            }
            else
            {
                webrequest.Enqueue(_config.webhook, new DiscordMessage("", embed).ToJson(), (code, response) => {
                }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
                });
            }
            
        }

        private class DiscordMessage
        {
            
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = "" + content;
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
