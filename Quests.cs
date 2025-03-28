using System;
using Microsoft.Extensions.Logging;
using Cysharp.Threading.Tasks;
using OpenMod.Unturned.Plugins;
using OpenMod.API.Plugins;
using Microsoft.Extensions.Configuration;
using Quests.API;
using SDG.Unturned;
using System.Threading;

[assembly: PluginMetadata("HEXPLOIT.Quests", DisplayName = "Quests", Author = "HEXPLOIT")]
namespace Quests
{
    public class Quests : OpenModUnturnedPlugin
    {
        private readonly ILogger<Quests> m_Logger;
        private readonly IConfiguration m_Configuration;
        private readonly IMongoDbDatabase m_Database;
        public static Quests? Instance { get; private set; }

        public Quests(
            IConfiguration configuration,
            ILogger<Quests> logger,
            IMongoDbDatabase database,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Logger = logger;
            m_Configuration = configuration;
            m_Database = database;
        }

        protected override async UniTask OnLoadAsync()
        {
            Instance = this;
            m_Logger.LogInformation($"Quests made by {Author} loaded");
            if (Provider.clients.Count == 0) return;
            foreach (var sp in Provider.clients)
            {
                await m_Database.LoadPlayerFromDatabase(sp.playerID.steamID.ToString(), sp.player.name);
            }
        }

        protected override async UniTask OnUnloadAsync()
        {
            foreach (var player in m_Database.GetCachedPlayerModels())
            {
                if (player.steam_id == null) continue;
                await m_Database.SavePlayerInDatabase(player.steam_id);
            }
            m_Logger.LogInformation($"Quests made by {Author} unloaded");
        }
    }
}
