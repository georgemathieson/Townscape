using System;
using System.IO;
using Townscape.CoffeeShop;
using UnityEngine;

namespace Townscape.Runtime.CoffeeShop
{
    /// <summary>
    /// Where Fellside Coffee is saved: <c>fellside-coffee.json</c> in Unity's persistent data folder
    /// (on Windows, <c>%USERPROFILE%\AppData\LocalLow\&lt;company&gt;\&lt;product&gt;</c>; on a Mac,
    /// <c>~/Library/Application Support/&lt;company&gt;/&lt;product&gt;</c>). Writes go to a temporary
    /// file first and then replace the save, so a crash mid-write never leaves half a game.
    /// </summary>
    public static class CoffeeShopSaveFile
    {
        public const string FileName = "fellside-coffee.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists => File.Exists(FilePath);

        /// <summary>
        /// The saved game, or a new one if there isn't a save. A save that can't be read is kept
        /// beside it, renamed, so it can be looked at, and a new game starts.
        /// </summary>
        public static CoffeeShopState Load(CoffeeShopBalance balance)
        {
            var path = FilePath;
            if (!File.Exists(path))
            {
                return NewGame(balance);
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Fellside Coffee] Couldn't read the save at {path}: {e.Message}. Starting a new game for this session.");
                return NewGame(balance);
            }

            if (CoffeeShopSave.TryRead(text, balance, out var state, out var problem))
            {
                return state;
            }

            var damaged = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, $"fellside-coffee.unreadable-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try
            {
                File.Move(path, damaged);
                Debug.LogWarning($"[Fellside Coffee] {problem} It has been kept as {damaged}, and a new game started.");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Fellside Coffee] {problem} Couldn't move it aside ({e.Message}); starting a new game for this session.");
            }

            return NewGame(balance);
        }

        public static void Save(CoffeeShopState state)
        {
            var path = FilePath;
            var temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(temporary, CoffeeShopSave.Write(state));
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException)
            {
                Debug.LogWarning($"[Fellside Coffee] Couldn't save to {path}: {e.Message}");
            }
        }

        public static void Delete()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        /// <summary>A new game with a fresh seed, so each one plays differently.</summary>
        public static CoffeeShopState NewGame(CoffeeShopBalance balance) => CoffeeShopState.NewGame(balance, NewSeed());

        public static int NewSeed() => Environment.TickCount & int.MaxValue;
    }
}
