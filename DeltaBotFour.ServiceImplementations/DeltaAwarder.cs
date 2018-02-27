using Core.Foundation.Helpers;
using DeltaBotFour.ServiceInterfaces;
using System;
using DeltaBotFour.ServiceInterfaces.RedditServices;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceImplementations
{
    public class DeltaAwarder : IDeltaAwarder
    {
        private readonly IUserWikiEditor _wikiEditor;
        private readonly IFlairEditor _flairEditor;

        public DeltaAwarder(IUserWikiEditor wikiEditor, IFlairEditor flairEditor)
        {
            _wikiEditor = wikiEditor;
            _flairEditor = flairEditor;
        }

        public void Award(Comment comment, Comment parentComment)
        {
            string newFlairText = DeltaHelper.GetIncrementedFlairText(parentComment.AuthorFlairText);

            // Award to the parent comment
            _flairEditor.SetUserFlair(parentComment.AuthorName, parentComment.AuthorFlairCssClass, newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryAward(comment, parentComment);

            ConsoleHelper.WriteLine($"DeltaBot awarded a delta -> user: {parentComment.AuthorName}", ConsoleColor.Green);
        }

        public void Unaward(Comment comment, Comment parentComment)
        {
            string newFlairText = DeltaHelper.GetDecrementedFlairText(parentComment.AuthorFlairText);

            // Unaward from the parent comment
            _flairEditor.SetUserFlair(parentComment.AuthorName, parentComment.AuthorFlairCssClass, newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryUnaward(comment, parentComment);

            ConsoleHelper.WriteLine($"DeltaBot unawarded a delta -> user: {parentComment.AuthorName}", ConsoleColor.Green);
        }
    }
}
