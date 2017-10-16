using DeltaBotFour.ServiceInterfaces;
using RedditSharp;
using RedditSharp.Things;
using System;

namespace DeltaBotFour.ServiceImplementations
{
    public class IncomingCommentObserver : IObserver<VotableThing>
    {
        private Reddit _reddit;
        private ICommentDispatcher _commentDispatcher;

        public IncomingCommentObserver(Reddit reddit, ICommentDispatcher commentDispatcher)
        {
            _reddit = reddit;
            _commentDispatcher = commentDispatcher;
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(VotableThing votableThing)
        {
            var comment = votableThing as Comment;

            // We are only observing comments
            if (comment == null) { return; }

            _commentDispatcher.SendToQueue(comment);
        }
    }
}
