using System.Collections.Generic;

namespace Quests.Models
{
    public class QuestModel
    {
        public int id; // quest id (1-50)
        public string? name; // quest name
        public string? description; // quest description
        public List<string>? reward_ids; // quest reward item list. It contains item ids
        public uint reward_exp; // amount of experience that will be given to player as reward
        public int reward_profile_xp; // amount of profile experience (is needed to raise level) that will be given to player as reward
        public List<string>? reward_commands; // commands that will be executed after receiving quest reward
        public EQuestCondition condition; // quest condition
        public bool onlyMegaZombie; // Additional attributes for a quest that will only consider mega zombies. Only applies if the quest type is set to "Zombies_kill"
        public bool onlyHeadshots; // Additional attributes for a quest that will only consider headshots damage. Only applies if the quest type is set to "Damaging"
        public bool playersOnly; // Additional attributes for a quest that will only consider player damage. Only applies if the quest type is set to "Damaging"
        public int condition_amount; // this parameter specifies how many times quest condition must be repeated to fulfill it
        public int condition_item_id; // Additional attributes for a quest that will only consider a certain type of item (its id). Only applies if the quest type is set to "Pickup_items", "Item_crafting" or "Harvesting"
        public bool onlyZombie; // Additional attributes for a quest that will only consider zombie damage. Only applies if the quest type is set to "Damaging"
        public bool isDaily; // Additional attribute for a quest which will reset quest progress 24 hours after the quest has been completed
    }
}
