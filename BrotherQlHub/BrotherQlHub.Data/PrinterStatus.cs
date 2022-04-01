namespace BrotherQlHub.Data
{
    public class PrinterStatus
    {
        public int MediaWidth { get; set; }
        public int MediaLength { get; set; }
        public string MediaType { get; set; }
        public int Errors { get; set; }
        public string StatusType { get; set; }
        public string Phase { get; set; }
        public int Notification { get; set; }
    }
}