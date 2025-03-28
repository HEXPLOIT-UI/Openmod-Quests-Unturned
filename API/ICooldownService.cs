using OpenMod.API.Ioc;
using Quests.Services;
using System.Collections.Generic;

namespace Quests.API
{
    [Service]
    public interface ICooldownService
    {
        public Dictionary<string, Cooldown> cooldowns { get; }
        void SetCooldown(string player, long cooldown, string title);
        bool hasCooldown(string player, string title);
        long getCooldown(string player, string title);
    }
}
