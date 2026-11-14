// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Game.Database;
using osu.Game.EzOsuGame.LocalProfile;
using osu.Game.Scoring;
using Realms;

namespace osu.Game.EzOsuGame.Maintenance
{
    public class EzReplayMaintenanceService
    {
        private readonly RealmAccess realm;
        private readonly ScoreManager scores;

        public EzReplayMaintenanceService(RealmAccess realm, ScoreManager scores)
        {
            this.realm = realm;
            this.scores = scores;
        }

        /// <summary>Local score/replay count per player name, ordered A–Z.</summary>
        public IReadOnlyList<EzLocalProfileUsernameCount> ScanUsernameCounts()
        {
            return realm.Run(r =>
            {
                var counts = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var score in r.All<ScoreInfo>().Where(s => !s.DeletePending))
                {
                    string? username = score.RealmUser?.Username;

                    if (string.IsNullOrWhiteSpace(username))
                        continue;

                    counts.TryGetValue(username, out int existing);
                    counts[username] = existing + 1;
                }

                return counts.Select(kv => new EzLocalProfileUsernameCount(kv.Key, kv.Value))
                             .OrderBy(c => c.Username, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(c => c.Username, StringComparer.Ordinal)
                             .ToList();
            });
        }

        /// <summary>Deletes every local score (and its replay file) owned by the given names. Returns the deleted count.</summary>
        public Task<int> DeleteUsernamesAsync(IReadOnlyCollection<string> usernames)
        {
            if (usernames.Count == 0)
                return Task.FromResult(0);

            return Task.Run(() =>
            {
                int deleted = 0;

                foreach (string username in usernames)
                {
                    int count = realm.Run(r => r.All<ScoreInfo>().Count(s => !s.DeletePending && s.RealmUser.Username == username));

                    if (count == 0)
                        continue;

                    scores.Delete(s => !s.DeletePending && s.RealmUser.Username == username);
                    deleted += count;
                }

                return deleted;
            });
        }
    }
}
