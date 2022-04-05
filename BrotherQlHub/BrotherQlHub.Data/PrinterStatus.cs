namespace BrotherQlHub.Data;

public record PrinterStatus(int MediaWidth, int MediaLength, string MediaType, int Errors, string StatusType,
    string Phase, int Notification);

public record PrinterInfo(string Model, string Serial, PrinterStatus Status);

public record PrinterUpdate(PrinterInfo Info, IPrinterTransport? Transport, string Host, string Ip);

