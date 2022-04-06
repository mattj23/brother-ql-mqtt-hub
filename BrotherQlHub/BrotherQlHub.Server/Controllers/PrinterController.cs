using BrotherQlHub.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace BrotherQlHub.Server.Controllers;

[Route("api/printer")]
[ApiController]
public class PrinterController : ControllerBase
{
    private readonly PrinterMonitor _printers;
    private readonly CategoryManager _categories;

    public PrinterController(PrinterMonitor printers, CategoryManager categories)
    {
        _printers = printers;
        _categories = categories;
    }

    public async Task<IActionResult> Index()
    {
        var output = new List<PrinterOutput>();
        var cat = await _categories.GetCategories();
        foreach (var p in _printers.GetPrinters())
        {
            var tags = new Dictionary<string, string>();
            foreach (var pair in p.Tags)
            {
                var category = cat.FirstOrDefault(c => c.Id == pair.Key);
                if (category is null) continue;

                tags[category.Name] = category.Tags.FirstOrDefault(t => t.Id == pair.Value)?.Name;
            }

            output.Add(new PrinterOutput
            {
                Handle = ToHandle(p.Serial),
                Serial = p.Serial,
                IsOnline = p.IsOnline,
                Model = p.Model,
                MediaType = p.MediaType,
                MediaWidth = p.MediaWidth,
                Errors = p.Errors,
                Tags = tags
            });

        }
        return Ok(output);
    }

    [HttpPost("{handle}/url")]
    public async Task<IActionResult> PrintUrl(string handle, string url)
    {
        var map = _printers.GetPrinters().ToDictionary(x => ToHandle(x.Serial), x => x.Serial);
        if (!map.ContainsKey(handle)) return BadRequest();
        
        await _printers.Print(map[handle], url);
        return Ok();
    }
        
    [HttpPost("{handle}/png")]
    public async Task<IActionResult> PrintUrl(string handle, IFormFile file)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var bytes = stream.ToArray();

        var map = _printers.GetPrinters().ToDictionary(x => ToHandle(x.Serial), x => x.Serial);
        if (!map.ContainsKey(handle)) return BadRequest();
        await _printers.Print(map[handle], bytes);
        return Ok();
    }

    private string ToHandle(string serial)
    {
        return serial.ToLower().Replace(" ", "-");
    }

    private class PrinterOutput
    {
        public string Handle { get; set; }
        public string Serial { get; set; }
        public bool IsOnline { get; set; }
        public string Model { get; set; }
        public string MediaType { get; set; }
        public int MediaWidth { get; set; }
        public int Errors { get; set; }

        public Dictionary<string, string> Tags { get; set; }
    }
}