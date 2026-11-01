using Elanco___Tick_Sighting.Data;
using Elanco___Tick_Sighting.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Elanco___Tick_Sighting.Services;

namespace Elanco___Tick_Sighting.Controllers
{
    public class SightingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly SightingService _sightings;

        public SightingsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            SightingService sightings)
        {
            _context = context;
            _environment = environment;
            _sightings = sightings;
        }

        [HttpGet]
        public IActionResult Information()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Form()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LocalSightings()
        {
            var sightings = await _context.TickSightings
                .AsNoTracking()
                .Select(sighting => new
                {
                    id = "local-" + sighting.TickSightingId,
                    date = sighting.SightingDate,
                    location = sighting.City,
                    species = sighting.Species,
                    latinName = sighting.LatinName,
                    description = sighting.Description,
                    imagePath = sighting.ImagePath,
                    source = "Community report"
                })
                .ToListAsync();

            return Json(sightings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Form(
            [Bind("City,SightingDate,Species,LatinName,Description")]
            TickSighting sighting,
            IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(sighting.City))
            {
                ModelState.Remove(nameof(sighting.City));
                ModelState.AddModelError(
                    nameof(sighting.City),
                    "Please enter a city or town.");
            }

            if (sighting.SightingDate == default)
            {
                ModelState.Remove(nameof(sighting.SightingDate));
                ModelState.AddModelError(
                    nameof(sighting.SightingDate),
                    "Please enter a valid sighting date.");
            }
            else if (sighting.SightingDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(sighting.SightingDate),
                    "The sighting date cannot be in the future.");
            }

            if (string.IsNullOrWhiteSpace(sighting.Species))
            {
                ModelState.Remove(nameof(sighting.Species));
                ModelState.AddModelError(
                    nameof(sighting.Species),
                    "Enter a species, or Unknown if unsure.");
            }

            string? imageExtension = null;

            if (image != null)
            {
                if (image.Length == 0)
                {
                    ModelState.AddModelError(
                        "image", "The selected image is empty.");
                }
                else if (image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "image", "Choose an image smaller than 5 MB.");
                }
                else
                {
                    byte[] header = new byte[8];

                    using var stream = image.OpenReadStream();
                    int bytesRead = 0;

                    while (bytesRead < header.Length)
                    {
                        int read = await stream.ReadAsync(
                            header, bytesRead, header.Length - bytesRead);

                        if (read == 0)
                        {
                            break;
                        }

                        bytesRead += read;
                    }

                    bool isJpeg = bytesRead >= 3
                        && header[0] == 0xFF
                        && header[1] == 0xD8
                        && header[2] == 0xFF;

                    bool isPng = bytesRead == 8
                        && header[0] == 0x89
                        && header[1] == 0x50
                        && header[2] == 0x4E
                        && header[3] == 0x47
                        && header[4] == 0x0D
                        && header[5] == 0x0A
                        && header[6] == 0x1A
                        && header[7] == 0x0A;

                    if (isJpeg)
                    {
                        imageExtension = ".jpg";
                    }
                    else if (isPng)
                    {
                        imageExtension = ".png";
                    }
                    else
                    {
                        ModelState.AddModelError(
                            "image", "Please upload a JPG or PNG image.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(sighting);
            }

            string? savedFilePath = null;

            try
            {
                if (image != null && imageExtension != null)
                {
                    string uploadsFolder = Path.Combine(
                        _environment.WebRootPath, "uploads");

                    Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid() + imageExtension;

                    savedFilePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(
                        savedFilePath, FileMode.CreateNew))
                    {
                        await image.CopyToAsync(stream);
                    }

                    sighting.ImagePath = "/uploads/" + fileName;
                }

                sighting.City = sighting.City.Trim();
                sighting.Species = sighting.Species.Trim();
                sighting.SubmittedAt = DateTime.UtcNow;

                _context.TickSightings.Add(sighting);
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (savedFilePath != null && System.IO.File.Exists(savedFilePath))
                {
                    System.IO.File.Delete(savedFilePath);
                }

                throw;
            }

            TempData["SuccessMessage"] =
                "Your tick sighting has been submitted.";

            return RedirectToAction(nameof(Form));
        }

        [HttpGet]
        public async Task<IActionResult> Data(
            string? city,
            DateTime? from,
            DateTime? to,
            string? species)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Please provide valid dates." });
            }

            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            {
                return BadRequest(new
                {
                    error = "The end date must be on or after the start date."
                });
            }

            try
            {
                return Json(await _sightings.GetAsync(city, from, to, species));
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new
                {
                    error = "Sightings are temporarily unavailable. Please try again later."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Insights(
            string? city,
            DateTime? from,
            DateTime? to,
            string? species)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Please provide valid dates." });
            }

            if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            {
                return BadRequest(new
                {
                    error = "The end date must be on or after the start date."
                });
            }

            try
            {
                var result = await _sightings.GetAsync(city, from, to, species);

                var regions = result.Sightings
                    .GroupBy(item => item.Location.Trim().ToUpperInvariant())
                    .Select(group => new
                    {
                        location = group.First().Location.Trim(),
                        count = group.Count()
                    })
                    .OrderBy(item => item.location);

                var monthly = result.Sightings
                    .GroupBy(item => new { item.Date.Year, item.Date.Month })
                    .OrderBy(group => group.Key.Year)
                    .ThenBy(group => group.Key.Month)
                    .Select(group => new
                    {
                        month = $"{group.Key.Year:D4}-{group.Key.Month:D2}",
                        count = group.Count()
                    });

                return Json(new
                {
                    total = result.Sightings.Count,
                    regions,
                    monthly,
                    warnings = result.Warnings
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new
                {
                    error = "Insights are temporarily unavailable. Please try again later."
                });
            }
        }
    }
}