using System.Collections.Generic;
using acidphantasm_botplacementsystem;

namespace acidphantasm_botplacementsystem.Spawning
{
    /// <summary>
    /// Rolling-window spawn rate limiter. Tracks recent spawn events (timestamped in
    /// raid-seconds, AbstractGame.PastTime) and answers whether another batch may spawn
    /// without exceeding the per-window cap.
    ///
    /// Only "limited" categories (scav, pmc) count toward the cap. Boss and marksman
    /// spawns are recorded for OBSERVATION/logging only - they are never gated, so they
    /// neither consume the budget nor get rejected. This keeps boss entourages and
    /// sniper waves untouched while still surfacing them in the spawn log.
    ///
    /// Single-threaded use only: driven from the NonWavesSpawnScenario.Update tick and
    /// the PMC / bot-creation Harmony patches, all of which run on the Unity main thread.
    /// </summary>
    internal static class SpawnRateLimiter
    {
        private struct SpawnEvent
        {
            public float Time;        // raid seconds (AbstractGame.PastTime)
            public int Count;         // bots in this event
            public bool CountsToLimit; // scav/pmc = true, boss/marksman = false
            public string Category;   // "scav" | "pmc" | "boss" | "marksman"
        }

        // Appended in time order (Update tick is monotonic), so pruning trims the front.
        private static readonly List<SpawnEvent> _events = new();

        public static void Reset()
        {
            _events.Clear();
            if (Plugin.SpawnRateLimitDebugLogging)
                Plugin.LogSource.LogInfo("[ABPS RateLimit] Reset for new raid");
        }

        private static void Prune(float now)
        {
            var cutoff = now - Plugin.SpawnRateLimitWindowSeconds;
            var removeCount = 0;
            for (var i = 0; i < _events.Count; i++)
            {
                if (_events[i].Time < cutoff) removeCount++;
                else break;
            }
            if (removeCount > 0)
                _events.RemoveRange(0, removeCount);
        }

        /// <summary>Sum of bots in the window that count toward the limit (scav + pmc).</summary>
        public static int LimitedCountInWindow(float now)
        {
            Prune(now);
            var sum = 0;
            foreach (var e in _events)
                if (e.CountsToLimit) sum += e.Count;
            return sum;
        }

        /// <summary>Total bots recorded in window across all categories (for logging).</summary>
        public static int TotalCountInWindow(float now)
        {
            Prune(now);
            var sum = 0;
            foreach (var e in _events)
                sum += e.Count;
            return sum;
        }

        /// <summary>
        /// True if 'count' more limited bots can spawn now without exceeding the cap.
        /// Always true when the limiter is disabled.
        /// </summary>
        public static bool CanSpawn(int count, float now)
        {
            if (!Plugin.SpawnRateLimitEnabled) return true;
            return LimitedCountInWindow(now) + count <= Plugin.SpawnRateLimitPerWindow;
        }

        /// <summary>
        /// Record a spawn event. countsToLimit=false (boss/marksman) records for log
        /// visibility only and never affects gating.
        /// </summary>
        public static void Record(int count, float now, string category, bool countsToLimit)
        {
            _events.Add(new SpawnEvent
            {
                Time = now,
                Count = count,
                CountsToLimit = countsToLimit,
                Category = category
            });

            if (Plugin.SpawnRateLimitDebugLogging)
            {
                var limited = LimitedCountInWindow(now);
                var total = TotalCountInWindow(now);
                Plugin.LogSource.LogInfo(
                    $"[ABPS RateLimit] +{count} {category}{(countsToLimit ? "" : " (observed)")} @ {now:0.0}s | " +
                    $"limited-in-window={limited}/{Plugin.SpawnRateLimitPerWindow} | total-in-window={total}");
            }
        }
    }
}
