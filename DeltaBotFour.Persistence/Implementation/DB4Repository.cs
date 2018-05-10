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
        private const string DbFileName = "DeltaBotFour.db";
        private const string DeltaBotStateCollectionName = "deltabotstate";
        private const string DeltaCommentsCollectionName = "deltacomments";
        private const string DeltaboardsCollectionName = "deltaboards";
        private const string WATTArticlesCollectionName = "wattarticles";
        private const string BsonIdField = "_id";
        private const string BsonValueField = "value";
        private const string LastActivityTimeUtcKey = "last_processed_comment_time_utc";
        private const string IgnoreQuotedDeltaPMUserListKey = "ignore_quoted_delta_pm_user_list";

        private readonly LiteDatabase _liteDatabase;

        public DB4Repository()
        {
            // DI Conttainer registers this as a singleton, so
            // we want to hold onto this instance
            _liteDatabase = new LiteDatabase(DbFileName);
        }

        public DateTime GetLastActivityTimeUtc()
        {
            var stateCollection = getState();
            return ((DateTime)stateCollection.FindById(LastActivityTimeUtcKey)[BsonValueField]).ToUniversalTime();
        }

        public void SetLastActivityTimeUtc()
        {
            var stateCollection = getState();

            var lastActivityTimeUtcDocument = stateCollection.FindById(LastActivityTimeUtcKey);
            lastActivityTimeUtcDocument[BsonValueField] = DateTime.UtcNow;
            stateCollection.Update(lastActivityTimeUtcDocument);
        }

        public bool DeltaCommentExists(string commentId)
        {
            return GetDeltaComment(commentId) != null;
        }

        public DeltaComment GetDeltaComment(string commentId)
        {
            return _liteDatabase.GetCollection<DeltaComment>(DeltaCommentsCollectionName).FindById(commentId);
        }

        public void UpsertDeltaComment(DeltaComment commentWithDelta)
        {
            var deltaCommentsCollection = _liteDatabase.GetCollection<DeltaComment>(DeltaCommentsCollectionName);
            deltaCommentsCollection.EnsureIndex(d => d.Id, true);
            deltaCommentsCollection.Upsert(commentWithDelta);
        }

        public void RemoveDeltaComment(string commentId)
        {
            var deltaCommentsCollection = _liteDatabase.GetCollection<DeltaComment>(DeltaCommentsCollectionName);
            deltaCommentsCollection.Delete(commentId);
        }

        public List<DeltaComment> GetDeltaCommentsForPost(string postId, string authorName = "")
        {
            var deltaCommentsCollection = _liteDatabase.GetCollection<DeltaComment>(DeltaCommentsCollectionName);
            return deltaCommentsCollection.Find(dc => dc.ParentPostId == postId && (string.IsNullOrEmpty(authorName) || dc.FromUsername == authorName)).ToList();
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

        public List<string> GetIgnoreQuotedDeltaPMUserList()
        {
            var stateCollection = getState();

            var ignoreQuotedDeltaPMUserListDocument = stateCollection.FindById(IgnoreQuotedDeltaPMUserListKey);
            var bsonList = ignoreQuotedDeltaPMUserListDocument[BsonValueField].AsArray;

            var users = new List<string>();
            foreach (var value in bsonList)
            {
                users.Add(value.AsString);
            }

            return users;
        }

        public void AddIgnoredQuotedDeltaPMUser(string username)
        {
            var stateCollection = getState();

            var ignoreQuotedDeltaPMUserListDocument = stateCollection.FindById(IgnoreQuotedDeltaPMUserListKey);
            var bsonList = ignoreQuotedDeltaPMUserListDocument[BsonValueField].AsArray;

            bool userExists = false;
            foreach (var value in bsonList)
            {
                if (value.AsString == username)
                {
                    userExists = true;
                    break;
                }
            }

            if (!userExists)
            {
                bsonList.Add(username);
                stateCollection.Update(ignoreQuotedDeltaPMUserListDocument);
            }
        }

        public void UpsertWATTArticle(WATTArticle article)
        {
            var wattArticlesCollection = _liteDatabase.GetCollection<WATTArticle>(WATTArticlesCollectionName);
            wattArticlesCollection.EnsureIndex(d => d.Id, true);
            wattArticlesCollection.Upsert(article);
        }

        public WATTArticle GetWattArticleForPost(string postId)
        {
            var wattArticlesCollection = _liteDatabase.GetCollection<WATTArticle>(WATTArticlesCollectionName);
            return wattArticlesCollection.Find(dc => dc.RedditPostId == postId).FirstOrDefault();
        }

        private LiteCollection<BsonDocument> getState()
        {
            var stateCollection = _liteDatabase.GetCollection<BsonDocument>(DeltaBotStateCollectionName);

            // Ensure that state key / value pairs exist
            if (stateCollection.FindById(LastActivityTimeUtcKey) == null)
            {
                var lastActivityTimeUtcDocument = new BsonDocument();
                lastActivityTimeUtcDocument[BsonIdField] = LastActivityTimeUtcKey;
                lastActivityTimeUtcDocument[BsonValueField] = DateTime.UtcNow;
                stateCollection.Insert(lastActivityTimeUtcDocument);
            }

            if (stateCollection.FindById(IgnoreQuotedDeltaPMUserListKey) == null)
            {
                var ignoreQuotedDeltaPMUserListDocument = new BsonDocument();
                ignoreQuotedDeltaPMUserListDocument[BsonIdField] = IgnoreQuotedDeltaPMUserListKey;
                ignoreQuotedDeltaPMUserListDocument[BsonValueField] = new List<BsonValue>();
                stateCollection.Insert(ignoreQuotedDeltaPMUserListDocument);
            }

            return stateCollection;
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
                case DeltaboardType.Yearly:
                    startUtc = DateTime.Now.StartOfYear().ToUniversalTime();
                    break;
                case DeltaboardType.AllTime:
                    startUtc = DateTime.MinValue.ToUniversalTime();
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
