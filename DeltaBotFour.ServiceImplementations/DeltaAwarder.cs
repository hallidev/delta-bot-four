using Core.Foundation.Helpers;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System;

namespace DeltaBotFour.ServiceImplementations
{
    public class DeltaAwarder : IDeltaAwarder
    {
        private readonly IUserWikiEditor _wikiEditor;
        private readonly Subreddit _subreddit;

        public DeltaAwarder(IUserWikiEditor wikiEditor, Subreddit subreddit)
        {
            _wikiEditor = wikiEditor;
            _subreddit = subreddit;
        }

        public void Award(Comment comment, Comment parentComment)
        {
            string newFlairText = DeltaHelper.GetIncrementedFlairText(parentComment.AuthorFlairText);

            // Award to the parent comment
            // TODO:uncomment
            //_subreddit.SetUserFlairAsync(parentComment.AuthorName, parentComment.AuthorFlairCssClass, newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryAward(comment, parentComment);

            ConsoleHelper.WriteLine($"DeltaBot awarded a delta -> user: {parentComment.AuthorName}", ConsoleColor.Green);
        }

        public void Unaward(Comment comment, Comment parentComment)
        {
            string newFlairText = DeltaHelper.GetDecrementedFlairText(parentComment.AuthorFlairText);

            // Unaward from the parent comment
            _subreddit.SetUserFlairAsync(parentComment.AuthorName, parentComment.AuthorFlairCssClass, newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryUnaward(comment, parentComment);

            ConsoleHelper.WriteLine($"DeltaBot unawarded a delta -> user: {parentComment.AuthorName}", ConsoleColor.Green);
        }
    }
}
