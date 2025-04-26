namespace AJS_VRMOD.Models
{
    public class DetectedGame
    {
        public string GameId { get; set; }
        public string LauncherName { get; set; }
        public string InstallPath { get; set; }
        public string ExecutablePath { get; set; }
        public GameType GameType { get; set; } = GameType.Unknown;
    }
}
