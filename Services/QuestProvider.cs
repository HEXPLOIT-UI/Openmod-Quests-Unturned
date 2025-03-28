using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using Quests.API;
using Quests.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quests.Controllers
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class QuestProvider : IQuestProvider
    {
        public List<QuestModel> quests { get; set; }
        public List<RewardModel> rewards { get; set; }
        private readonly IConfiguration m_configuration;

        public QuestProvider(IConfiguration configuration)
        {
            quests = new();
            rewards = new();
            m_configuration = configuration;
            LoadQuestsFromConfig();
            LoadRewardsFromConfig();
        }

        public QuestModel GetQuestById(int id)
        {
            if (quests.Count == 0)
                throw new InvalidOperationException("Quest list is empty");

            return quests.Find(x => x.id == id);
        }
        public RewardModel GetRewardByLevelFor(int forLevel)
        {
            if (rewards.Count == 0)
                throw new InvalidOperationException("Rewards list is empty");

            return rewards.Find(x => x.forLevel == forLevel);
        }

        public void ReloadQuests()
        {
            LoadQuestsFromConfig();
        }

        public void ReloadRewards()
        {
            LoadRewardsFromConfig();
        }

        private void LoadQuestsFromConfig()
        {
            quests.Clear();
            var questsSection = m_configuration.GetSection("Quests");
            foreach (var questSection in questsSection.GetChildren())
            {
                var quest = new QuestModel
                {
                    id = int.Parse(questSection.Key),
                    name = questSection["name"],
                    description = questSection["description"],
                    reward_ids = questSection.GetSection("reward_items").Get<List<string>>(),
                    reward_exp = questSection.GetValue<uint>("reward_experience", 100),
                    reward_profile_xp = questSection.GetValue("reward_profile_exp", 10),
                    reward_commands = questSection.GetSection("reward_commands").Get<List<string>>(),
                    condition = questSection.GetValue<EQuestCondition>("Condition"),
                    condition_amount = questSection.GetValue("Condition_amount", 1),
                    onlyMegaZombie = questSection.GetValue("onlyMega", false),
                    condition_item_id = questSection.GetValue("Condition_item_id", -1),
                    onlyHeadshots = questSection.GetValue("onlyHeadshots", false),
                    onlyZombie = questSection.GetValue("onlyZombie", false),
                    isDaily = questSection.GetValue("isDaily", false),
                };
                quests.Add(quest);
            }
        }

        private void LoadRewardsFromConfig()
        {
            rewards.Clear();
            var rewardsSection = m_configuration.GetSection("Rewards");
            foreach (var rewardSection in rewardsSection.GetChildren())
            {
                var reward = new RewardModel
                {
                    forLevel = int.Parse(rewardSection.Key),
                    commands = rewardSection.GetSection("commands").Get<List<string>>(),
                };
                rewards.Add(reward);
            }
        }
    }

}
