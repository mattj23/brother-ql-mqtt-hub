using System.Collections.Generic;

namespace BrotherQlMqttHub.Data
{
    public class HostMessage
    {
        public bool Online { get; set; }
        public string Ip { get; set; }
        public string Host { get; set; }
        public int UpdateS { get; set; }

        public List<PrinterInfo> Printers { get; set; }
    }
}