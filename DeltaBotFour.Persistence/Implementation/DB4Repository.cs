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
        private const int LastProcessedIdCountToTrack = 200;

        private const string DbFileName = "DeltaBotFour.db";
        private const string DeltaBotStateCollectionName = "deltabotstate";
        private const string DeltaCommentsCollectionName = "deltacomments";
        private const string DeltaboardsCollectionName = "deltaboards";
        private const string DeltaLogPostMappingsCollectionName = "deltalogpostmappings";
        private const string WATTArticlesCollectionName = "wattarticles";
        private const string BsonIdField = "_id";
        private const string BsonValueField = "value";
        private const string LastActivityTimeUtcKey = "last_processed_comment_time_utc";
        private const string LastProcessedCommentIdsKey = "last_processed_comment_ids";
        private const string LastProcessedEditIdsKey = "last_processed_edit_ids";
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

        public List<string> GetLastProcessedCommentIds()
        {
            var stateCollection = getState();

            var document = stateCollection.FindById(LastProcessedCommentIdsKey);
            var bsonList = document[BsonValueField].AsArray;

            var commentIds = new List<string>();
            foreach (var value in bsonList)
            {
                commentIds.Add(value.AsString);
            }

            // Reverse the list so newest are on top
            commentIds.Reverse();

            return commentIds;
        }

        public void SetLastProcessedCommentId(string commentId)
        {
            // Get current list of last processed comments
            var commentIds = GetLastProcessedCommentIds();

            // We want oldest on top here, and it's currently sorted newest on top
            commentIds.Reverse();

            // Remove the first item (oldest) and add new item
            if (commentIds.Count > LastProcessedIdCountToTrack)
            {
                commentIds.RemoveAt(0);
            }
            
            commentIds.Add(commentId);

            var stateCollection = getState();

            var document = stateCollection.FindById(LastProcessedCommentIdsKey);
            var bsonList = document[BsonValueField].AsArray;
            
            bsonList.Clear();
            
            foreach (var id in commentIds)
            {
                bsonList.Add(id);
            }

            stateCollection.Update(document);
        }

        public List<string> GetLastProcessedEditIds()
        {
            var stateCollection = getState();

            var document = stateCollection.FindById(LastProcessedEditIdsKey);
            var bsonList = document[BsonValueField].AsArray;

            var editIds = new List<string>();
            foreach (var value in bsonList)
            {
                editIds.Add(value.AsString);
            }

            // Reverse the list so newest are on top
            editIds.Reverse();

            return editIds;
        }

        public void SetLastProcessedEditId(string editId)
        {
            // Get current list of last processed comments
            var editIds = GetLastProcessedEditIds();

            // We want oldest on top here, and it's currently sorted newest on top
            editIds.Reverse();

            // Remove the first item (oldest) and add new item
            if (editIds.Count > LastProcessedIdCountToTrack)
            {
                editIds.RemoveAt(0);
            }

            editIds.Add(editId);

            var stateCollection = getState();

            var document = stateCollection.FindById(LastProcessedEditIdsKey);
            var bsonList = document[BsonValueField].AsArray;

            bsonList.Clear();

            foreach (var id in editIds)
            {
                bsonList.Add(id);
            }

            stateCollection.Update(document);
        }

        public bool DeltaCommentExists(string commentId)
        {
            return GetDeltaComment(commentId) != null;
        }

        public bool DeltaCommentExistsForParentCommentByAuthor(string parentCommentId, string authorName)
        {
            var deltaComment = _liteDatabase.GetCollection<DeltaComment>(DeltaCommentsCollectionName)
                .FindOne(dc => dc.ParentId == parentCommentId && dc.FromUsername == authorName);

            return deltaComment != null;
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

        public DeltaLogPostMapping GetDeltaLogPostMapping(string postId)
        {
            var deltaLogPostMappingsCollection = _liteDatabase.GetCollection<DeltaLogPostMapping>(DeltaLogPostMappingsCollectionName);
            return deltaLogPostMappingsCollection.Find(dc => dc.Id == postId).FirstOrDefault();
        }

        public void UpsertDeltaLogPostMapping(DeltaLogPostMapping mapping)
        {
            var deltaLogPostMappingsCollection = _liteDatabase.GetCollection<DeltaLogPostMapping>(DeltaLogPostMappingsCollectionName);
            deltaLogPostMappingsCollection.EnsureIndex(d => d.Id, true);
            deltaLogPostMappingsCollection.Upsert(mapping);
        }

        public List<string> GetIgnoreQuotedDeltaPMUserList()
        {
            var stateCollection = getState();

            var document = stateCollection.FindById(IgnoreQuotedDeltaPMUserListKey);
            var bsonList = document[BsonValueField].AsArray;

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

            var document = stateCollection.FindById(IgnoreQuotedDeltaPMUserListKey);
            var bsonList = document[BsonValueField].AsArray;

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
                stateCollection.Update(document);
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
                // If no LastActivity exists, create a new one and set the date to 10 minutes prior
                // so that it picks up anything during the transition
                var document = new BsonDocument();
                document[BsonIdField] = LastActivityTimeUtcKey;
                document[BsonValueField] = DateTime.UtcNow.AddMinutes(-10);
                stateCollection.Insert(document);
            }

            if (stateCollection.FindById(LastProcessedCommentIdsKey) == null)
            {
                var document = new BsonDocument();
                document[BsonIdField] = LastProcessedCommentIdsKey;
                document[BsonValueField] = new List<BsonValue>();
                stateCollection.Insert(document);
            }

            if (stateCollection.FindById(LastProcessedEditIdsKey) == null)
            {
                var document = new BsonDocument();
                document[BsonIdField] = LastProcessedEditIdsKey;
                document[BsonValueField] = new List<BsonValue>();
                stateCollection.Insert(document);
            }

            if (stateCollection.FindById(IgnoreQuotedDeltaPMUserListKey) == null)
            {
                var document = new BsonDocument();
                document[BsonIdField] = IgnoreQuotedDeltaPMUserListKey;
                document[BsonValueField] = new List<BsonValue>();
                stateCollection.Insert(document);
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
