using Microsoft.AspNetCore.Mvc;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.IO;
using System.Text;

namespace EventZax.Controllers
{
    [Route("api/pdf")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly IConverter _converter;
        public PdfController(IConverter converter) { _converter = converter; }

        [HttpPost("generate")]
        public IActionResult GeneratePdf([FromBody] string html)
        {
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = { PaperSize = PaperKind.A4 },
                Objects = { new ObjectSettings { HtmlContent = html } }
            };
            var pdf = _converter.Convert(doc);
            return File(pdf, "application/pdf", "ticket.pdf");
        }
    }
}