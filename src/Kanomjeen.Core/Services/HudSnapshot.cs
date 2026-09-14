using System;
using System.Reflection;
using Rocket.Unturned.Player;

namespace Kanomjeen.Core.Services
{
    public sealed class HudSnapshot
    {
        public byte Health;
        public byte Food;
        public byte Water;
        public byte Virus;
        public byte Stamina;
        public byte Oxygen;
        public float Bearing;
        public float X;
        public float Y;
        public float Z;
        public string Direction;

        public static HudSnapshot From(UnturnedPlayer player)
        {
            var snapshot = new HudSnapshot();
            if (player?.Player == null) return snapshot;
            snapshot.Health = Clamp(player.Health);
            var life = player.Player.life;
            snapshot.Food = ReadByte(life, "food", "Food");
            snapshot.Water = ReadByte(life, "water", "Water");
            snapshot.Virus = ReadByte(life, "virus", "Virus");
            snapshot.Oxygen = ReadByte(life, "oxygen", "Oxygen");
            var movement = player.Player.movement;
            snapshot.Stamina = ReadByte(movement, "stamina", "Stamina");
            var rotation = player.Player.transform.eulerAngles.y;
            snapshot.Bearing = (rotation + 360f) % 360f;
            snapshot.Direction = DirectionFor(snapshot.Bearing);
            var position = player.Player.transform.position;
            snapshot.X = position.x;
            snapshot.Y = position.y;
            snapshot.Z = position.z;
            return snapshot;
        }

        private static string DirectionFor(float bearing)
        {
            var index = (int)Math.Round(bearing / 45f) % 8;
            return new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[index];
        }

        private static byte ReadByte(object target, params string[] names)
        {
            if (target == null) return 0;
            var type = target.GetType();
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    try { return Clamp(Convert.ToInt32(property.GetValue(target, null))); } catch { }
                }
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    try { return Clamp(Convert.ToInt32(field.GetValue(target))); } catch { }
                }
            }
            return 0;
        }

        private static byte Clamp(int value) => (byte)Math.Max(0, Math.Min(100, value));
    }
}
