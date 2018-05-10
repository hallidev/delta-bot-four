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

        public string Upsert(string postId, string postPermalink)
        {
            // Get all of the delta comments for this post
            var deltaComments = _repository.GetDeltaCommentsForPost(postId);

            // Build post title and text
            var postTitleAndText = _postBuilder.BuildDeltaLogPost(deltaComments);

            // Determine if a post already exists in DeltaLog
            var existingPostMapping = _repository.GetDeltaLogPostMapping(postId);

            if (existingPostMapping == null)
            {
                // Make new post to the DeltaLog sub
                var newPost = _subredditService.Post(postTitleAndText.Item1, postTitleAndText.Item2, _appConfiguration.DeltaLogSubredditName);

                // Add new mapping
                var newPostMapping = new DeltaLogPostMapping
                {
                    Id = postId,
                    MainSubPostUrl = postPermalink,
                    DeltaLogPostId = newPost.Id,
                    DeltaLogPostUrl = newPost.Permalink
                };

                _repository.AddDeltaLogPostMapping(newPostMapping);

                return newPost.Permalink;
            }
            else
            {
                // There is an existing mapping - edit post
                _redditService.EditPost(existingPostMapping.DeltaLogPostUrl, postTitleAndText.Item2);

                return existingPostMapping.DeltaLogPostUrl;
            }
        }
    }
}
