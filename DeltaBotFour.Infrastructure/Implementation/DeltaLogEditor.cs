using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaLogEditor : IDeltaLogEditor
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _repository;
        private readonly IPostBuilder _postBuilder;
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;

        public DeltaLogEditor(AppConfiguration appConfiguration,
            IDB4Repository repository,
            IPostBuilder postBuilder,
            IRedditService redditService,
            ISubredditService subredditService)
        {
            _appConfiguration = appConfiguration;
            _repository = repository;
            _postBuilder = postBuilder;
            _redditService = redditService;
            _subredditService = subredditService;
        }

        public string Upsert(string mainSubPostId, string mainSubPostPermalink, string mainSubPostTitle, string opUsername)
        {
            // Get all of the delta comments for this post
            var deltaComments = _repository.GetDeltaCommentsForPost(mainSubPostId);

            // Build post title and text
            var postTitleAndText = _postBuilder.BuildDeltaLogPost(mainSubPostTitle, mainSubPostPermalink, opUsername, deltaComments);

            // Determine if a post already exists in DeltaLog
            var existingPostMapping = _repository.GetDeltaLogPostMapping(mainSubPostId);

            if (existingPostMapping == null)
            {
                return createNewPost(postTitleAndText.Item1, postTitleAndText.Item2, mainSubPostId, mainSubPostPermalink);
            }

            try
            {
                // There is an existing mapping - edit post
                _redditService.EditPost(existingPostMapping.DeltaLogPostUrl, postTitleAndText.Item2);
            }
            catch (Exception ex)
            {
                // This will fail with a forbidden if the post was manually deleted.
                // It should never really happen, but this is handy for my testing
                // and won't really hurt in production
                if (ex.ToString().Contains("403 (Forbidden)"))
                {
                    return createNewPost(postTitleAndText.Item1, postTitleAndText.Item2, mainSubPostId, mainSubPostPermalink);
                }

                throw;
            }

            return existingPostMapping.DeltaLogPostUrl;
        }

        private string createNewPost(string title, string text, string mainSubPostId, string mainSubPostPermalink)
        {
            // Make new post to the DeltaLog sub
            var newPost = _subredditService.Post(title, text, _appConfiguration.DeltaLogSubredditName);

            // Add new mapping
            var newPostMapping = new DeltaLogPostMapping
            {
                Id = mainSubPostId,
                MainSubPostUrl = mainSubPostPermalink,
                DeltaLogPostId = newPost.Id,
                DeltaLogPostUrl = newPost.Permalink
            };

            _repository.UpsertDeltaLogPostMapping(newPostMapping);

            return newPost.Permalink;
        }
    }
}
