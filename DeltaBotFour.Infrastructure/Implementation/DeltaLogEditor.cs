using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaLogEditor : IDeltaLogEditor
    {
        private readonly IDB4Repository _repository;
        private readonly IPostBuilder _postBuilder;
        private readonly IRedditService _redditService;

        public DeltaLogEditor(IDB4Repository repository,
            IPostBuilder postBuilder,
            IRedditService redditService)
        {
            _repository = repository;
            _postBuilder = postBuilder;
            _redditService = redditService;
        }

        public void UpsertOrRemove(string postId)
        {
            var deltaComments = _repository.GetDeltaCommentsForPost(postId);
        }
    }
}
