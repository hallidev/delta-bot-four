using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentProcessor : ICommentProcessor
    {
        private Subreddit _subreddit;

        public CommentProcessor(Subreddit subreddit)
        {
            _subreddit = subreddit;
        }

        public void Process(QueueMessage<DB4Comment> comment)
        {
            throw new System.NotImplementedException();
        }
    }
}
