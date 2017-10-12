namespace DeltaBotFour.Models
{
    public class DB4Comment
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string LinkTitle { get; set; }
        public string AuthorName { get; set; }
        public string ParentAuthorName { get; set; }
        public string ShortLink { get; set; }
        public bool Edited { get; set; }
        public string Body { get; set; }
    }
}
