using System;

namespace DeltaBotFour.Models
{
    public class WATTArticle
    {
        public Guid Id { get; set; }
        public string RedditPostId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
