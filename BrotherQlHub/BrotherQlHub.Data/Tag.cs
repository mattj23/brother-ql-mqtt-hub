using System.ComponentModel.DataAnnotations;

namespace BrotherQlHub.Data
{
    public class Tag 
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public TagCategory Category { get; set; }
    }
}