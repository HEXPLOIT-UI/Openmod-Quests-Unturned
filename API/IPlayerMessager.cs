using OpenMod.API.Ioc;
using OpenMod.Unturned.Players;
using System.Drawing;

namespace Quests.API
{
    [Service]
    public interface IPlayerMessager
    {
        void SendMessageGlobalAsync(string message, string? iconUrl, Color color);
        void SendMessageLocalAsync(UnturnedPlayer player, string message, string? iconUrl, Color color);
    }
}
