using System;
using System.Collections.Generic;

namespace DeltaBotFour.Models
{
    public class DB4Thing
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public DB4ThingType Type { get; set; }
        public string AuthorName { get; set; }
        public string AuthorFlairText { get; set; }
        public string AuthorFlairCssClass { get; set; }
        public string Body { get; set; }
        public DateTime Created { get; set; }
        public DateTime CreatedUTC { get; set; }
        public bool IsEdited { get; set; }
        public string Permalink { get; set; }
        public string Shortlink { get; set; }
        public string Title { get; set; }
        public DB4Thing ParentPost { get; set; }
        public DB4Thing ParentThing { get; set; }
        public List<DB4Thing> Comments { get; set; }
    }
}
