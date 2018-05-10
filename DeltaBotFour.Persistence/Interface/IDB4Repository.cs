using System;
using System.Collections.Generic;
using DeltaBotFour.Models;

namespace DeltaBotFour.Persistence.Interface
{
    public interface IDB4Repository
    {
        DateTime GetLastActivityTimeUtc();
        void SetLastActivityTimeUtc();
        bool DeltaCommentExists(string commentId);
        DeltaComment GetDeltaComment(string commentId);
        void UpsertDeltaComment(DeltaComment commentWithDelta);
        void RemoveDeltaComment(string commentId);
        List<DeltaComment> GetDeltaCommentsForPost(string postId, string authorName = "");
        List<Deltaboard> GetCurrentDeltaboards();
        void AddDeltaboardEntry(string username);
        void RemoveDeltaboardEntry(string username);
        DeltaLogPostMapping GetDeltaLogPostMapping(string postId);
        void AddDeltaLogPostMapping(DeltaLogPostMapping mapping);
        List<string> GetIgnoreQuotedDeltaPMUserList();
        void AddIgnoredQuotedDeltaPMUser(string username);
        void UpsertWATTArticle(WATTArticle article);
        WATTArticle GetWattArticleForPost(string postId);
    }
}
