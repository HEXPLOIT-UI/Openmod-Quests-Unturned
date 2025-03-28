using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using Quests.API;
using Quests.Utils;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Quests.Commands
{
    [Command("quest")]
    [CommandAlias("quests")]
    [CommandDescription("open quest menu command")]
    [CommandActor(typeof(UnturnedUser))]
    public class QuestCommand : UnturnedCommand
    {
        private readonly IQuestProvider m_questProvider;
        private readonly IMongoDbDatabase m_db;
        private readonly IConfiguration m_configuration;
        private readonly ILogger<Quests> m_Logger;

        public QuestCommand(IServiceProvider serviceProvider, IQuestProvider questProvider, IMongoDbDatabase mongoDbDatabase, 
            IConfiguration configuration,
            ILogger<Quests> logger) : base(serviceProvider)
        {
            m_questProvider = questProvider;
            m_db = mongoDbDatabase;
            m_configuration = configuration;
            m_Logger = logger;
        }

        protected override async UniTask OnExecuteAsync()
        {
            if (m_questProvider.quests.Count < 1)
            {
                await PrintAsync(m_configuration.GetValue("Messages:QuestsNotFound:text", "Error, message not found"), ColorTranslator.FromHtml("#"+m_configuration.GetValue("Messages:QuestsNotFound:color", "#fff")));
                return;
            }
            var player = (UnturnedUser)Context.Actor;
            string steamId = player.SteamId.ToString();
            if (m_db.GetPlayerModel(steamId) == null)
            {
                await m_db.LoadPlayerFromDatabase(steamId, player.DisplayName);
            }

            int tracked_quest_id = m_db.GetTrackedQuestId(steamId);
            int tracked_quest_progress = m_db.GetTrackedQuestProgress(steamId);
            int player_exp = m_db.GetPlayerXp(steamId);
            int level = m_db.GetPlayerLevel(steamId);
            int rank = await m_db.GetRank(steamId);
            int kills = m_db.GetPlayerKills(steamId);
            int deaths = m_db.GetPlayerDeaths(steamId);
            double KDR = deaths == 0 ? kills : (double)kills / deaths;
            string picture = await SteamProfile.GetProfilePictureUrlAsync(player.SteamId.ToString());
            double ExpScaledProgress = MathUtils.MapToRange(player_exp, 0, 400);
            Dictionary<string, long> reloadable_quests = m_db.GetQuestsResetList(steamId);
            foreach (var reloadable_quest in reloadable_quests.Keys.ToList())
            {
                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() >= reloadable_quests[reloadable_quest])
                {
                    reloadable_quests.Remove(reloadable_quest);
                    m_db.RemoveQuestFromResetList(steamId, reloadable_quest);
                    m_db.RemoveCompletedQuest(steamId, reloadable_quest);
                }
            }
            Dictionary<string, bool> completed_quests = m_db.GetCompletedQuestIds(steamId);

            await UniTask.SwitchToMainThread();

            //Console.WriteLine($"kdr {KDR} kills {kills} deaths {deaths}");
            EffectManager.sendUIEffect(6131, 6131, player.Player.Player.channel.owner.transportConnection, true);
            EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Frame", true);
            EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"AnimateFrameWindow", true);

            player.Player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"ProfileName", player.DisplayName);

            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"ExpCounter", player_exp + "/" + 400);
            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"ExpFill", "".PadLeft((int)ExpScaledProgress, 'a'));

            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"RankCounter", rank.ToString());
            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"LevelCounter", level.ToString());
            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"KillsCounter", kills.ToString());
            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"DeathCounter", deaths.ToString());
            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"KDRCounter", KDR.ToString("0.00"));
            foreach (var quest in m_questProvider.quests)
            {
                EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}", true);
                if (completed_quests.ContainsKey(quest.id.ToString()))
                {
                    if (completed_quests[quest.id.ToString()])
                    {
                        EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-Completed", true);
                        if (reloadable_quests.TryGetValue(quest.id.ToString(), out long timeToReset))
                        {
                            EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-ResetTimer", true);
                            EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-ResetTimerCounter", MathUtils.getFormatedTime(timeToReset));
                        }
                    } else
                    {
                        EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-CompletedNotClaimed", true);
                    }
                    continue;
                }

                EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-Name", quest.name);
                EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-Description", quest.description);
                EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-IsDaily", quest.isDaily);
                if (quest.id == tracked_quest_id)
                {
                    EffectManager.sendUIEffectVisibility(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-ProgressBar", true);
                    EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-Counter", tracked_quest_progress + "/" + quest.condition_amount);
                    double scaledProgress = MathUtils.MapToRange(tracked_quest_progress, 0, quest.condition_amount);
                    EffectManager.sendUIEffectText(6131, player.Player.Player.channel.owner.transportConnection, true, $"Quest-{quest.id}-Fill", "".PadLeft((int)scaledProgress, 'a'));
                }
            }

            EffectManager.sendUIEffectImageURL(6131, player.Player.Player.channel.owner.transportConnection, true, "ProfilePicture", picture);
        }
    }
}
