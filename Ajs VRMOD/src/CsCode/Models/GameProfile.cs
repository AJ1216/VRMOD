namespace AJS_VRMOD.Models
{
    public class GameProfile
    {
        public string GameId { get; set; }
        public string Launcher { get; set; }
        public GameType GameType { get; set; }
        public string InstallPath { get; set; }
        public string ExecutablePath { get; set; }
    }
}
