using Core.Foundation.Helpers;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System;

namespace DeltaBotFour.ServiceImplementations
{
    public class DeltaAwarder : IDeltaAwarder
    {
        private readonly IWikiEditor _wikiEditor;
        private readonly Subreddit _subreddit;

        public DeltaAwarder(IWikiEditor wikiEditor, Subreddit subreddit)
        {
            _wikiEditor = wikiEditor;
            _subreddit = subreddit;
        }

        public async void Award(Comment comment, Comment parentComment)
        {
            string newFlairText = DeltaHelper.GetIncrementedFlairText(parentComment.AuthorFlairText);

            // Award to the parent comment
            await _subreddit.SetUserFlairAsync(parentComment.AuthorName, parentComment.AuthorFlairCssClass, newFlairText);

            ConsoleHelper.WriteLine($"DeltaBot awarded a delta -> user: {parentComment.AuthorName}", ConsoleColor.Green);
        }

        public async void Unaward(Comment comment, Comment parentComment)
        {
            string newFlairText = DeltaHelper.GetDecrementedFlairText(parentComment.AuthorFlairText);

            // Unaward to the parent comment
            await _subreddit.SetUserFlairAsync(parentComment.AuthorName, parentComment.AuthorFlairCssClass, newFlairText);

            ConsoleHelper.WriteLine($"DeltaBot unawarded a delta -> user: {parentComment.AuthorName}", ConsoleColor.Green);
        }
    }
}
