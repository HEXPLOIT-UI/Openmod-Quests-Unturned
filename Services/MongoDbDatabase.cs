using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenMod.API;
using OpenMod.API.Ioc;
using Quests.API;
using Quests.Models;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quests.Services
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class MongoDbDatabase : IMongoDbDatabase
    {
        private readonly IMongoDatabase _database;
        private readonly IConfiguration m_configuration;
        private readonly ILogger<Quests> m_logger;
        private readonly IMongoCollection<MongoDBPlayerModel> _collection;
        private readonly List<MongoDBPlayerModel> _tempPlayers;
        private readonly IRuntime m_runtime;

        public MongoDbDatabase(IConfiguration configuration, ILogger<Quests> logger, IRuntime runtime)
        {  
            m_configuration = configuration;
            m_logger = logger;
            m_runtime = runtime;
            var client = new MongoClient(m_configuration.GetValue<string>("MongoDB:connection_string"));
            _database = client.GetDatabase(m_configuration.GetValue<string>("MongoDB:database_name"));
            _collection = _database.GetCollection<MongoDBPlayerModel>(m_configuration.GetValue<string>("MongoDB:collection_name"));
            _tempPlayers = new List<MongoDBPlayerModel>();
        }

        public MongoDBPlayerModel InsertEmptyPlayerModel(string steamId, string name)
        {
            var user = new MongoDBPlayerModel
            {
                Id = ObjectId.GenerateNewId(),
                steam_id = steamId,
                cached_name = name,
                completed_quests_ids = new Dictionary<string, bool>(),
                level = 0,
                xp = 0,
                tracked_quest_id = -1,
                tracked_quest_progress = 0,
                quests_chache = new Dictionary<string, int>(),
                kills = 0,
                deaths = 0,
                claimed_rewards = new List<int>(),
                reloadable_quests = new Dictionary<string, long>(),
            };
            _tempPlayers.AddOrReplace(user);
            m_logger.LogInformation($"Created new player model for: {name} ({steamId})");
            return user;
        }
		
        public async Task SavePlayerInDatabase(string steamId)
        {
            try
            {
                var filter = Builders<MongoDBPlayerModel>.Filter.Eq(x => x.steam_id, steamId);
                var result = await _collection.ReplaceOneAsync(filter, GetPlayerModel(steamId), new ReplaceOptions { IsUpsert = true });
                m_logger.LogInformation($"Data saved for {steamId}");
            } catch (TimeoutException e)
            {
                ShutdownDueError(e);
            }
        }

        public void SetPlayerCachedName(string steamId, string name) => GetPlayerModel(steamId).cached_name = name;

        public async Task LoadPlayerFromDatabase(string steamId, string name)
        {
            try
            {
                var filter = Builders<MongoDBPlayerModel>.Filter.Eq(u => u.steam_id, steamId);
                var result = await _collection.Find(filter).FirstOrDefaultAsync();
                if (result == null)
                {
                    InsertEmptyPlayerModel(steamId, name);
                    return;
                }
                _tempPlayers.AddOrReplace(result);
                return;
            } catch (TimeoutException e)
            {
                ShutdownDueError(e);
            }
        }

        public List<MongoDBPlayerModel> GetCachedPlayerModels() => _tempPlayers;

        public MongoDBPlayerModel GetPlayerModel(string steamId) => _tempPlayers.Find(x => x.steam_id == steamId);


        public Dictionary<string, bool> GetCompletedQuestIds(string steamId) => GetPlayerModel(steamId)?.completed_quests_ids ?? [];

        public int GetPlayerLevel(string steamId) => GetPlayerModel(steamId)?.level ?? 0;

        public int GetPlayerXp(string steamId) => GetPlayerModel(steamId)?.xp ?? 0;

        public void AddPlayerLevel(string steamId, int amount) => GetPlayerModel(steamId).level += amount;

        public void AddPlayerXp(string steamId, int amount) => GetPlayerModel(steamId).xp += amount;

        public void SetPlayerXp(string steamId, int amount) => GetPlayerModel(steamId).xp = amount;

        public void AddCompletedQuest(string steamId, int quest_id, bool claimed) => GetCompletedQuestIds(steamId).AddOrReplace(quest_id.ToString(), claimed);

        public void RemoveCompletedQuest(string steamId, string quest_id) => GetCompletedQuestIds(steamId).Remove(quest_id.ToString());


        public void AddPlayerClaimedReward(string steamId, int reward_id) => GetPlayerClaimedRewards(steamId).AddOrReplace(reward_id);

        public List<int> GetPlayerClaimedRewards(string steamId) => GetPlayerModel(steamId)?.claimed_rewards ?? new List<int>();

        public int GetTrackedQuestId(string steamId) => GetPlayerModel(steamId)?.tracked_quest_id ?? 0;

        public void SetTrackedQuestProgress(string steamId, int amount) => GetPlayerModel(steamId).tracked_quest_progress = amount;

        public int GetTrackedQuestProgress(string steamId) => GetPlayerModel(steamId)?.tracked_quest_progress ?? 0;

        public void SetTrackedQuestId(string steamId, int quest_Id) => GetPlayerModel(steamId).tracked_quest_id = quest_Id;

        public Dictionary<string, int> GetChachedQuestsProgress(string steamId) => GetPlayerModel(steamId)?.quests_chache ?? new Dictionary<string, int>();

        public void AddProgressToChachedQuests(string steamId, int quest_id, int progress) => GetChachedQuestsProgress(steamId).AddOrReplace(quest_id.ToString(), progress);

        public Dictionary<string, long> GetQuestsResetList(string steamId) => GetPlayerModel(steamId)?.reloadable_quests ?? new Dictionary<string, long>();

        public void AddQuestToResetList(string steamId, int quest_id, long end_reset_time) => GetQuestsResetList(steamId).AddOrReplace(quest_id.ToString(), end_reset_time);

        public void RemoveQuestFromResetList(string steamId, string quest_id) => GetQuestsResetList(steamId).Remove(quest_id.ToString());

        public void AddTrackedQuestProgress(string steamId, int amount) => GetPlayerModel(steamId).tracked_quest_progress += amount;

        public void RemoveQuestFromChachedQuests(string steamId, int quest_id) => GetChachedQuestsProgress(steamId).Remove(quest_id.ToString());

        public int GetPlayerKills(string steamId) => GetPlayerModel(steamId)?.kills ?? 0;

        public void AddPlayerKills(string steamId, int amount) => GetPlayerModel(steamId).kills += amount;

        public int GetPlayerDeaths(string steamId) => GetPlayerModel(steamId)?.deaths ?? 0;

        public void AddPlayerDeaths(string steamId, int amount) => GetPlayerModel(steamId).deaths += amount;

        public async Task<int> GetRank(string steamId)
        {
            var sort = Builders<MongoDBPlayerModel>.Sort.Descending(player => player.kills);
            var options = new FindOptions<MongoDBPlayerModel>
            {
                Sort = sort
            };

            var filter = Builders<MongoDBPlayerModel>.Filter.Empty;
            var cursor = await _collection.FindAsync(filter, options);
            var players = await cursor.ToListAsync();
            int playerRank = players.FindIndex(player => player.steam_id == steamId);

            if (playerRank != -1)
            {
                playerRank++; // Increase by 1 to get the real place in the top (numbering from 1)
                return playerRank;
            }
            else
            {
                return -1;
            }
        }

        private void ShutdownDueError(TimeoutException e)
        {
            m_logger.LogCritical(e, "Can't connect to the database");
            Provider.shutdown(0, "Database connection error");
        }

        public async Task<List<MongoDBPlayerModel>> GetLeaderboard(int limit)
        {
            var sort = Builders<MongoDBPlayerModel>.Sort.Descending(player => player.level);
            var options = new FindOptions<MongoDBPlayerModel>
            {
                Sort = sort,
                Limit = limit
            };
            var filter = Builders<MongoDBPlayerModel>.Filter.Empty;
            var cursor = await _collection.FindAsync(filter, options);
            List<MongoDBPlayerModel> leaderboard = new ();
            await cursor.ForEachAsync(leaderboard.Add);
            return leaderboard;
        }
    }
}
