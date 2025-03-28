using MongoDB.Bson;
using MoreLinq;
using System.Collections.Generic;

namespace Quests.Models
{
    public class MongoDBPlayerModel
    {
        public ObjectId Id { get; set; }
        public string? steam_id;
        public string? cached_name;

        /*
         * contains quest id and a value indicating whether player has taken reward for quest or not
        */
        public Dictionary<string, bool>? completed_quests_ids;
        public List<int>? claimed_rewards;
        public int level;
        public int xp;
        public int tracked_quest_id;
        public int tracked_quest_progress;

        /* 
        * if player changes the current quest to another, 
        * while having some progress on it, it is saved here like dictionary (quest id - progress on it)
        */
        public Dictionary<string, int>? quests_chache;

        public int kills;
        public int deaths;
        public Dictionary<string, long>? reloadable_quests;
        public new string ToString() => $"steam id: {steam_id}; " +
                $"name: {cached_name}; " +
                $"completed quests: {completed_quests_ids.ToJson()};" +
                $" claimed rewards: {claimed_rewards.ToJson()};" +
                $" level: {level};" +
                $" xp: {xp};" +
                $" tracked quest: {tracked_quest_id};" +
                $" tracked quest progress: {tracked_quest_progress};" +
                $" quests cache: {quests_chache.ToJson()};" +
                $" kills: {kills};" +
                $" deaths: {deaths};" +
                $" reloadable_quests: {reloadable_quests.ToJson()}";
    }
}
