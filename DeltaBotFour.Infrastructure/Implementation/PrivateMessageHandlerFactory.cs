using System;
using System.Linq;
using DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageHandlerFactory : IPrivateMessageHandlerFactory
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;
        private readonly ICommentProcessor _commentProcessor;
        private readonly ICommentDetector _commentDetector;
        private readonly ICommentBuilder _commentBuilder;
        private readonly ICommentReplier _replier;
        private readonly IDeltaAwarder _deltaAwarder;
        private readonly IStickyCommentEditor _stickyCommentEditor;

        public PrivateMessageHandlerFactory(AppConfiguration appConfiguration,
            IDB4Repository db4Repository,
            IRedditService redditService,
            ISubredditService subredditService,
            ICommentProcessor commentProcessor,
            ICommentDetector commentDetector,
            ICommentBuilder commentBuilder,
            ICommentReplier replier,
            IDeltaAwarder deltaAwarder,
            IStickyCommentEditor stickyCommentEditor)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
            _redditService = redditService;
            _subredditService = subredditService;
            _commentProcessor = commentProcessor;
            _commentDetector = commentDetector;
            _commentBuilder = commentBuilder;
            _replier = replier;
            _deltaAwarder = deltaAwarder;
            _stickyCommentEditor = stickyCommentEditor;
        }

        public IPrivateMessageHandler Create(DB4Thing privateMessage)
        {
            // Some private messages don't have an author. All handlers here
            // require an author to process.
            if (string.IsNullOrEmpty(privateMessage.AuthorName))
            {
                return null;
            }

            var privateMessageParser = new PrivateMessageParser(privateMessage);
            var parseResult = privateMessageParser.Parse();

            var isMod = _subredditService.IsUserModerator(privateMessage.AuthorName);

            // Mod commands
            if (isMod)
            {
                // Force Add Delta (moderator only)
                if (string.Equals(parseResult.Command, _appConfiguration.PrivateMessages.ModForceAddDeltaSubject,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    return new ModForceAddDeltaPMHandler(_appConfiguration, _redditService, _commentDetector,
                        _commentBuilder, _replier, _deltaAwarder);
                }

                // Remove delta (moderator only)
                if (string.Equals(parseResult.Command, _appConfiguration.PrivateMessages.ModDeleteDeltaSubject,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    return new ModDeleteDeltaPMHandler(_appConfiguration, _redditService, _commentDetector,
                        _commentBuilder,
                        _replier, _deltaAwarder, _db4Repository);
                }
            }

            // Add Delta
            if (string.Equals(parseResult.Command, _appConfiguration.PrivateMessages.ModAddDeltaSubject,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                return new AddDeltaPMHandler(_commentProcessor, _redditService);
            }

            // Stop quoted deltas warning
            if (privateMessage.Subject.ToLower()
                    .Contains(_appConfiguration.PrivateMessages.DeltaInQuoteSubject.ToLower())
                || (parseResult.IsDirectChat && privateMessage.Body.ToLower()
                    .Contains("!stop")))
            {
                return new StopQuotedDeltaWarningsPMHandler(_appConfiguration, _db4Repository, _redditService);
            }

            // WATT Article created (author must be in ValidWATTUsers list)
            if (_appConfiguration.ValidWATTUsers.Any(u =>
                    string.Equals(u, privateMessage.AuthorName, StringComparison.CurrentCultureIgnoreCase)) &&
                string.Equals(privateMessage.Subject, _appConfiguration.PrivateMessages.WATTArticleCreatedSubject,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                return new WATTArticleCreatedPMHandler(_db4Repository, _redditService, _stickyCommentEditor);
            }

            return null;
        }
    }
}