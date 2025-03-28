using Cysharp.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;
using Quests.API;
using System.Threading.Tasks;

namespace Quests.Events
{
    internal class PlayerConnectedEvent : IEventListener<UnturnedPlayerConnectedEvent>
    {
        private readonly IMongoDbDatabase m_db;
        public PlayerConnectedEvent(IMongoDbDatabase mongoDbDatabase)
        {
            m_db = mongoDbDatabase;
        }

        public Task HandleEventAsync(object? sender, UnturnedPlayerConnectedEvent @event)
        {
            UniTask.Run(() =>
            {
                m_db.LoadPlayerFromDatabase(@event.Player.SteamId.ToString(), @event.Player.Player.name);
            });
            return Task.CompletedTask;
        }
    }
}
