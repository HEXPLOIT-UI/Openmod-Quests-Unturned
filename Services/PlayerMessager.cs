using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.UnityEngine.Extensions;
using OpenMod.Unturned.Players;
using Quests.API;
using SDG.Unturned;
using System.Drawing;
using System.Threading.Tasks;

namespace Quests.Services
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class PlayerMessager : IPlayerMessager
    {
        public void SendMessageGlobalAsync(string message, string? iconUrl, Color color)
        {
            var unityColor = color.ToUnityColor();
            ChatManager.serverSendMessage(message, unityColor, iconURL: iconUrl, mode: EChatMode.GLOBAL, useRichTextFormatting: true);
        }

        public void SendMessageLocalAsync(UnturnedPlayer player, string message, string? iconUrl, Color color)
        {
            var unityColor = color.ToUnityColor();
            ChatManager.serverSendMessage(message, unityColor, toPlayer: player.SteamPlayer, iconURL: iconUrl, useRichTextFormatting: true);
        }
    }
}
