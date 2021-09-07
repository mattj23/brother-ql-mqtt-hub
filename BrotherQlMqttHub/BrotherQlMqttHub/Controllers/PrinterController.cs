using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrotherQlMqttHub.Services;

namespace BrotherQlMqttHub.Controllers
{
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

        [HttpPost("print")]
        public async Task<IActionResult> PrintUrl(PrintRequest request)
        {
            await _printers.PrintRequest(request.Serial, request.Url);
            return Ok(request);
        }

        public class PrintRequest
        {
            public string Serial { get; set; }
            public string Url { get; set; }
        }

        private class PrinterOutput
        {
            public string Serial { get; set; }
            public bool IsOnline { get; set; }
            public string Model { get; set; }
            public string MediaType { get; set; }
            public int MediaWidth { get; set; }
            public int Errors { get; set; }

            public Dictionary<string, string> Tags { get; set; }
        }
    }
}
