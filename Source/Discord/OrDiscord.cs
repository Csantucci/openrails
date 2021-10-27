using System;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using DiscordRpc;
/* DISCORD RICH PRESENCE 
* In this file, you can specify the texts for the rich presence, and the client ID for Discord App.
* https://discord.com/rich-presence
* https://discord.com/developers/docs/rich-presence/how-to
* 
* This class uses the following open-source library: https://github.com/Jimmacle/discord-rpc.net (DiscordRpc.dll)
* 
* 
*/
namespace Discord
{
    public class OrDiscord
    {
        //private DiscordRpc.EventHandlers handlers;  // EVENT HANDLERS
        //private DiscordRpc.RichPresence presence; // DISCORD RICH PRESENCE PROPERTIES - List of properties: DiscordRpc.cs - Line 62
        private string CLIENT_ID = "789239264336543814"; // APP CLIENT ID (Working Discord app, configured on the Discord side.)
        private RichPresence Client;

        public OrDiscord()
        {
            Client = new RichPresence();
            Client.Initialize(CLIENT_ID); 
            this.UpdateStatus(false);
        }
        public void Dispose()
        {
            this.Client.Dispose();
        }
        private long getCurrentTime() //UNIX FORMAT - for DISCORD API needs.
        {
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)timeSpan.TotalSeconds;
            
        }

            public void UpdateStatus(bool inGame, string startAt = "", string HeadTo = "")
        {
            if (inGame == true) {
                Client.Update(new RichPresenceBuilder().WithState(startAt + " - " + HeadTo, "Driving train between:").WithArtwork("in-game-image", "Open Rails (MSTS) - Train Simulator", "orts-image", "").WithTimeInfo(DateTime.Now,null));
            }
            else{
                Client.Update(new RichPresenceBuilder().WithState("", "In Main Menu").WithArtwork("orts-image", "Open Rails (MSTS) - Train Simulator","",""));
            }
            
        }

    }
}
