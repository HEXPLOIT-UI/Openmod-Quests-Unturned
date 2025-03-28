using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using Quests.API;
using System;

namespace Quests.Commands
{
    [Command("qdebug")]
    [CommandAlias("qd")]
    [CommandDescription("debug command")]
    [CommandActor(typeof(UnturnedUser))]
    internal class DebugCommand : UnturnedCommand
    {
        private readonly IMongoDbDatabase m_db;
        public DebugCommand(IMongoDbDatabase mongoDbDatabase, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_db = mongoDbDatabase;
        }

        protected override async UniTask OnExecuteAsync()
        {
            var user = (UnturnedUser) Context.Actor;
            await PrintAsync(m_db.GetCachedPlayerModels().Count.ToString());
            await PrintAsync(m_db.GetPlayerModel(user.SteamId.ToString()).ToString());
        }
    }
}
