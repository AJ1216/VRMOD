using System.Collections.Generic;

namespace AJS_VRMOD.Models
{
    public class GameTypeSignature
    {
        public GameType GameType { get; set; }
        public List<FilePattern> FilePatterns { get; set; } = new List<FilePattern>();
        public List<string> FolderPatterns { get; set; } = new List<string>();
    }
}
