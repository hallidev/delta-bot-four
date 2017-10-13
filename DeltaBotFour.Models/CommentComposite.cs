using RedditSharp.Things;
using System.Collections.Generic;

namespace DeltaBotFour.Models
{
    public class CommentComposite
    {
        public Comment ParentComment { get; set; }
        public Comment Comment { get; set; }
        public List<Comment> ChildComments { get; set; }
    }
}
