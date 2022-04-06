namespace BrotherQlHub.Data;

public interface IPrinterTransport
{
    IObservable<PrinterUpdate> Updates { get; }

    Task Print(string serial, string imageUrl);
    Task Print(string serial, byte[] pngData);
}