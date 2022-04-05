namespace BrotherQlHub.Data;

public interface IPrinterTransport
{
    IObservable<PrinterUpdate> Updates { get; }

    Task<bool> Print(string serial, string imageUrl);
    Task<bool> Print(string serial, byte[] pngData);
}