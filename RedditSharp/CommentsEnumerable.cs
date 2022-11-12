using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedditSharp.Things;

namespace RedditSharp
{
    /// <summary>
    ///     <see cref="IAsyncEnumerator{T}" /> for enumarting all comments on a post.
    ///     Will traverse <see cref="More" /> objects it encounters.
    /// </summary>
    public class CommentsEnumarable : IAsyncEnumerable<Comment>
    {
        private readonly IWebAgent agent;
        private readonly int limit;
        private readonly Post post;

        /// <summary>
        ///     Constructs an <see cref="IAsyncEnumerable{T}" /> for the <see cref="Comment" />(s) on the <paramref name="post" />.
        ///     This will result in multiple requests for larger comment trees as it will resolve all <see cref="More" /> objects
        ///     it encounters.
        /// </summary>
        /// <param name="agent"> WebAgent necessary for requests</param>
        /// <param name="post">The <see cref="Post" /> of the comments section to enumerate</param>
        /// <param name="limitPerRequest">Initial request size, ignored by the MoreChildren endpoint</param>
        public CommentsEnumarable(IWebAgent agent, Post post, int limitPerRequest = 0)
        {
            this.post = post;
            this.agent = agent;
            limit = limitPerRequest;
        }

        public IAsyncEnumerator<Comment> GetAsyncEnumerator(
            CancellationToken cancellationToken = new CancellationToken())
        {
            return new CommentsEnumerator(agent, post, limit);
        }

        /// <summary>
        ///     Returns <see cref="IAsyncEnumerator{T}" /> for the comments on the <see cref="Post" />>
        /// </summary>
        /// <returns></returns>
        public IAsyncEnumerator<Comment> GetEnumerator()
        {
            return new CommentsEnumerator(agent, post, limit);
        }

        private class CommentsEnumerator : IAsyncEnumerator<Comment>
        {
            private const string GetCommentsUrl = "/comments/{0}.json";
            private readonly IWebAgent agent;
            private IReadOnlyList<Comment> currentBranch;
            private int currentIndex;
            private readonly List<More> existingMores;
            private readonly int limit;

            private readonly Post post;

            public CommentsEnumerator(IWebAgent agent, Post post, int limitPerRequest = 0)
            {
                existingMores = new List<More>();
                currentIndex = -1;
                this.post = post;
                this.agent = agent;
                limit = limitPerRequest;
            }

            public Comment Current => currentBranch[currentIndex];

            public async ValueTask<bool> MoveNextAsync()
            {
                if (currentIndex == -1)
                {
                    currentIndex = 0;
                    await GetBaseComments();
                    return currentBranch.Count > 0;
                }

                currentIndex++;
                if (currentIndex >= currentBranch.Count)
                {
                    if (existingMores.Count == 0)
                    {
                        return false;
                    }

                    currentIndex = 0;
                    while (existingMores.Count > 0)
                    {
                        var more = existingMores.First();
                        existingMores.Remove(more);
                        var newBranch = new List<Comment>();
                        var newThings = await more.GetThingsAsync();
                        foreach (var thing in newThings)
                            if (thing.Kind == "more")
                                existingMores.Add((More) thing);
                            else
                                newBranch.Add((Comment) thing);
                        currentBranch = newBranch;
                        if (currentBranch.Count > 0) return true;
                    }

                    return false; //ran out of branches to check
                }

                return true;
            }

            public async ValueTask DisposeAsync()
            {
            }

            private async Task GetBaseComments()
            {
                var url = string.Format(GetCommentsUrl, post.Id);
                if (limit > 0)
                {
                    var query = "limit=" + limit;
                    url = string.Format("{0}?{1}", url, query);
                }

                var json = await agent.Get(url).ConfigureAwait(false);
                var postJson = json.Last()["data"]["children"];

                var retrieved = new List<Comment>();
                foreach (var item in postJson)
                {
                    var newComment = new Comment(agent, item, post);
                    if (newComment.Kind != "more")
                        retrieved.Add(newComment);
                    else
                        existingMores.Add(new More(agent, item));
                }

                currentBranch = retrieved;
            }
        }
    }
}