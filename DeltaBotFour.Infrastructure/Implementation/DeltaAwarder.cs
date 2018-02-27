using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
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

        public void Award(DB4Thing comment)
        {
            string newFlairText = DeltaHelper.GetIncrementedFlairText(comment.ParentThing.AuthorFlairText);

            // Award to the parent comment
            _flairEditor.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass, newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryAward(comment);

            ConsoleHelper.WriteLine($"DeltaBot awarded a delta -> user: {comment.ParentThing.AuthorName}", ConsoleColor.Green);
        }

        public void Unaward(DB4Thing comment)
        {
            string newFlairText = DeltaHelper.GetDecrementedFlairText(comment.ParentThing.AuthorFlairText);

            // Unaward from the parent comment
            _flairEditor.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass, newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryUnaward(comment);

            ConsoleHelper.WriteLine($"DeltaBot unawarded a delta -> user: {comment.ParentThing.AuthorName}", ConsoleColor.Green);
        }
    }
}
