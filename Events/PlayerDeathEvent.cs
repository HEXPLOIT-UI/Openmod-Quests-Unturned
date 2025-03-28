using Cysharp.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Users;
using Quests.API;
using Steamworks;
using System;
using System.Threading.Tasks;

namespace Quests.Events
{
    internal class PlayerDeathEvent : IEventListener<UnturnedPlayerDeathEvent>
    {
        private readonly IMongoDbDatabase m_db;
        private readonly IUnturnedUserDirectory m_UnturnedUserDirectory;
        public PlayerDeathEvent(IMongoDbDatabase mongoDbDatabase, IUnturnedUserDirectory unturnedUserDirectory)
        {
            m_db = mongoDbDatabase;
            m_UnturnedUserDirectory = unturnedUserDirectory;
        }
        public Task HandleEventAsync(object? sender, UnturnedPlayerDeathEvent @event)
        {
            UniTask.Run(async () =>
            {
                string steamId = @event.Player.SteamId.ToString();
                if (m_db.GetPlayerModel(steamId) != null)
                {
                    m_db.AddPlayerDeaths(steamId, 1);
                    await m_db.SavePlayerInDatabase(steamId);
                    if (@event.Instigator == CSteamID.Nil || @event.Instigator.Equals(@event.Player.SteamId) || m_UnturnedUserDirectory.FindUser(@event.Instigator) == null) return;
                    m_db.AddPlayerKills(@event.Instigator.ToString(), 1);
                }
            });
            return Task.CompletedTask;
        }
    }
}
