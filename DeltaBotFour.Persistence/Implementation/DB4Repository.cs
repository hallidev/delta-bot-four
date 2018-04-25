using System;
using System.Collections.Generic;
using System.Linq;
using Core.Foundation.Exceptions;
using Core.Foundation.Extensions;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using LiteDB;

namespace DeltaBotFour.Persistence.Implementation
{
    public class DB4Repository : IDB4Repository
    {
        private const string DbFileName = "Deltaboards.db";
        private const string DeltaboardsCollectionName = "deltaboards";

        private readonly LiteDatabase _liteDatabase;

        public DB4Repository()
        {
            // DI Conttainer registers this as a singleton, so
            // we want to hold onto this instance
            _liteDatabase = new LiteDatabase(DbFileName);
        }

        public List<Deltaboard> GetCurrentDeltaboards()
        {
            var deltaboards = new List<Deltaboard>();

            var deltaboardCollection = _liteDatabase.GetCollection<Deltaboard>(DeltaboardsCollectionName);

            // Get deltaboards - will be created if they don't exist
            foreach (var deltaboardType in Enum.GetValues(typeof(DeltaboardType)))
            {
                deltaboards.Add(getCurrentDeltaboard(deltaboardCollection, (DeltaboardType)deltaboardType));
            }

            return deltaboards;
        }

        public void AddDeltaboardEntry(string username)
        {
            // Get deltaboards
            var deltaboards = GetCurrentDeltaboards();

            foreach (var deltaboard in deltaboards)
            {
                // Get the existing user entry for each deltaboard (daily, weekly, monthly, etc)
                var existingEntry = deltaboard.Entries.FirstOrDefault(e => e.Username == username);

                if (existingEntry != null)
                {
                    // Increment count if it exists
                    existingEntry.Count++;
                }
                else
                {
                    // Create new entry if there is nothing for this user
                    var newEntry = new DeltaboardEntry
                    {
                        Rank = int.MaxValue,
                        Username = username,
                        Count = 1
                    };

                    deltaboard.Entries.Add(newEntry);
                }

                calculateRanks(deltaboard);
            }

            // Save changes
            updateDeltaboards(deltaboards);
        }

        public void RemoveDeltaboardEntry(string username)
        {
            // Get deltaboards
            var deltaboards = GetCurrentDeltaboards();

            foreach (var deltaboard in deltaboards)
            {
                // Get the existing user entry for each deltaboard (daily, weekly, monthly, etc)
                var existingEntry = deltaboard.Entries.FirstOrDefault(e => e.Username == username);

                if (existingEntry != null)
                {
                    // Increment count if it exists
                    existingEntry.Count--;

                    // If this set the user back to 0, remove the entry
                    if (existingEntry.Count == 0)
                    {
                        deltaboard.Entries.Remove(existingEntry);
                    }
                }

                calculateRanks(deltaboard);
            }

            // Save changes
            updateDeltaboards(deltaboards);
        }

        private Deltaboard getCurrentDeltaboard(LiteCollection<Deltaboard> deltaboardCollection, DeltaboardType type)
        {
            DateTime startUtc;

            switch (type)
            {
                case DeltaboardType.Daily:
                    startUtc = DateTime.Today.AddDays(1).ToUniversalTime();
                    break;
                case DeltaboardType.Weekly:
                    startUtc = DateTime.Now.StartOfWeek(DayOfWeek.Monday).ToUniversalTime();
                    break;
                case DeltaboardType.Monthly:
                    startUtc = DateTime.Now.StartOfMonth().ToUniversalTime();
                    break;
                default:
                    throw new UnhandledEnumException<DeltaboardType>(type);
            }

            // Find deltaboard
            string id = $"{type}-{startUtc}";
            var deltaboard = deltaboardCollection
                .Include(d => d.Entries)
                .FindById(id);

            // Create if it doesn't exist
            if (deltaboard == null)
            {
                deltaboard = createDeltaboard(id, type, startUtc);
                deltaboardCollection.EnsureIndex(d => d.Id, true);
                deltaboardCollection.Insert(deltaboard);
            }

            return deltaboard;
        }

        private Deltaboard createDeltaboard(string id, DeltaboardType type, DateTime createdUtc)
        {
            return new Deltaboard
            {
                Id = id, // Composite keys aren't supported in LiteDb
                DeltaboardType = type,
                CreatedUtc = createdUtc,
                LastUpdatedUtc = createdUtc,
                Entries = new List<DeltaboardEntry>()
            };
        }

        private void calculateRanks(Deltaboard deltaboard)
        {
            int rank = 1;

            foreach (var entry in deltaboard.Entries.OrderByDescending(e => e.Count))
            {
                deltaboard.Entries.First(e => e.Username == entry.Username).Rank = rank;
                rank++;
            }
        }

        private void updateDeltaboards(List<Deltaboard> deltaboards)
        {
            var deltaboardsCollection = _liteDatabase.GetCollection<Deltaboard>(DeltaboardsCollectionName);

            foreach (var deltaboard in deltaboards)
            {
                deltaboardsCollection.Update(deltaboard);
            }
        }
    }
}
