using System;

namespace DeltaBotFour.Models
{
    public class DeltaComment
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsEdited { get; set; }
        public string FromUsername { get; set; }
        public string ToUsername { get; set; }
        public string CommentText { get; set; }
        public string LinkId { get; set; }
        public string Permalink { get; set; }
        public string Shortlink { get; set; }
        public string ParentPostId { get; set; }
        public string ParentPostLinkId { get; set; }
        public string ParentPostPermalink { get; set; }
        public string ParentPostShortlink { get; set; }
        public string ParentPostTitle { get; set; }
    }
}
