using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRGameConverter.ProcessManagement;

namespace VRGameConverter.Profiles
{
    /// <summary>
    /// Manages game profiles and automatically detects game types
    /// </summary>
    public class GameProfileManager
    {
        private Dictionary<string, GameProfile> profiles = new Dictionary<string, GameProfile>();
        private List<GameTypeSignature> gameSignatures = new List<GameTypeSignature>();

        public GameProfileManager()
        {
            InitializeGameSignatures();
        }

        private void InitializeGameSignatures()
        {
            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.GTA5,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "GTA5.exe", Required = true },
                    new FilePattern { Pattern = "common.rpf", Required = true },
                    new FilePattern { Pattern = "x64a.rpf", Required = true }
                },
                FolderPatterns = new List<string> { "update", "x64" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.GTA4,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "GTAIV.exe", Required = true },
                    new FilePattern { Pattern = "common.rpf", Required = true }
                },
                FolderPatterns = new List<string> { "pc" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.HogwartsLegacy,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "HogwartsLegacy.exe", Required = true },
                    new FilePattern { Pattern = "Engine.dll", Required = false }
                },
                FolderPatterns = new List<string> { "Phoenix", "Content" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.SpiderMan,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "Spider-Man.exe", Required = true },
                    new FilePattern { Pattern = "MarvelsFE.dll", Required = false }
                },
                FolderPatterns = new List<string> { "asset_resources" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.Cyberpunk2077,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "Cyberpunk2077.exe", Required = true },
                    new FilePattern { Pattern = "REDprelauncher.exe", Required = false }
                },
                FolderPatterns = new List<string> { "r6", "bin", "archive" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.RedDeadRedemption2,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "RDR2.exe", Required = true }
                },
                FolderPatterns = new List<string> { "x64", "update" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.CallOfDutyBlackOps1,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "BlackOps.exe", Required = true }
                },
                FolderPatterns = new List<string> { "zone" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.CallOfDutyBlackOps2,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "t6mp.exe", Required = false },
                    new FilePattern { Pattern = "t6sp.exe", Required = false }
                },
                FolderPatterns = new List<string> { "zone" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.CallOfDutyBlackOps3,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "BlackOps3.exe", Required = true }
                },
                FolderPatterns = new List<string> { "players", "zone" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.WalkingDeadSaintsSinners,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "TWD.exe", Required = true }
                },
                FolderPatterns = new List<string> { "Content", "Engine" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.BatmanArkhamKnight,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "BatmanAK.exe", Required = true }
                },
                FolderPatterns = new List<string> { "BmGame", "Engine" }
            });

            gameSignatures.Add(new GameTypeSignature
            {
                GameType = GameType.WatchDogs2,
                FilePatterns = new List<FilePattern>
                {
                    new FilePattern { Pattern = "WatchDogs2.exe", Required = true }
                },
                FolderPatterns = new List<string> { "data_win64" }
            });
        }

        public GameType DetectGameType(DetectedGame game)
        {
            string installPath = game.InstallPath;

            if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
            {
                return GameType.Unknown;
            }

            foreach (var signature in gameSignatures)
            {
                if (MatchesSignature(installPath, signature))
                {
                    return signature.GameType;
                }
            }

            string exeName = Path.GetFileName(game.ExecutablePath).ToLower();

            if (exeName.Contains("gta5")) return GameType.GTA5;
            if (exeName.Contains("gtaiv") || exeName.Contains("gta4")) return GameType.GTA4;
            if (exeName.Contains("hogwarts")) return GameType.HogwartsLegacy;
            if (exeName.Contains("spider")) return GameType.SpiderMan;
            if (exeName.Contains("cyberpunk")) return GameType.Cyberpunk2077;
            if (exeName.Contains("rdr2")) return GameType.RedDeadRedemption2;
            if (exeName.Contains("blackops3")) return GameType.CallOfDutyBlackOps3;
            if (exeName.Contains("blackops")) return GameType.CallOfDutyBlackOps1;
            if (exeName.Contains("t6")) return GameType.CallOfDutyBlackOps2;
            if (exeName.Contains("twd")) return GameType.WalkingDeadSaintsSinners;
            if (exeName.Contains("batmanak")) return GameType.BatmanArkhamKnight;
            if (exeName.Contains("watchdogs2")) return GameType.WatchDogs2;

            return GameType.Unknown;
        }

        private bool MatchesSignature(string path, GameTypeSignature signature)
        {
            foreach (var filePattern in signature.FilePatterns.Where(fp => fp.Required))
            {
                bool found = Directory.GetFiles(path, filePattern.Pattern, SearchOption.AllDirectories).Any();
                if (!found) return false;
            }

            int matchedFolders = signature.FolderPatterns.Count(folder =>
                Directory.GetDirectories(path, folder, SearchOption.AllDirectories).Any());

            return matchedFolders >= Math.Max(1, signature.FolderPatterns.Count / 2);
        }
            public GameProfile GetOrCreateProfile(DetectedGame game)
{       
            string gameKey = $"{game.LauncherName}:{game.GameId}";

            if (profiles.TryGetValue(gameKey, out var existingProfile))
    {
        return existingProfile;
    }       

            GameType gameType = DetectGameType(game);
            game.GameType = gameType;

            var newProfile = new GameProfile
    {
                   GameId = game.GameId,
                    InstallPath = game.InstallPath,
             ExecutablePath = game.ExecutablePath,
            GameType = gameType,
            GameName = game.GameName, // You might want to set GameName as well
            Settings = new ProfileSettings() // Initialize the Settings property
    };

    // Initialize nested settings with default values
        newProfile.Settings.OpenVR = new OpenVrSettings
    {
            Enabled = false, // Set default value
            RenderWidth = 0,   // Set default value
            RenderHeight = 0  // Set default value
        // Add other default OpenVR settings
    };

        newProfile.Settings.Native = new NativeSettings
    {
            ForceAspectRatio = false, // Set default value
            AspectRatio = 1.777f       // Set default value (e.g., 16:9)
        // Add other default Native settings
    };

        profiles[gameKey] = newProfile;
        return newProfile;
}