using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaAwarder : IDeltaAwarder
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IUserWikiEditor _wikiEditor;
        private readonly IFlairEditor _flairEditor;
        private readonly IDeltaboardEditor _deltaboardEditor;

        public DeltaAwarder(AppConfiguration appConfiguration,
            IUserWikiEditor wikiEditor, 
            IFlairEditor flairEditor, 
            IDeltaboardEditor deltaboardEditor)
        {
            _appConfiguration = appConfiguration;
            _wikiEditor = wikiEditor;
            _flairEditor = flairEditor;
            _deltaboardEditor = deltaboardEditor;
        }

        public void Award(DB4Thing comment)
        {
            if (_appConfiguration.DB4Modes.Contains(DB4Mode.DeltaMonitor))
            {
                string newFlairText = DeltaHelper.GetIncrementedFlairText(comment.ParentThing.AuthorFlairText);

                // Award to the parent comment
                _flairEditor.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                    newFlairText);

                // Update wiki
                _wikiEditor.UpdateUserWikiEntryAward(comment);
            }

            if (_appConfiguration.DB4Modes.Contains(DB4Mode.Deltaboard))
            {
                // Update deltaboards
                _deltaboardEditor.AddDelta(comment.ParentThing.AuthorName);
            }

            ConsoleHelper.WriteLine($"DeltaBot awarded a delta -> user: {comment.ParentThing.AuthorName}", ConsoleColor.Green);
        }

        public void Unaward(DB4Thing comment)
        {
            if (_appConfiguration.DB4Modes.Contains(DB4Mode.DeltaMonitor))
            {
                string newFlairText = DeltaHelper.GetDecrementedFlairText(comment.ParentThing.AuthorFlairText);

                // Unaward from the parent comment
                _flairEditor.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                    newFlairText);

                // Update wiki
                _wikiEditor.UpdateUserWikiEntryUnaward(comment);
            }

            if (_appConfiguration.DB4Modes.Contains(DB4Mode.Deltaboard))
            {
                // Update deltaboards
                _deltaboardEditor.RemoveDelta(comment.ParentThing.AuthorName);
            }

            ConsoleHelper.WriteLine($"DeltaBot unawarded a delta -> user: {comment.ParentThing.AuthorName}", ConsoleColor.Green);
        }
    }
}
