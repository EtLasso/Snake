using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Snake.Systems
{
    public static class SoundManager
    {
        private static readonly string _soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
        private static Dictionary<string, SoundPlayer> _players = new Dictionary<string, SoundPlayer>();
        private static bool _muted = false;

        // Cache für SoundPlayer, um Lags zu verhindern
        static SoundManager()
        {
            try
            {
                if (!Directory.Exists(_soundPath))
                    Directory.CreateDirectory(_soundPath);
            }
            catch { }
        }

        public static bool IsMuted => _muted;

        public static void ToggleMute()
        {
            _muted = !_muted;
        }

        // --- HAUPTMETHODEN FÜR DAS SPIEL ---

        public static void PlayEat()
        {
            if (_muted) return;
            // Versuche Datei zu spielen, sonst Fallback auf Retro-Beep
            if (!PlayWav("eat.wav"))
                PlayRetroBeep(600, 50);
        }

        public static void PlayBonus()
        {
            if (_muted) return;
            if (!PlayWav("bonus.wav"))
            {
                // Kleines Arpeggio (Tonleiter)
                Task.Run(() => {
                    try
                    {
                        Console.Beep(600, 50);
                        System.Threading.Thread.Sleep(30);
                        Console.Beep(800, 50);
                        System.Threading.Thread.Sleep(30);
                        Console.Beep(1000, 50);
                    }
                    catch { }
                });
            }
        }

        public static void PlayDie()
        {
            if (_muted) return;
            if (!PlayWav("die.wav"))
            {
                // Trauriger Abstieg
                Task.Run(() => {
                    try
                    {
                        Console.Beep(400, 150);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(300, 150);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(200, 300);
                    }
                    catch { }
                });
            }
        }

        public static void PlayGameStart()
        {
            if (_muted) return;
            if (!PlayWav("start.wav"))
            {
                Task.Run(() => {
                    try
                    {
                        Console.Beep(400, 100);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(600, 100);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(1000, 200);
                    }
                    catch { }
                });
            }
        }

        public static void PlayClick()
        {
            if (_muted) return;
            if (!PlayWav("click.wav"))
                PlayRetroBeep(1000, 20);
        }

        public static void PlayLevelUp()
        {
            if (_muted) return;
            if (!PlayWav("levelup.wav"))
            {
                Task.Run(() => {
                    try
                    {
                        Console.Beep(800, 80);
                        System.Threading.Thread.Sleep(30);
                        Console.Beep(1200, 150);
                    }
                    catch { }
                });
            }
        }

        // --- INTERNE LOGIK ---

        private static bool PlayWav(string filename)
        {
            string fullPath = Path.Combine(_soundPath, filename);

            if (File.Exists(fullPath))
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (!_players.ContainsKey(filename))
                        {
                            _players[filename] = new SoundPlayer(fullPath);
                            _players[filename].Load(); // Vorladen
                        }
                        _players[filename].Play();
                    }
                    catch { /* Fehler ignorieren, Spiel soll nicht abstürzen */ }
                });
                return true;
            }
            return false;
        }

        private static void PlayRetroBeep(int freq, int duration)
        {
            // Task.Run ist wichtig, da Console.Beep den Thread blockiert!
            Task.Run(() => {
                try { Console.Beep(freq, duration); } catch { }
            });
        }

        public static void DisposePlayers()
        {
            foreach (var player in _players.Values)
            {
                player?.Dispose();
            }
            _players.Clear();
        }
    }
}