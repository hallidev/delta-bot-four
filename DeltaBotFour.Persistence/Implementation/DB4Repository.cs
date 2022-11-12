using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using Core.Foundation.Exceptions;
using Core.Foundation.Extensions;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using LiteDB;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DeltaBotFour.Persistence.Implementation
{
    public class DB4Repository : IDB4Repository
    {
        private const int DeltaCommentRetainDays = 365;
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
            _liteDatabase = new LiteDatabase($"Filename={DbFileName}");
        }

        public void Migrate()
        {
            DeltaBotFourDbContext dbContext = new DeltaBotFourDbContext();
            dbContext.Database.Migrate();
        }

        public void MigrateLiteDbToSqlLite()
        {
            Console.WriteLine();
            var dbContext = new DeltaBotFourDbContext();

            // DeltaComments
            var sqlDeltaCommentCount = dbContext
                .DeltaComments
                .Count();

            if (sqlDeltaCommentCount == 0)
            {
                Console.WriteLine("Migrating Comments");
                var deltaComments = _liteDatabase.GetCollection<DeltaComment>(DeltaCommentsCollectionName);

                var comments = deltaComments
                    .FindAll()
                    .ToList();

                dbContext
                    .AddRange(comments);

                dbContext.SaveChanges();
            }

            // PostMappings
            var sqlDeltaLogPostMappingCount = dbContext
                .DeltaLogPostMappings
                .Count();

            if (sqlDeltaLogPostMappingCount == 0)
            {
                Console.WriteLine("Migrating PostMappings");
                var postMappings = _liteDatabase.GetCollection<DeltaLogPostMapping>(DeltaLogPostMappingsCollectionName);

                var mappings = postMappings
                    .FindAll()
                    .ToList();

                dbContext
                    .AddRange(mappings);

                dbContext.SaveChanges();
            }

            // Deltaboards
            var sqlDeltaboardsCount = dbContext
                .Deltaboards
                .Count();

            if (sqlDeltaboardsCount == 0)
            {
                Console.WriteLine("Migrating Deltaboards");
                var deltaboards = _liteDatabase.GetCollection<Deltaboard>(DeltaboardsCollectionName);

                var boards = deltaboards
                    .FindAll()
                    .Where(e => !e.Id.Contains("5:00"))
                    .Select(e =>
                    {
                        e.Id = e.Id.Replace(" 12:00:00 AM", string.Empty);
                        return e;
                    })
                    .ToList();

                foreach (var board in boards)
                {
                    foreach (var entry in board.Entries)
                    {
                        entry.Id = Guid.NewGuid();
                        entry.DeltaboardId = board.Id;
                    }
                }

                dbContext
                    .AddRange(boards);

                dbContext.SaveChanges();
            }

            Console.WriteLine("Done");
        }

        public DateTime GetLastActivityTimeUtc()
        {
            var state = getState();
            return state.LastActivityTimeUtcKey.DateTime;
        }

        public void SetLastActivityTimeUtc()
        {
            var state = getState();

            var dbContext = new DeltaBotFourDbContext();
            dbContext.Attach(state);

            state.LastActivityTimeUtcKey = DateTimeOffset.UtcNow;

            dbContext.SaveChangesAsync();
        }

        public List<string> GetLastProcessedCommentIds()
        {
            var state = getState();

            var commentIds = JsonConvert.DeserializeObject<List<string>>(state.LastProcessedCommentIds);

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

            var state = getState();

            var dbContext = new DeltaBotFourDbContext();
            dbContext.Attach(state);

            state.LastProcessedCommentIds = JsonConvert.SerializeObject(commentIds);

            dbContext.SaveChanges();
        }

        public List<string> GetLastProcessedEditIds()
        {
            var state = getState();

            var editIds = JsonConvert.DeserializeObject<List<string>>(state.LastProcessedEditIds);

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

            var state = getState();

            var dbContext = new DeltaBotFourDbContext();
            dbContext.Attach(state);

            state.LastProcessedEditIds = JsonConvert.SerializeObject(editIds);

            dbContext.SaveChanges();
        }

        public int CleanOldDeltaComments()
        {
            var dbContext = new DeltaBotFourDbContext();

            var oldComments = dbContext
                .DeltaComments
                .ToList()
                .Where(e => (DateTime.Now - e.CreatedUtc).TotalDays > DeltaCommentRetainDays)
                .ToList();

            dbContext.RemoveRange(oldComments);
            dbContext.SaveChanges();

            return oldComments.Count;
        }

        public bool DeltaCommentExistsForParentCommentByAuthor(string parentCommentId, string authorName)
        {
            var dbContext = new DeltaBotFourDbContext();

            var exists = dbContext
                .DeltaComments
                .Any(dc => dc.ParentId == parentCommentId && dc.FromUsername == authorName);

            return exists;
        }

        public void UpsertDeltaComment(DeltaComment commentWithDelta)
        {
            var dbContext = new DeltaBotFourDbContext();

            dbContext
                .DeltaComments
                .Where(e => e.Id == commentWithDelta.Id)
                .ExecuteDelete();

            dbContext
                .Add(commentWithDelta);

            dbContext.SaveChanges();
        }

        public void RemoveDeltaComment(string commentId)
        {
            var dbContext = new DeltaBotFourDbContext();

            var comment = dbContext
                .DeltaComments
                .SingleOrDefault(e => e.Id == commentId);

            if (comment != null)
            {
                dbContext
                    .Remove(comment);

                dbContext.SaveChanges();
            }
        }

        public List<DeltaComment> GetDeltaCommentsForPost(string postId, string authorName = "")
        {
            var dbContext = new DeltaBotFourDbContext();

            var comments = dbContext
                .DeltaComments
                .Where(e => e.ParentPostId == postId &&
                            (string.IsNullOrEmpty(authorName) || e.FromUsername == authorName))
                .ToList();

            return comments;
        }

        public List<Deltaboard> GetCurrentDeltaboards()
        {
            var deltaboards = new List<Deltaboard>();

            // Get deltaboards - will be created if they don't exist
            foreach (var deltaboardType in Enum.GetValues(typeof(DeltaboardType)))
            {
                deltaboards.Add(getCurrentDeltaboard((DeltaboardType) deltaboardType));
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
            var dbContext = new DeltaBotFourDbContext();

            var mapping = dbContext
                .DeltaLogPostMappings
                .SingleOrDefault(e => e.Id == postId);

            return mapping;
        }

        public void UpsertDeltaLogPostMapping(DeltaLogPostMapping mapping)
        {
            var dbContext = new DeltaBotFourDbContext();

            dbContext
                .DeltaLogPostMappings
                .Where(e => e.Id == mapping.Id)
                .ExecuteDelete();

            dbContext
                .Add(mapping);

            dbContext.SaveChanges();
        }

        public List<string> GetIgnoreQuotedDeltaPMUserList()
        {
            var state = getState();

            var list = JsonConvert.DeserializeObject<List<string>>(state.IgnoreQuotedDeltaPMUserList);

            return list;
        }

        public void AddIgnoredQuotedDeltaPMUser(string username)
        {
            var userList = GetIgnoreQuotedDeltaPMUserList();

            if (!userList.Contains(username))
            {
                userList.Add(username);
            }

            var state = getState();

            var dbContext = new DeltaBotFourDbContext();
            dbContext.Attach(state);

            state.IgnoreQuotedDeltaPMUserList = JsonConvert.SerializeObject(userList);

            dbContext.SaveChanges();
        }

        public void UpsertWATTArticle(WATTArticle article)
        {
            //var wattArticlesCollection = _liteDatabase.GetCollection<WATTArticle>(WATTArticlesCollectionName);
            //wattArticlesCollection.EnsureIndex(d => d.Id, true);
            //wattArticlesCollection.Upsert(article);
        }

        public WATTArticle GetWattArticleForPost(string postId)
        {
            //var wattArticlesCollection = _liteDatabase.GetCollection<WATTArticle>(WATTArticlesCollectionName);
            //return wattArticlesCollection.Find(dc => dc.RedditPostId == postId).FirstOrDefault();
            return null;
        }

        private DB4State getState()
        {
            var dbContext = new DeltaBotFourDbContext();

            var db4State = dbContext
                .Db4States
                .SingleOrDefault();

            if (db4State == null)
            {
                // Initialize default state
                db4State = new DB4State
                {
                    Id = Guid.NewGuid(),
                    LastActivityTimeUtcKey = DateTimeOffset.UtcNow,
                    LastProcessedCommentIds = JsonConvert.SerializeObject(new List<string>()),
                    LastProcessedEditIds = JsonConvert.SerializeObject(new List<string>()),
                    IgnoreQuotedDeltaPMUserList = JsonConvert.SerializeObject(new List<string>())
                };

                dbContext.Add(db4State);
                dbContext.SaveChanges();
            }

            return db4State;
        }

        private Deltaboard getCurrentDeltaboard(DeltaboardType type)
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
            var dbContext = new DeltaBotFourDbContext();

            string id = $"{type}-{startUtc.ToShortDateString()}";

            var deltaboard = dbContext
                .Deltaboards
                .Include(d => d.Entries)
                .SingleOrDefault(e => e.Id == id);

            // Create if it doesn't exist
            if (deltaboard == null)
            {
                deltaboard = createDeltaboard(id, type, startUtc);

                dbContext.Add(deltaboard);
                dbContext.SaveChanges();
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
            var dbContext = new DeltaBotFourDbContext();

            dbContext
                .Deltaboards
                .Where(e => deltaboards.Select(x => x.Id).Contains(e.Id))
                .ExecuteDelete();

            dbContext
                .AddRange(deltaboards);

            dbContext.SaveChanges();
        }
    }
}