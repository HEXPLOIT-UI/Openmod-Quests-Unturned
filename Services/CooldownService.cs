using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using Quests.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace Quests.Services
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class CooldownService : ICooldownService
    {
        public Dictionary<string, Cooldown> cooldowns { get; private set; }

        public CooldownService()
        {
            cooldowns = new ();
        }
        
        public void SetCooldown(string player, long cooldown, string title)
        {
            if (cooldowns.ContainsKey(player + title))
            {
                cooldowns.Remove(player + title);
            }
            cooldowns.Add(player + title, new Cooldown(player, DateTimeOffset.Now.ToUnixTimeMilliseconds() + cooldown, title));
        }

        public bool hasCooldown(string player, string title)
        {
            return cooldowns.ContainsKey(player + title) && cooldowns[player + title].getCooldown() > DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public long getCooldown(string player, string title)
        {
            return cooldowns.ContainsKey(player + title) ? cooldowns[player + title].getCooldown() - DateTimeOffset.Now.ToUnixTimeMilliseconds() : 0;
        }
    }

    public class Cooldown
    {
        private string player;
        private long cooldown;
        private string key;
        

        public Cooldown(string player, long cooldown, string key)
        {
            this.player = player;
            this.cooldown = cooldown;
            this.key = key;
        }

        public string getPlayer()
        {
            return player;
        }

        public long getCooldown()
        {
            return cooldown;
        }

        public string getKey()
        {
            return key;
        }
    }
}
