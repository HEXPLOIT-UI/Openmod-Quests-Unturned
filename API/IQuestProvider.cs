using OpenMod.API.Ioc;
using Quests.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quests.API
{
    [Service]
    public interface IQuestProvider
    {
        List<QuestModel> quests { get; set; }
        List<RewardModel> rewards { get; set; }
        QuestModel GetQuestById(int id);
        RewardModel GetRewardByLevelFor(int forLevel);
        void ReloadQuests();
        void ReloadRewards();
    }
}
