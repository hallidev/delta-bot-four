using System;
using System.Collections.Generic;
using DeltaBotFour.Models;

namespace DeltaBotFour.Persistence.Interface
{
    public interface IDB4Repository
    {
        void Migrate();
        void MigrateLiteDbToSqlLite();
        DateTime GetLastActivityTimeUtc();
        void SetLastActivityTimeUtc();
        List<string> GetLastProcessedCommentIds();
        void SetLastProcessedCommentId(string commentId);
        List<string> GetLastProcessedEditIds();
        void SetLastProcessedEditId(string editId);
        int CleanOldDeltaComments();
        bool DeltaCommentExistsForParentCommentByAuthor(string parentCommentId, string authorName);
        void UpsertDeltaComment(DeltaComment commentWithDelta);
        void RemoveDeltaComment(string commentId);
        List<DeltaComment> GetDeltaCommentsForPost(string postId, string authorName = "");
        List<Deltaboard> GetCurrentDeltaboards();
        void AddDeltaboardEntry(string username);
        void RemoveDeltaboardEntry(string username);
        DeltaLogPostMapping GetDeltaLogPostMapping(string postId);
        void UpsertDeltaLogPostMapping(DeltaLogPostMapping mapping);
        List<string> GetIgnoreQuotedDeltaPMUserList();
        void AddIgnoredQuotedDeltaPMUser(string username);
        void UpsertWATTArticle(WATTArticle article);
        WATTArticle GetWattArticleForPost(string postId);
    }
}