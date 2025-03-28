using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using Quests.API;
using Quests.Models;
using System.Collections.Generic;

namespace Quests.Services
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class LeaderboardService : ILeaderboardService
    {
        public List<MongoDBPlayerModel> Leaderboard { get; set; }
        private readonly IMongoDbDatabase m_db;
        //private long lastRefresh = 0;

        public LeaderboardService(IMongoDbDatabase mongoDbDatabase)
        {
            Leaderboard = new ();
            m_db = mongoDbDatabase;
            //AsyncHelper.Schedule("RefreshLeaderboard", () => RefreshLeaderboard());
        }

        /*public async Task RefreshLeaderboard()
        {
            if (Quests.Instance == null) return;
            while (Quests.Instance.IsComponentAlive)
            {
                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() >= lastRefresh)
                {
                    Leaderboard = await m_db.GetLeaderboard(50);
                    lastRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }
            }
        }*/
    }
}
