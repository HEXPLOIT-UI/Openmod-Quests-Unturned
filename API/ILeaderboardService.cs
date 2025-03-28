using OpenMod.API.Ioc;
using Quests.Models;
using System.Collections.Generic;

namespace Quests.API
{
    [Service]
    public interface ILeaderboardService
    {
        List<MongoDBPlayerModel> Leaderboard { get; set; }
    }
}
