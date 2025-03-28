using Microsoft.Extensions.Configuration;
using OpenMod.API.Eventing;
using OpenMod.Core.Helpers;
using OpenMod.Unturned.Building.Events;
using OpenMod.Unturned.Players.Crafting.Events;
using OpenMod.Unturned.Players.Inventory.Events;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Resources.Events;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Zombies.Events;
using Quests.API;
using Quests.Models;
using Quests.Patches;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Quests.Events
{
    internal class QuestListener : IEventListener<UnturnedZombieDeadEvent>, IEventListener<UnturnedPlayerItemAddedEvent>, 
        IEventListener<UnturnedPlayerDeathEvent>, IEventListener<UnturnedResourceDamagingEvent>, 
        IEventListener<UnturnedPlantHarvestingEvent>, IEventListener<UnturnedPlayerCraftingEvent>, 
        IEventListener<UnturnedZombieDamagingEvent>, IEventListener<UnturnedPlayerDamagedEvent>
    {
        private readonly IUnturnedUserDirectory m_UnturnedUserDirectory;
        private readonly IPlayerMessager m_PlayerMessager;
        private readonly IMongoDbDatabase m_MongoDbDatabase;
        private readonly IQuestProvider m_QuestProvider;
        private readonly IConfiguration m_configuration;

        public QuestListener(IUnturnedUserDirectory unturnedUserDirectory, IPlayerMessager playerMessager, IMongoDbDatabase mongoDbDatabase, IQuestProvider questProvider, IConfiguration configuration)
        {
            m_UnturnedUserDirectory = unturnedUserDirectory;
            m_PlayerMessager = playerMessager;
            m_MongoDbDatabase = mongoDbDatabase;
            m_QuestProvider = questProvider;
            m_configuration = configuration;
        }

        public Task HandleEventAsync(object? sender, UnturnedZombieDeadEvent @event)
        {
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            if (@event.Zombie?.Zombie != Patch_DamageTool.s_CurrentDamageZombieParameters.zombie) return Task.CompletedTask;
            var _damageZombieParameters = Patch_DamageTool.s_CurrentDamageZombieParameters;
            if (_damageZombieParameters.instigator is not Player player) return Task.CompletedTask;
            string steamId = player.channel.owner.playerID.steamID.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Zombies_kill);
            if (quest == null) return Task.CompletedTask;
            if (quest.onlyMegaZombie && !@event.Zombie.Zombie.isMega) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object? sender, UnturnedPlayerItemAddedEvent @event)
        {
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            string steamId = @event.Player.SteamId.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Pickup_items);
            if (quest == null) return Task.CompletedTask;
            if (quest.condition_item_id != -1 && @event.ItemJar.item.id != quest.condition_item_id) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object? sender, UnturnedPlayerDeathEvent @event)
        {
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            if (@event.Instigator == CSteamID.Nil) return Task.CompletedTask;
            string steamId = @event.Instigator.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Players_kill);
            if (quest == null) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object? sender, UnturnedResourceDamagingEvent @event)
        {
            if (@event.Instigator == null || @event.InstigatorId == CSteamID.Nil) return Task.CompletedTask;
            if (@event.ResourceSpawnpoint.health - @event.DamageAmount > 0) return Task.CompletedTask; // calculate whether tree will fall over on damage
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            string steamId = @event.Instigator.SteamId.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Tree_felling);
            if (quest == null) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object? sender, UnturnedPlantHarvestingEvent @event)
        {
            if (@event.Instigator == null || @event.Instigator.SteamId == CSteamID.Nil) return Task.CompletedTask;
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            string steamId = @event.Instigator.SteamId.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Harvesting);
            if (quest == null) return Task.CompletedTask;
            if (quest.condition_item_id != -1 && quest.condition_item_id != @event.Buildable.BarricadeDrop.asset.id) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object? sender, UnturnedPlayerCraftingEvent @event)
        {
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            string steamId = @event.Player.SteamId.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Item_crafting);
            if (quest == null) return Task.CompletedTask;
            if (quest.condition_item_id != -1 && @event.ItemId == quest.condition_item_id) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest);
            return Task.CompletedTask;
        }
        public Task HandleEventAsync(object? sender, UnturnedPlayerDamagedEvent @event)
        {
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            string steamId = @event.Player.SteamId.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Damaging);
            if (quest == null) return Task.CompletedTask;
            if (quest.onlyHeadshots && @event.Limb != ELimb.SKULL || quest.onlyZombie || quest.onlyMegaZombie) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest, @event.DamageAmount);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object? sender, UnturnedZombieDamagingEvent @event)
        {
            if (@event.Instigator == null || @event.Instigator.SteamId == CSteamID.Nil) return Task.CompletedTask;
            if (m_QuestProvider.quests.Count == 0) return Task.CompletedTask;
            string steamId = @event.Instigator.SteamId.ToString();
            var quest = GetQuest(steamId, EQuestCondition.Damaging);
            if (quest == null) return Task.CompletedTask;
            if (quest.playersOnly || (quest.onlyHeadshots && @event.Limb != ELimb.SKULL) || (quest.onlyMegaZombie && !@event.Zombie.Zombie.isMega)) return Task.CompletedTask;
            AddProgressAndUpdateState(steamId, quest, @event.DamageAmount);
            return Task.CompletedTask;
        }

        private QuestModel? GetQuest(string steamId, EQuestCondition condition)
        {
            int tracked_quest = m_MongoDbDatabase.GetTrackedQuestId(steamId);
            if (tracked_quest == -1) return null;
            return m_QuestProvider.quests.Find(x => x.id == tracked_quest && x.condition == condition);
        }

        private void AddProgressAndUpdateState(string steamId, QuestModel quest, int amount = 1)
        {
            Dictionary<string, bool> completed_quests = m_MongoDbDatabase.GetCompletedQuestIds(steamId);
            if (completed_quests.ContainsKey(quest.id.ToString())) return;
            m_MongoDbDatabase.AddTrackedQuestProgress(steamId, amount);
            if (m_MongoDbDatabase.GetTrackedQuestProgress(steamId) < quest.condition_amount) return;
            m_MongoDbDatabase.AddCompletedQuest(steamId, quest.id, false);
            m_MongoDbDatabase.RemoveQuestFromChachedQuests(steamId, quest.id);
            m_MongoDbDatabase.SetTrackedQuestId(steamId, -1);
            m_MongoDbDatabase.SetTrackedQuestProgress(steamId, 0);
            AsyncHelper.Schedule("SavePlayerData", () => m_MongoDbDatabase.SavePlayerInDatabase(steamId));
            if (ulong.TryParse(steamId, out ulong steamIdValue))
            {
                var user = m_UnturnedUserDirectory.FindUser(new CSteamID(steamIdValue));
                if (user == null) return;
                m_PlayerMessager.SendMessageLocalAsync(user.Player, m_configuration.GetValue("Messages:PlayerQuestCompleted:text", "Error, message not found")
                    .Replace("{quest_name}", quest.name).Replace("{quest_description}", quest.description),
                        m_configuration.GetValue<string>("Messages:IconUrl"), ColorTranslator.FromHtml("#" + m_configuration.GetValue("Messages:PlayerQuestCompleted:color", "#fff")));
            }
        }
    }
}
