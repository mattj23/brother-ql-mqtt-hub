using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrotherQlHub.Data
{
    public class PrinterTag
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Printer))]
        public string PrinterSerial { get; set; }

        [ForeignKey(nameof(TagCategoryId))]
        public int TagCategoryId { get; set; }

        [ForeignKey(nameof(Tag))]
        public int TagId { get; set; }
        
    }
}