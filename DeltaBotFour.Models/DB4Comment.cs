using System.Collections.Generic;

namespace DeltaBotFour.Models
{
    public class DB4Comment
    {
        public string ParentId { get; set; }
        public string ShortLink { get; set; }
        public string Body { get; set; }
        public bool IsEdited { get; set; }
        public List<DB4Comment> Comments { get; set; }
    }
}
