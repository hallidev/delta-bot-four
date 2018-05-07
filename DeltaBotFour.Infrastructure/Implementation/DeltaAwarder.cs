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
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;
        private readonly IDeltaboardEditor _deltaboardEditor;

        public DeltaAwarder(AppConfiguration appConfiguration,
            IUserWikiEditor wikiEditor,
            IRedditService redditService,
            ISubredditService subredditService, 
            IDeltaboardEditor deltaboardEditor)
        {
            _appConfiguration = appConfiguration;
            _wikiEditor = wikiEditor;
            _redditService = redditService;
            _subredditService = subredditService;
            _deltaboardEditor = deltaboardEditor;
        }

        public void Award(DB4Thing comment)
        {
            if (_appConfiguration.DB4Modes.Contains(DB4Mode.DeltaMonitor))
            {
                // Get the user's current delta count from flair
                int currentDeltaCount = DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText);

                // Get new flair with incremented delta count
                string newFlairText = DeltaHelper.GetIncrementedFlairText(comment.ParentThing.AuthorFlairText);

                // Award to the parent comment
                _subredditService.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                    newFlairText);

                // Update wiki
                _wikiEditor.UpdateUserWikiEntryAward(comment);

                // If this was the user's first delta, send the first delta PM
                if (currentDeltaCount == 0)
                {
                    string subject = _appConfiguration.PrivateMessages.FirstDeltaSubject;
                    string body = _appConfiguration.PrivateMessages.FirstDeltaMessage
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.UsernameToken, comment.ParentThing.AuthorName);

                    _redditService.SendPrivateMessage(subject, body, comment.ParentThing.AuthorName);
                }
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
                // Get the user's current delta count from flair
                int currentDeltaCount = DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText);

                string newFlairText = string.Empty;

                // If we are removing the user's only delta, we don't want the text to read "0∆"
                if (currentDeltaCount != 1)
                {
                    newFlairText = DeltaHelper.GetDecrementedFlairText(comment.ParentThing.AuthorFlairText);
                }

                // Unaward from the parent comment
                _subredditService.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
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
