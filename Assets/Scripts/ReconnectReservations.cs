using System;
using System.Collections.Generic;
namespace SecretsReborn
{
    public sealed class ReconnectReservations
    {
        public const double GraceSeconds = 300;
        private sealed class Entry { public string world, character; public double until; public bool active; }
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        public void Bind(string token, string world, string character)
        {
            if (!Guid.TryParseExact(token,"N",out _) || string.IsNullOrEmpty(world) || string.IsNullOrEmpty(character)) throw new ArgumentException("Invalid reconnect binding.");
            entries[token] = new Entry { world = world, character = character, active = true };
        }
        public void Release(string token, double now)
        { if (token != null && entries.TryGetValue(token,out var entry)) { entry.active = false; entry.until = now + GraceSeconds; } }
        public string Resolve(string token, string world, double now)
        { return token != null && entries.TryGetValue(token,out var entry) && entry.world == world && !entry.active && entry.until > now ? entry.character : null; }
        public bool ReservedForOther(string character, string token, string world, double now)
        { foreach (var pair in entries) if (pair.Key != token && pair.Value.world == world && pair.Value.character == character && (pair.Value.active || pair.Value.until > now)) return true; return false; }
    }
}
