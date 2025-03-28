using Cysharp.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;
using Quests.API;
using System.Threading.Tasks;

namespace Quests.Events
{
    internal class PlayerDisconnectedEvent : IEventListener<UnturnedPlayerDisconnectedEvent>
    {
        private readonly IMongoDbDatabase m_db;
        public PlayerDisconnectedEvent(IMongoDbDatabase mongoDbDatabase)
        {
            m_db = mongoDbDatabase;
        }

        public Task HandleEventAsync(object? sender, UnturnedPlayerDisconnectedEvent @event)
        {
            UniTask.Run(() =>
            {
                var model = m_db.GetPlayerModel(@event.Player.SteamId.ToString());
                if (model != null)
                {
                    m_db.SavePlayerInDatabase(@event.Player.SteamId.ToString());
                    m_db.GetCachedPlayerModels().Remove(model);
                }
            });
            return Task.CompletedTask;
        }
    }
}
