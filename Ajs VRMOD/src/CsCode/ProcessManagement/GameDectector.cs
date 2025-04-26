using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace VRGameConverter.ProcessManagement
{
    /// <summary>
    /// Handles game detection across multiple platforms and launchers
    /// </summary>
    public class GameDetector
    {
        // List of known game launchers and their registry/installation paths
        private static readonly Dictionary<string, LauncherInfo> knownLaunchers = new Dictionary<string, LauncherInfo>
        {
            { "Steam", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\Valve\Steam", @"SOFTWARE\Wow6432Node\Valve\Steam" },
                InstallPathValue = "InstallPath",
                LibraryFoldersFile = "steamapps\\libraryfolders.vdf",
                GameListingMethod = GameListingMethod.VDFParse,
                ProcessName = "steam"
            }},
            { "Epic Games", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\Epic Games", @"SOFTWARE\Wow6432Node\Epic Games" },
                InstallPathValue = "InstallLocation",
                ManifestFolder = "Manifests",
                GameListingMethod = GameListingMethod.JSONManifests,
                ProcessName = "EpicGamesLauncher"
            }},
            { "GOG Galaxy", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\GOG.com\GalaxyClient", @"SOFTWARE\Wow6432Node\GOG.com\GalaxyClient" },
                InstallPathValue = "ClientInstallationPath",
                GameListingMethod = GameListingMethod.DatabaseParse,
                ProcessName = "GalaxyClient"
            }},
            { "Xbox", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\Microsoft\GamingServices" },
                GameListingMethod = GameListingMethod.WinStoreApps,
                ProcessName = "XboxApp"
            }},
            { "Rockstar Games Launcher", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\Rockstar Games\Launcher", @"SOFTWARE\Wow6432Node\Rockstar Games\Launcher" },
                InstallPathValue = "InstallFolder",
                GameListingMethod = GameListingMethod.RockstarLibrary,
                ProcessName = "Launcher"
            }},
            { "EA App", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\Electronic Arts\EA Desktop", @"SOFTWARE\Wow6432Node\Electronic Arts\EA Desktop" },
                InstallPathValue = "InstallLocation",
                GameListingMethod = GameListingMethod.EALibrary,
                ProcessName = "EADesktop"
            }},
            { "Ubisoft Connect", new LauncherInfo {
                RegistryKeys = new string[] { @"SOFTWARE\Ubisoft\Ubisoft Connect", @"SOFTWARE\Wow6432Node\Ubisoft\Ubisoft Connect" },
                InstallPathValue = "InstallDir",
                GameListingMethod = GameListingMethod.UbisoftLibrary,
                ProcessName = "upc"
            }}
        };
        
        // Cache of detected games
        private Dictionary<string, DetectedGame> detectedGames = new Dictionary<string, DetectedGame>();
        
        public GameDetector()
        {
            RefreshGameList();
        }
        
        /// <summary>
        /// Scan the system for installed games across all supported launchers
        /// </summary>
        public void RefreshGameList()
        {
            detectedGames.Clear();
            
            foreach (var launcher in knownLaunchers)
            {
                try
                {
                    var launcherPath = FindLauncherPath(launcher.Value);
                    if (!string.IsNullOrEmpty(launcherPath))
                    {
                        var games = ListGamesForLauncher(launcher.Key, launcher.Value, launcherPath);
                        foreach (var game in games)
                        {
                            // Use the game ID as key to avoid duplicates across launchers
                            string gameKey = $"{launcher.Key}:{game.GameId}";
                            detectedGames[gameKey] = game;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error detecting games for {launcher.Key}: {ex.Message}");
                }
            }
            
            // Also scan for non-launcher games (direct executables)
            ScanForNonLauncherGames();
        }
        
        /// <summary>
        /// Find the installation path for a launcher using registry keys
        /// </summary>
        private string FindLauncherPath(LauncherInfo launcher)
        {
            foreach (var registryKey in launcher.RegistryKeys)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
                    {
                        if (key != null && !string.IsNullOrEmpty(launcher.InstallPathValue))
                        {
                            string path = key.GetValue(launcher.InstallPathValue) as string;
                            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                            {
                                return path;
                            }
                        }
                    }
                }
                catch
                {
                    // Continue to next registry key
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// List games installed for a specific launcher
        /// </summary>
        private List<DetectedGame> ListGamesForLauncher(string launcherName, LauncherInfo launcherInfo, string launcherPath)
        {
            switch (launcherInfo.GameListingMethod)
            {
                case GameListingMethod.VDFParse:
                    return ListSteamGames(launcherPath);
                case GameListingMethod.JSONManifests:
                    return ListEpicGames(launcherPath);
                case GameListingMethod.DatabaseParse:
                    return ListGOGGames(launcherPath);
                case GameListingMethod.WinStoreApps:
                    return ListXboxGames();
                case GameListingMethod.RockstarLibrary:
                    return ListRockstarGames(launcherPath);
                case GameListingMethod.EALibrary:
                    return ListEAGames(launcherPath);
                case GameListingMethod.UbisoftLibrary:
                    return ListUbisoftGames(launcherPath);
                default:
                    return new List<DetectedGame>();
            }
        }
        
        /// <summary>
        /// Find Steam games using VDF file parsing
        /// </summary>
        private List<DetectedGame> ListSteamGames(string steamPath)
        {
            var games = new List<DetectedGame>();
            
            try
            {
                // Parse library folders VDF to find all Steam libraries
                string libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFoldersPath))
                {
                    List<string> libraryPaths = ParseSteamLibraryFolders(libraryFoldersPath);
                    
                    // Add the default Steam library
                    libraryPaths.Add(steamPath);
                    
                    // Search for game manifests in each library
                    foreach (string libraryPath in libraryPaths)
                    {
                        string appsPath = Path.Combine(libraryPath, "steamapps");
                        if (Directory.Exists(appsPath))
                        {
                            // Find all appmanifest_*.acf files
                            foreach (string manifestFile in Directory.GetFiles(appsPath, "appmanifest_*.acf"))
                            {
                                DetectedGame game = ParseSteamAppManifest(manifestFile, libraryPath);
                                if (game != null)
                                {
                                    games.Add(game);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing Steam games: {ex.Message}");
            }
            
            return games;
        }
        
        /// <summary>
        /// Parse Steam's libraryfolders.vdf to find all library paths
        /// </summary>
        private List<string> ParseSteamLibraryFolders(string vdfPath)
        {
            var libraryPaths = new List<string>();
            string[] lines = File.ReadAllLines(vdfPath);
            
            foreach (string line in lines)
            {
                // Look for "path" entries in the VDF
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("\"path\""))
                {
                    // Extract the path value between quotes
                    int firstQuote = trimmedLine.IndexOf('"', 6); // Start after "path"
                    if (firstQuote >= 0)
                    {
                        int secondQuote = trimmedLine.IndexOf('"', firstQuote + 1);
                        if (secondQuote > firstQuote)
                        {
                            string path = trimmedLine.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                            if (Directory.Exists(path))
                            {
                                libraryPaths.Add(path);
                            }
                        }
                    }
                }
            }
            
            return libraryPaths;
        }
        
        /// <summary>
        /// Parse a Steam app manifest file to extract game information
        /// </summary>
        private DetectedGame ParseSteamAppManifest(string manifestPath, string libraryPath)
        {
            try
            {
                string[] lines = File.ReadAllLines(manifestPath);
                string appId = null;
                string name = null;
                string installDir = null;
                
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    if (trimmedLine.StartsWith("\"appid\""))
                    {
                        appId = ExtractValueFromVdfLine(trimmedLine);
                    }
                    else if (trimmedLine.StartsWith("\"name\""))
                    {
                        name = ExtractValueFromVdfLine(trimmedLine);
                    }
                    else if (trimmedLine.StartsWith("\"installdir\""))
                    {
                        installDir = ExtractValueFromVdfLine(trimmedLine);
                    }
                }
                
                if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(installDir))
                {
                    string gamePath = Path.Combine(libraryPath, "steamapps", "common", installDir);
                    
                    // Find the main executable
                    string exePath = FindGameExecutable(gamePath);
                    
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        return new DetectedGame
                        {
                            GameId = appId,
                            GameName = name,
                            InstallPath = gamePath,
                            ExecutablePath = exePath,
                            LauncherName = "Steam",
                            LaunchMethod = LaunchMethod.Direct
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Steam manifest {manifestPath}: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Extract a value from a VDF line (format: "key" "value")
        /// </summary>
        private string ExtractValueFromVdfLine(string line)
        {
            int firstQuote = line.IndexOf('"');
            if (firstQuote >= 0)
            {
                int secondQuote = line.IndexOf('"', firstQuote + 1);
                if (secondQuote > firstQuote)
                {
                    int thirdQuote = line.IndexOf('"', secondQuote + 1);
                    if (thirdQuote > secondQuote)
                    {
                        int fourthQuote = line.IndexOf('"', thirdQuote + 1);
                        if (fourthQuote > thirdQuote)
                        {
                            return line.Substring(thirdQuote + 1, fourthQuote - thirdQuote - 1);
                        }
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Find Epic Games
        /// </summary>
        private List<DetectedGame> ListEpicGames(string epicPath)
        {
            // Implementation would parse Epic's manifest JSON files
            // Simplified placeholder
            return new List<DetectedGame>();
        }
        
        /// <summary>
        /// Find GOG Galaxy games
        /// </summary>
        private List<DetectedGame> ListGOGGames(string gogPath)
        {
            // Implementation would parse GOG Galaxy's database
            // Simplified placeholder
            return new List<DetectedGame>();
        }
        
        /// <summary>
        /// Find Xbox Game Pass games
        /// </summary>
        private List<DetectedGame> ListXboxGames()
        {
            // Implementation would use Windows Store API
            // Simplified placeholder
            return new List<DetectedGame>();
        }
        
        /// <summary>
        /// Find Rockstar Games
        /// </summary>
        private List<DetectedGame> ListRockstarGames(string rockstarPath)
        {
            // Implementation for Rockstar Games Launcher
            // Simplified placeholder
            return new List<DetectedGame>();
        }
        
        /// <summary>
        /// Find EA games
        /// </summary>
        private List<DetectedGame> ListEAGames(string eaPath)
        {
            // Implementation for EA App
            // Simplified placeholder
            return new List<DetectedGame>();
        }
        
        /// <summary>
        /// Find Ubisoft games
        /// </summary>
        private List<DetectedGame> ListUbisoftGames(string ubiPath)
        {
            // Implementation for Ubisoft Connect
            // Simplified placeholder
            return new List<DetectedGame>();
        }
        
        /// <summary>
        /// Scan custom folders for non-launcher games
        /// </summary>
        private void ScanForNonLauncherGames()
        {
            // Implementation would scan user-defined folders
            // Not implemented in this example
        }
        
        /// <summary>
        /// Find the main executable file for a game
        /// </summary>
        private string FindGameExecutable(string gamePath)
        {
            if (!Directory.Exists(gamePath))
                return null;
                
            // Look for executables in the game directory
            foreach (string exePath in Directory.GetFiles(gamePath, "*.exe", SearchOption.AllDirectories))
            {
                // Skip obvious non-game executables
                string filename = Path.GetFileName(exePath).ToLower();
                if (filename == "unins000.exe" || 
                    filename == "launcher.exe" || 
                    filename.Contains("setup") ||
                    filename.Contains("redist") ||
                    filename.Contains("crash"))
                {
                    continue;
                }
                
                // Check file size - main executables are usually large
                FileInfo fileInfo = new FileInfo(exePath);
                if (fileInfo.Length > 5 * 1024 * 1024) // > 5 MB
                {
                    return exePath;
                }
            }
            
            // If no large EXE was found, try some common patterns
            string[] commonNames = { "game.exe", "app.exe", Path.GetFileName(gamePath) + ".exe" };
            foreach (string name in commonNames)
            {
                string path = Path.Combine(gamePath, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            // If all else fails, return the first EXE found
            string[] allExes = Directory.GetFiles(gamePath, "*.exe");
            if (allExes.Length > 0)
            {
                return allExes[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Get the list of detected games
        /// </summary>
        public IEnumerable<DetectedGame> GetDetectedGames()
        {
            return detectedGames.Values;
        }
        
        /// <summary>
        /// Get a game by its key
        /// </summary>
        public DetectedGame GetGame(string gameKey)
        {
            if (detectedGames.TryGetValue(gameKey, out var game))
            {
                return game;
            }
            return null;
        }
    }
    
    /// <summary>
    /// Manages active game processes for VR injection
    /// </summary>
    public class GameProcessManager
    {
        // Process handle for the active game
        private Process gameProcess;
        
        /// <summary>
        /// Launch a game directly or via launcher
        /// </summary>
        public Process LaunchGame(DetectedGame game)
        {
            switch (game.LaunchMethod)
            {
                case LaunchMethod.Direct:
                    return LaunchGameDirect(game);
                case LaunchMethod.LauncherProtocol:
                    return LaunchGameViaProtocol(game);
                case LaunchMethod.LauncherApp:
                    return LaunchGameViaLauncher(game);
                default:
                    throw new ArgumentException($"Unsupported launch method: {game.LaunchMethod}");
            }
        }
        
        /// <summary>
        /// Launch a game directly from its executable
        /// </summary>
        private Process LaunchGameDirect(DetectedGame game)
        {
            try
            {
                // Create process start info
                var startInfo = new ProcessStartInfo
                {
                    FileName = game.ExecutablePath,
                    WorkingDirectory = Path.GetDirectoryName(game.ExecutablePath),
                    UseShellExecute = true
                };
                
                // Launch the process
                gameProcess = Process.Start(startInfo);
                return gameProcess;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch game: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Launch a game via a launcher protocol (e.g., steam://run/123)
        /// </summary>
        private Process LaunchGameViaProtocol(DetectedGame game)
        {
            try
            {
                string protocol = game.LauncherName.ToLower();
                string protocolUrl = $"{protocol}://run/{game.GameId}";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = protocolUrl,
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);
                
                // The process started this way won't be the actual game, so find it by scanning
                return WaitForGameProcess(game);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch game via protocol: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Launch a game via its launcher application
        /// </summary>
        private Process LaunchGameViaLauncher(DetectedGame game)
        {
            try
            {
                // Find launcher process name
                string launcherProcess = null;
                if (knownLaunchers.TryGetValue(game.LauncherName, out var launcherInfo))
                {
                    launcherProcess = launcherInfo.ProcessName;
                }
                
                if (string.IsNullOrEmpty(launcherProcess))
                {
                    throw new Exception($"Unknown launcher: {game.LauncherName}");
                }
                
                // Check if launcher is running
                Process[] launcherProcesses = Process.GetProcessesByName(launcherProcess);
                if (launcherProcesses.Length == 0)
                {
                    // Launch the launcher
                    Process.Start(launcherProcess);
                    
                    // Wait for launcher to start
                    int retries = 10;
                    while (retries > 0 && Process.GetProcessesByName(launcherProcess).Length == 0)
                    {
                        System.Threading.Thread.Sleep(1000);
                        retries--;
                    }
                }
                
                // Launch the game via protocol
                return LaunchGameViaProtocol(game);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch game via launcher: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Wait for a game process to start and return it
        /// </summary>
        private Process WaitForGameProcess(DetectedGame game)
        {
            // Get the expected process name
            string exeName = Path.GetFileNameWithoutExtension(game.ExecutablePath);
            
            // Remember current processes
            HashSet<int> existingProcessIds = new HashSet<int>();
            foreach (var process in Process.GetProcessesByName(exeName))
            {
                existingProcessIds.Add(process.Id);
            }
            
            // Wait for new processes
            int retries = 30; // 30 seconds timeout
            while (retries > 0)
            {
                Process[] processes = Process.GetProcessesByName(exeName);
                foreach (var process in processes)
                {
                    if (!existingProcessIds.Contains(process.Id))
                    {
                        gameProcess = process;
                        return process;
                    }
                }
                
                System.Threading.Thread.Sleep(1000);
                retries--;
            }
            
            throw new TimeoutException($"Timed out waiting for game process: {exeName}");
        }
        
        /// <summary>
        /// Attach to an already running game process
        /// </summary>
        public Process AttachToGame(DetectedGame game)
        {
            string exeName = Path.GetFileNameWithoutExtension(game.ExecutablePath);
            Process[] processes = Process.GetProcessesByName(exeName);
            
            if (processes.Length == 0)
            {
                throw new Exception($"Game {game.GameName} is not running");
            }
            
            // For simplicity, use the first matching process
            gameProcess = processes[0];
            return gameProcess;
        }
        
        /// <summary>
        /// Check if a game is running
        /// </summary>
        public bool IsGameRunning(DetectedGame game)
        {
            if (game == null)
                return false;
                
            string exeName = Path.GetFileNameWithoutExtension(game.ExecutablePath);
            return Process.GetProcessesByName(exeName).Length > 0;
        }
    }
    
    // Support classes
    
    public class LauncherInfo
    {
        public string[] RegistryKeys { get; set; }
        public string InstallPathValue { get; set; }
        public string LibraryFoldersFile { get; set; }
        public string ManifestFolder { get; set; }
        public GameListingMethod GameListingMethod { get; set; }
        public string ProcessName { get; set; }
    }
    
    public class DetectedGame
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string InstallPath { get; set; }
        public string ExecutablePath { get; set; }
        public string LauncherName { get; set; }
        public LaunchMethod LaunchMethod { get; set; }
        public GameType GameType { get; set; }
        
        public override string ToString()
        {
            return $"{GameName} ({LauncherName})";
        }
    }
    
    public enum GameListingMethod
    {
        VDFParse,          // Steam
        JSONManifests,     // Epic
        DatabaseParse,     // GOG
        WinStoreApps,      // Xbox
        RockstarLibrary,   // Rockstar
        EALibrary,         // EA
        UbisoftLibrary     // Ubisoft
    }
    
    public enum LaunchMethod
    {
        Direct,            // Launch directly via executable
        LauncherProtocol,  // Launch via protocol (e.g., steam://run/123)
        LauncherApp        // Launch via launcher application
    }
    
    public enum GameType
    {
        Unknown,
        GTA4,
        GTA5,
        HogwartsLegacy,
        SpiderMan,
        // Add more game types as needed
    }
    
    public class Ray
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
    }
    
    // Dictionary wrapper - simplified for example
    public class Dictionary<TKey, TValue>
    {
        private System.Collections.Generic.Dictionary<TKey, TValue> dict = new System.Collections.Generic.Dictionary<TKey, TValue>();
        
        public void Add(TKey key, TValue value)
        {
            dict.Add(key, value);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dict.TryGetValue(key, out value);
        }
        
        public TValue this[TKey key]
        {
            get { return dict[key]; }
            set { dict[key] = value; }
        }
        
        public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection Values
        {
            get { return dict.Values; }
        }
        
        public void Clear()
        {
            dict.Clear();
        }
    }
}