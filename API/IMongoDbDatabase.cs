using OpenMod.API.Ioc;
using Quests.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Quests.API
{
    [Service]
    public interface IMongoDbDatabase
    {
        MongoDBPlayerModel InsertEmptyPlayerModel(string steamId, string name);
        Task SavePlayerInDatabase(string steamId);
        void SetPlayerCachedName(string steamId, string name);
        Task LoadPlayerFromDatabase(string steamId, string name);
        List<MongoDBPlayerModel> GetCachedPlayerModels();
        MongoDBPlayerModel GetPlayerModel(string steamId);
        Dictionary<string, bool> GetCompletedQuestIds(string steamId);
        int GetPlayerLevel(string steamId);
        int GetPlayerXp(string steamId);
        void AddPlayerLevel(string steamId, int amount);
        void AddPlayerXp(string steamId, int amount);
        void SetPlayerXp(string steamId, int amount);
        void AddCompletedQuest(string steamId, int quest_id, bool claimed);
        void RemoveCompletedQuest(string steamId, string quest_id);
        void AddPlayerClaimedReward(string steamId, int reward_id);
        List<int> GetPlayerClaimedRewards(string steamId);
        int GetTrackedQuestId(string steamId);
        void SetTrackedQuestProgress(string steamId, int amount);
        int GetTrackedQuestProgress(string steamId);
        void SetTrackedQuestId(string steamId, int quest_Id);
        Dictionary<string, int> GetChachedQuestsProgress(string steamId);
        void AddProgressToChachedQuests(string steamId, int quest_id, int progress);
        Dictionary<string, long> GetQuestsResetList(string steamId);
        void AddQuestToResetList(string steamId, int quest_id, long end_reset_time);
        void RemoveQuestFromResetList(string steamId, string quest_id);
        void AddTrackedQuestProgress(string steamId, int amount);
        void RemoveQuestFromChachedQuests(string steamId, int quest_id);
        int GetPlayerKills(string steamId);
        void AddPlayerKills(string steamId, int amount);
        int GetPlayerDeaths(string steamId);
        void AddPlayerDeaths(string steamId, int amount);
        Task<int> GetRank(string steamId);
        Task<List<MongoDBPlayerModel>> GetLeaderboard(int limit);
    }
}
