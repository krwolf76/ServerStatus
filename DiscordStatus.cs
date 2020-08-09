using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordObjects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Random = Oxide.Core.Random;
using System.Globalization;
using Oxide.Core.Configuration;
using static Oxide.Ext.Discord.DiscordObjects.Embed;
using Oxide.Game.Rust.Libraries;
using System.Data.SqlTypes;

namespace Oxide.Plugins
{
    [Info("Discord Status", "Tricky/KR_WOLF", "2.0.45")]
    [Description("Shows server information as a discord bot status")]

    public class DiscordStatus : CovalencePlugin
    {
        #region Fields
        [DiscordClient]
        private DiscordClient Client;

        [PluginReference]
        private Plugin DiscordAuth;

        Configuration config;
        private int statusIndex = -1;
        private string[] StatusTypes = new string[]
        {
            "Game",
            "Stream",
            "Listen",
            "Watch"
        };
        #endregion

        #region Config
        class Configuration
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string BotToken = string.Empty;

            [JsonProperty(PropertyName = "Server Status Channel Settings")]
            public string ChannelID = string.Empty;

            [JsonProperty(PropertyName = "Time Format")]
            public string TimeFormat = "yyyy/MM/dd HH:mm:ss";

            [JsonProperty(PropertyName = "Lang Set")]
            public string LangSetting = "en";

            [JsonProperty(PropertyName = "Server Status Check (DON'T CHANGE)")]
            public bool ServerStatus = false;

            [JsonProperty(PropertyName = "Update Interval (Seconds)")]
            public int UpdateInterval = 5;

            [JsonProperty(PropertyName = "Randomize Status")]
            public bool Randomize = false;

            [JsonProperty(PropertyName = "Status Type (Game/Stream/Listen/Watch)")]
            public string StatusType = "Game";

            [JsonProperty(PropertyName = "Status", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Status = new List<string>
            {
                "{players.online} / {server.maxplayers} Online!",
                "{server.entities} Entities",
                "{players.sleepers} Sleepers!",
                "{players.authenticated} Linked Account(s)"
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            lang.SetServerLanguage(config.LangSetting);

            if (config.BotToken == string.Empty)
                return;

            timer.Every(901, () => Reload());

            Discord.CreateClient(this, config.BotToken);

            if (Client != null)
            {
                if (config.ChannelID == string.Empty)
                {
                    PrintWarning($"Channel ID is not set.");
                }
                else
                {
                    if (config.ServerStatus == false)
                    {
                        Channel.GetChannel(Client, config.ChannelID, chan =>
                        {
                            chan.CreateMessage(Client, "@everyone");
                            chan.CreateMessage(Client, ServerStats($"{string.Format(Lang("Online"))}", $"{string.Format(Lang("Online Descriptions"))}"));
                        });
                        config.ServerStatus = true;
                        SaveConfig();
                    }

                }
            }


            timer.Every(config.UpdateInterval, () => UpdateStatus());
        }
        

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            //if (arg.cmd.FullName == "global.quit")
            //{
            //    Channel.GetChannel(Client, config.ChannelID, chan =>
            //    {
            //        chan.CreateMessage(Client, ServerStats(Lang("Quit", null), Lang("Quit Descriptions", null)));
            //    });
            //    timer.Once(10, () =>
            //    {
            //        Puts("1");
            //    });
            //    return false;
            //}
            //return null;
            if (config.ServerStatus == true)
            {
                if (arg.cmd != null)
                {
                    if (arg.Args == null) return null;
                    
                    if (arg.cmd.FullName == "global.quit")
                    {
                        Channel.GetChannel(Client, config.ChannelID, chan =>
                        {
                            chan.CreateMessage(Client, ServerStats(Lang("Quit", null), Lang("Quit Descriptions", null, arg.HasArgs(1) ? arg.Args[0] : Lang("Unknown", null))));
                        });

                        timer.Once(10, () =>
                        {

                        });
                        return true;
                    }
                    if (arg.cmd.Name == "restart")
                    {

                        if (arg.Args[0] == "-1")
                        {
                            Channel.GetChannel(Client, config.ChannelID, chan =>
                            {
                                chan.CreateMessage(Client, "@everyone");
                                chan.CreateMessage(Client, ServerStats(Lang("Restart Cancel", null), Lang("Restart Cancel Descriptions", null)));
                            });
                            Puts("Cancel Restart!");
                            return true;
                        }

                        Channel.GetChannel(Client, config.ChannelID, chan =>
                        {
                            chan.CreateMessage(Client, "@everyone");
                            chan.CreateMessage(Client, ServerStats(Lang("Restart", null), Lang("Restart Descriptions", null, arg.HasArgs(1) ? arg.Args[0] : "300", arg.HasArgs(2) ? arg.Args[1] : Lang("Unknown", null))));
                        });
                        return null;

                    }
                }
            }
            return null;
        }

        [ConsoleCommand("qquit")]
        private void QuitCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection.connected == false || arg.Connection.authLevel == 0) return;
            Channel.GetChannel(Client, config.ChannelID, chan =>
            {
                chan.CreateMessage(Client, ServerStats(Lang("Quit", null), Lang("Quit Descriptions", null)));
            });
            timer.Once(1, () =>
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, ($"quit {Lang("Quit Descriptions", null)}"));
            });

        }

        private void Unload() => Discord.CloseClient(Client);

        private void OnServerShutdown()
        {
            if (config.ServerStatus == true)
            {

                Channel.GetChannel(Client, config.ChannelID, chan =>
                {
                    chan.CreateMessage(Client, ServerStats(Lang("Quit", null), Lang("Quit Descriptions", null)));
                });
                config.ServerStatus = false;
                SaveConfig();
            }

        }
        #endregion

        #region Status Update
        private void UpdateStatus()
        {
            if (config.Status.Count == 0)
                return;

            var index = GetStatusIndex();

            Client.UpdateStatus(new Presence()
            {
                Game = new Ext.Discord.DiscordObjects.Game()
                {
                    Name = Format(config.Status[index]),
                    Type = GetStatusType()
                }
            });

            statusIndex = index;
        }
        #endregion

        #region Helper Methods
        private int GetStatusIndex()
        {
            if (!config.Randomize)
                return (statusIndex + 1) % config.Status.Count;

            var index = 0;
            do index = Random.Range(0, config.Status.Count - 1);
            while (index == statusIndex);

            return index;
        }
        private void Reload()
        {
            ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, ($"oxide.reload DiscordStatus"));
        }
        private ActivityType GetStatusType()
        {
            if (!StatusTypes.Contains(config.StatusType))
                PrintError($"Unknown Status Type '{config.StatusType}'");

            switch (config.StatusType)
            {
                case "Game":
                    return ActivityType.Game;
                case "Stream":
                    return ActivityType.Streaming;
                case "Listen":
                    return ActivityType.Listening;
                case "Watch":
                    return ActivityType.Watching;
                default:
                    return default(ActivityType);
            }
        }

        private string Format(string message)
        {
            message = message
                .Replace("{guild.name}", Client?.DiscordServer?.name ?? "{unknown}")
                .Replace("{members.total}", Client?.DiscordServer?.member_count.ToString() ?? "{unknown}")
                .Replace("{channels.total}", Client?.DiscordServer?.channels?.Count.ToString() ?? "{unknown}")
                .Replace("{server.hostname}", server.Name)
                .Replace("{server.maxplayers}", server.MaxPlayers.ToString())
                .Replace("{players.online}", players.Connected.Count().ToString())
                .Replace("{players.authenticated}", DiscordAuth != null ? GetAuthCount().ToString() : "{unknown}");

#if RUST
            message = message
                .Replace("{server.ip}", ConVar.Server.ip)
                .Replace("{server.port}", ConVar.Server.port.ToString())
                .Replace("{server.entities}", BaseNetworkable.serverEntities.Count.ToString())
                .Replace("{server.worldsize}", ConVar.Server.worldsize.ToString())
                .Replace("{server.seed}", ConVar.Server.seed.ToString())
                .Replace("{players.queued}", ConVar.Admin.ServerInfo().Queued.ToString())
                .Replace("{players.joining}", ConVar.Admin.ServerInfo().Joining.ToString())
                .Replace("{players.sleepers}", BasePlayer.sleepingPlayerList.Count.ToString())
                .Replace("{players.total}", players.Connected.Count() + BasePlayer.sleepingPlayerList.Count.ToString());
#endif

            return message;
        }

        private int GetAuthCount() => (int) DiscordAuth.Call("API_GetAuthCount");

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
        public Embed ServerStats(string content, string reason)
        {
            List<Field> fieldDataList = new List<Field>();
            fieldDataList.Add(this.setFieldValue(Lang("Time", null), $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}", true));
            fieldDataList.Add(this.setFieldValue(string.Format(Lang("Descriptions")), reason, false));
            
            Embed embed = new Embed
            {
                title = Lang("Title", null),
                description = content,
                fields = fieldDataList,
                color = 15158332
            };
            return embed; 
        }
        private Field setFieldValue(String name, String value, bool inline)
        {
            Field fieldData = new Field();
            fieldData.name = name;
            fieldData.value = value;
            fieldData.inline = inline;
            return fieldData;
        }

        #endregion
    }
}
