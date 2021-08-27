using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BrotherQlMqttHub.Data
{
    public class TagCategory
    {
        [Key]
        public string Id { get; set; }

        public string ShortDescription { get; set; }

        public string Description { get; set; }

        public List<Tag> Tags { get; set; }
        
    }
}