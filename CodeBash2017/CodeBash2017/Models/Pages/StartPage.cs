using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace CodeBash2017.Models.Pages
{
    [ContentType(DisplayName = "StartPage", GUID = "b8fe8485-587d-4880-b485-a52430ea55de", Description = "Landing page for the website.")]
    public class StartPage : PageData
    {
    }
}