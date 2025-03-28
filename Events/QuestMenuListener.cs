using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenMod.API.Commands;
using OpenMod.API.Eventing;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Core.Helpers;
using OpenMod.Unturned.Players;
using OpenMod.Unturned.Players.UI.Events;
using OpenMod.Unturned.Users;
using Quests.API;
using Quests.Models;
using Quests.Services;
using Quests.Utils;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quests.Events
{
    internal class QuestMenuListener : IEventListener<UnturnedPlayerButtonClickedEvent>
    {
        private readonly IQuestProvider m_questProvider;
        private readonly IMongoDbDatabase m_db;
        private readonly ICooldownService m_cooldownService;
        private readonly ICommandExecutor m_commandExecutor;
        private readonly IConsoleActorAccessor m_consoleActorAccessor;
        private readonly IUnturnedUserDirectory m_UnturnedUserDirectory;
        private readonly IPlayerMessager m_PlayerMessager;
        private readonly IConfiguration m_configuration;

        public QuestMenuListener(IQuestProvider questProvider,
            IMongoDbDatabase mongoDbDatabase,
            ICooldownService cooldownService,
            ICommandExecutor commandExecutor,
            IConsoleActorAccessor consoleActorAccessor,
            IUnturnedUserDirectory unturnedUserDirectory, 
            IPlayerMessager playerMessager,
            IConfiguration configuration)
        {
            m_questProvider = questProvider;
            m_db = mongoDbDatabase;
            m_cooldownService = cooldownService;
            m_commandExecutor = commandExecutor;
            m_consoleActorAccessor = consoleActorAccessor;
            m_UnturnedUserDirectory = unturnedUserDirectory;
            m_PlayerMessager = playerMessager;
            m_configuration = configuration;
        }

        private async Task CloseMenuAsync(UnturnedPlayer player)
        {
            await UniTask.Delay(400);
            await UniTask.SwitchToMainThread();
            EffectManager.askEffectClearByID(6131, player.Player.channel.owner.transportConnection);
        }

        public async Task HandleEventAsync(object? sender, UnturnedPlayerButtonClickedEvent @event)
        {
            // button press event delay to prevent flooding with requests
            UnturnedPlayer player = @event.Player;
            if (m_cooldownService.hasCooldown(@event.Player.SteamId.ToString(), "questuiclick")) return;
            m_cooldownService.SetCooldown(@event.Player.SteamId.ToString(), 150, "questuiclick");
            if (@event.ButtonName.Equals("QuestExitButton"))
            {
                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"AnimateFrameWindow", false);
                player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
                AsyncHelper.Schedule("CloseMenuDelay", () => CloseMenuAsync(player));
                return;
            }

            if (@event.ButtonName.Contains("Quest-"))
            {
                string[] questObjects = @event.ButtonName.Split('-');
                if (questObjects.Length < 2) return;
                int questId = int.TryParse(questObjects[1], out int i) ? i : -1;
                if (questId == -1) return;
                await HandleDefaultClick(@event.ButtonName, questId, player);
            }

            else if (@event.ButtonName.Contains("Reward-"))
            {
                string[] rewardObjects = @event.ButtonName.Split('-');
                if (rewardObjects.Length < 2) return;
                int rewardId = int.TryParse(rewardObjects[1], out int i) ? i : -1;
                if (rewardId == -1) return;
                await HandleRewardClick(@event.ButtonName, rewardId, player);
            }
            if (@event.ButtonName.Equals($"RewardsButton"))
            {
                string steamId = player.SteamId.ToString();
                int level = m_db.GetPlayerLevel(steamId); 
                List<int> claimed_rewards = m_db.GetPlayerClaimedRewards(steamId);

                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"MainPage", false);
                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"RewardsPage", true);
                foreach (var reward in m_questProvider.rewards)
                {
                    EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}", true);
                    if (claimed_rewards.Contains(reward.forLevel))
                    {
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Claimed", true);
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Locked", false);
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Available", false);
                        continue;
                    }
                    EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Name", $"Level {reward.forLevel}");
                    if (reward.forLevel > level)
                    {
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Claimed", false);
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Locked", true);
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Available", false);
                    }
                    else
                    {
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Claimed", false);
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Locked", false);
                        EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Available", true);
                    }
                }
            }
            if (@event.ButtonName.Equals($"BackFromRewardsButton"))
            {

                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"MainPage", true);
                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"RewardsPage", false);
            }
            return;
        }

        private async Task HandleRewardClick(string button, int rewardId, UnturnedPlayer player)
        {
            string steamId = player.SteamId.ToString();
            int playerLevel = m_db.GetPlayerLevel(steamId);
            List<int> claimed_rewards = m_db.GetPlayerClaimedRewards(steamId);

            if (button.Equals($"Reward-{rewardId}-Available"))
            {
                if (playerLevel < rewardId) return;
                if (claimed_rewards.Contains(rewardId)) return;
                RewardModel reward = m_questProvider.GetRewardByLevelFor(rewardId);
                List<string> rewards = reward.commands ?? new ();
                foreach (string command in rewards)
                {
                    _ = m_commandExecutor.ExecuteAsync(m_consoleActorAccessor.Actor, command.Replace("{targetPlayer}", player.SteamId.ToString()).Split(' '), "");
                }
                m_db.AddPlayerClaimedReward(steamId, rewardId);

                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Claimed", true);
                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Reward-{reward.forLevel}-Available", false);
            }
        }

        private async Task HandleDefaultClick(string button, int questId, UnturnedPlayer player)
        {
            string steamId = player.SteamId.ToString();
            Dictionary<string, bool> completedQuests = m_db.GetCompletedQuestIds(steamId);
            //Console.WriteLine($"ButtonName: {button} | quest id: {questId}");
            #region NotCompleted or in progress
            if (button.Equals("Quest-"+questId))
            {
                if (completedQuests.ContainsKey(questId.ToString())) return;
                int tracked_quest_id = m_db.GetTrackedQuestId(steamId);
                if (tracked_quest_id == -1)
                {
                    // start tracking selected quest

                    m_db.SetTrackedQuestId(steamId, questId);
                    Dictionary<string, int> chachedQuestProgress = m_db.GetChachedQuestsProgress(steamId);
                    if (chachedQuestProgress != null && chachedQuestProgress.Count > 0 && chachedQuestProgress.ContainsKey(questId.ToString()))
                    {
                        m_db.SetTrackedQuestProgress(steamId, chachedQuestProgress[questId.ToString()]);
                    }
                    int tracked_quest_progress = m_db.GetTrackedQuestProgress(steamId);
                    QuestModel quest = m_questProvider.GetQuestById(questId);
                    double scaledProgress = MathUtils.MapToRange(tracked_quest_progress, 0, quest.condition_amount);

                    EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-ProgressBar", true);
                    EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-Counter", tracked_quest_progress + "/" + quest.condition_amount);
                    EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-Fill", "".PadLeft((int)scaledProgress, 'a'));
                    return;
                }
                if (tracked_quest_id == questId)
                {
                    // cancel tracking of quest and cache it

                    int tracked_quest_progress = m_db.GetTrackedQuestProgress(steamId);
                    m_db.AddProgressToChachedQuests(steamId, questId, tracked_quest_progress);
                    m_db.SetTrackedQuestId(steamId, -1);
                    m_db.SetTrackedQuestProgress(steamId, 0);

                    EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-ProgressBar", false);
                    return;
                } else
                {
                    // replace current quest with the selected quest
                    int tracked_quest_progress = m_db.GetTrackedQuestProgress(steamId);
                    m_db.AddProgressToChachedQuests(steamId, tracked_quest_id, tracked_quest_progress);
                    m_db.SetTrackedQuestId(steamId, questId);
                    Dictionary<string, int> chachedQuestProgress = m_db.GetChachedQuestsProgress(steamId);
                    if (chachedQuestProgress != null && chachedQuestProgress.Count > 0 && chachedQuestProgress.ContainsKey(questId.ToString()))
                    {
                        m_db.SetTrackedQuestProgress(steamId, chachedQuestProgress[questId.ToString()]);
                    }
                    int tracked_quest_progress_new = m_db.GetTrackedQuestProgress(steamId);
                    QuestModel quest = m_questProvider.GetQuestById(questId);
                    double scaledProgress = MathUtils.MapToRange(tracked_quest_progress_new, 0, quest.condition_amount);

                    EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{tracked_quest_id}-ProgressBar", false);
                    EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-ProgressBar", true);
                    EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-Counter", tracked_quest_progress_new + "/" + quest.condition_amount);
                    EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-Fill", "".PadLeft((int)scaledProgress, 'a'));
                }
            } 
            #endregion

            #region Completed but not claimed

            if (button.Equals($"Quest-{questId}-CompletedNotClaimed"))
            {
                if (!completedQuests.ContainsKey(questId.ToString()))
                {
                    return;
                }
                m_db.AddCompletedQuest(steamId, questId, true);
                QuestModel quest = m_questProvider.GetQuestById(questId);
                long end_time_reset = 0;
                if (quest.isDaily)
                {
                    end_time_reset = DateTimeOffset.Now.AddHours(24).ToUnixTimeMilliseconds();
                    m_db.AddQuestToResetList(steamId, questId, end_time_reset);
                }
                m_db.AddPlayerXp(steamId, quest.reward_profile_xp);
                int xp = m_db.GetPlayerXp(steamId);
                int level = m_db.GetPlayerLevel(steamId);

                if (xp >= 400)
                {
                    m_db.AddPlayerLevel(steamId, 1);
                    m_db.SetPlayerXp(steamId, xp -= 400);
                    level++;
                    m_PlayerMessager.SendMessageLocalAsync(player, m_configuration.GetValue("Messages:PlayerLevelUp:text", "Error, message not found")
                    .Replace("{level}", level.ToString()),
                    m_configuration.GetValue<string>("Messages:IconUrl"), ColorTranslator.FromHtml("#"+m_configuration.GetValue("Messages:PlayerLevelUp:color", "#fff")));
                }
                int exp = xp >= 400 ? 0 : xp;

                double ExpScaledProgress = MathUtils.MapToRange(exp, 0, 400);

                EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"LevelCounter", level.ToString());
                EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"ExpCounter", exp + "/" + 400);
                EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"ExpFill", "".PadLeft((int)ExpScaledProgress, 'a'));

                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-CompletedNotClaimed", false);
                EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-Completed", true);
                if (quest.isDaily)
                {
                    EffectManager.sendUIEffectVisibility(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-ResetTimer", true);
                    EffectManager.sendUIEffectText(6131, player.Player.channel.owner.transportConnection, true, $"Quest-{questId}-ResetTimerCounter", MathUtils.getFormatedTime(end_time_reset-3000));
                }
                player.Player.skills.askAward(quest.reward_exp);
                if (quest.reward_ids != null && quest.reward_ids.Count > 0)
                {
                    foreach (string item in quest.reward_ids)
                    {
                        string[] itemArgs = item.Split(':');
                        byte amount = itemArgs.Length > 1 && itemArgs[1] != null ? byte.Parse(itemArgs[1]) : (byte)1;
                        ItemTool.tryForceGiveItem(player.Player, ushort.Parse(itemArgs[0]), amount);
                    }
                }
                if (quest.reward_commands != null && quest.reward_commands.Count > 0)
                {
                    foreach (string command in quest.reward_commands)
                    {
                        _ = m_commandExecutor.ExecuteAsync(m_consoleActorAccessor.Actor, command.Replace("{targetPlayer}", player.SteamId.ToString()).Split(' '), "");
                    }
                }
            }

            #endregion
        }
    }
}
