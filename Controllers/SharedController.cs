using Elanco___Tick_Sighting.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Elanco___Tick_Sighting.Controllers
{
    public class SharedController : Controller
    {
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}