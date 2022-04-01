using System.ComponentModel.DataAnnotations;

namespace BrotherQlHub.Data
{
    public class Printer
    {
        [Key]
        public string Serial { get; set; }

        public string Model { get; set; } 

        public DateTime? LastSeen { get; set; }

        public List<PrinterTag> Tags { get; set; }
        
    }
}