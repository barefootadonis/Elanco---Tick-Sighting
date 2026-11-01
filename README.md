# Tick Tracker

**Developed by Adesuwa Nosa-Ogie**  
Elanco Placement Program - Full Stack Technical Task

## Overview

Tick Tracker is a full-stack web application for reporting and viewing tick sightings across the UK.

The application combines records from the supplied Elanco educational API with community reports stored in a local MySQL database. Users can explore sightings on an interactive map, filter records, view activity over time and submit reports with photographs.

This is an educational MVP, not an official surveillance system or a medical risk assessment tool.

## Features

### Interactive Map

- Interactive UK map built with Leaflet and OpenStreetMap.
- Sightings grouped by city.
- Blue markers indicate cities containing a report from the last 30 days.
- White markers indicate older reports only.
- Filters for species and date range.
- A reset button to clear filters.

Markers represent approximate city centres because the supplied API provides city names rather than precise coordinates.

### Sighting Information

- A details panel displaying the selected report's date, location, species, Latin name and source.
- Descriptions and photographs for community reports where provided.
- A monthly activity timeline for the selected city.
- Directions to the city through Google Maps.
- Sharing through the browser's sharing feature or a copied link.

### Educational Content

- Information about selected UK tick species.
- Tick bite prevention guidance.
- A city- and year-based chart showing monthly reported sightings.
- Links to NHS and UK Health Security Agency guidance.

### Report a Sighting

- A form accepting location, sighting date, species and optional details.
- Optional JPG or PNG uploads, limited to 5 MB.
- Server-side validation for required answers and future dates.
- Inline feedback when a required answer is left blank.
- Submission error and success messages.

## Technical Choices

| Technology | Purpose and reasoning |
|---|---|
| C# and ASP.NET Core MVC | Separates request handling, application logic and page presentation. I also had previous experience with this stack. |
| Razor views | Integrates server-rendered HTML with the ASP.NET application without requiring a separate frontend framework. |
| Entity Framework Core | Handles database queries and saving reports through C# models. |
| MySQL | Provides persistent storage for community reports. |
| JavaScript | Handles map interactions, filtering, sharing and chart rendering. |
| Leaflet and OpenStreetMap | Provide an interactive map with attribution. |
| Bootstrap and CSS | Support responsive layouts and consistent form controls. |
| In-memory caching | Reduces repeated calls to the external API. |

The frontend uses HTML, CSS and JavaScript to keep the MVP manageable within the available development time.

## Requirements

- Visual Studio with the ASP.NET and web development workload, or a compatible .NET development environment.
- The .NET SDK version specified by `TargetFramework` in the project's `.csproj` file.
- MySQL running locally. XAMPP can be used to provide the database server.
- Internet access for the Elanco API, Leaflet assets and OpenStreetMap tiles.

NuGet dependencies are listed in the project's `.csproj` file and restored when the project is built.

## How to Run

### 1. Get the project

Clone or download this repository and open the solution in Visual Studio.

### 2. Start MySQL

Start the MySQL server, for example through the XAMPP control panel.

### 3. Configure the connection

Update `ConnectionStrings:DefaultConnectionMySQL` in `appsettings.json` to match your local database credentials:

```json
"ConnectionStrings": {
  "DefaultConnectionMySQL": "server=localhost;port=3306;database=ticksightings;user=root;password=YOUR_PASSWORD;"
}
```

Use your own username and password. The database account needs permission to create the database and apply migrations.


### 4. Restore packages and build

Build the solution in Visual Studio. Allow NuGet packages to restore if prompted.

### 5. Create the database tables

Open **Tools → NuGet Package Manager → Package Manager Console**.

Select the application project as the default project and run:

```powershell
Update-Database
```

The repository includes Entity Framework migrations, so a new migration is not required for initial setup.

### 6. Run the application

Start the project using Visual Studio's HTTPS launch profile. Open the localhost URL shown by Visual Studio.

The default page is the report form. Use the navigation to access the sightings map and About Ticks page.

No login or API key is required.

### 7. Try the main workflow

1. Submit a sighting for London with a valid date and species.
2. Optionally attach a JPG or PNG smaller than 5 MB.
3. Open Sighting Information.
4. Select the London marker and find the submitted report.
5. Try the species and date filters.
6. View the timeline and test the sharing button.

Community reports are stored in MySQL. Uploaded images are saved in `wwwroot/uploads`, and their relative paths are stored in the database. The uploads folder is created automatically.

## Architecture

The application follows an MVC structure:

- **Models** describe stored reports and the records returned to the frontend.
- **Controllers** handle page requests, form submissions and JSON endpoints.
- **Views** contain the Razor pages.
- **SightingService** retrieves, validates, combines and filters sightings.
- **ApplicationDbContext** provides access to MySQL through Entity Framework Core.

The service collects and prepares the sighting data. The controllers use it to return both individual reports and summaries, such as monthly totals.

### Data Flow

1. The browser requests sightings from the application's backend.
2. `SightingService` retrieves external records from the supplied Elanco API, using a five-minute in-memory cache.
3. Community reports are retrieved from MySQL.
4. External records with missing essential values, invalid dates or repeated IDs are excluded.
5. The combined records are returned as JSON.
6. JavaScript groups records by city and displays markers and report details.
7. The insights endpoint calculates monthly totals and counts by city.
8. The frontend presents those totals as activity charts.

The About page also retrieves its records through the application's backend and groups them by month for the selected city and year.

### Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/Sightings/Data` | Returns combined sightings and source warnings. Supports `city`, `from`, `to` and `species` filters. |
| GET | `/Sightings/Insights` | Returns total sightings, counts by city and monthly totals, with the same filter parameters. |
| GET | `/Sightings/LocalSightings` | Returns locally submitted reports. |
| POST | `/Sightings/Form` | Validates and saves a new report and optional image. |

Example requests:

```text
/Sightings/Data?city=London&from=2024-01-01&to=2024-12-31
/Sightings/Insights?city=London
```

The map also filters its loaded records in the browser for immediate interaction.

### Duplicate and Incomplete Data Handling

Repeated API records with the same ID are removed. API IDs start with api- and community report IDs start with local-, so they can be distinguished.

Reports with different IDs are kept, even when their city, species and date match, because they could be separate sightings.

API records are left out if their ID, location or date is missing or invalid. If no species is provided, it is shown as “Unknown”.

### Error Handling

- Invalid submissions return clear validation messages.
- Future sighting dates are rejected.
- Image uploads are checked for size and JPG/PNG file signatures.
- Invalid date filters return a bad-request response.
- External requests have a timeout.
- Failure of the external API does not prevent the service from attempting to load community reports.
- Failure of one source is reported through warnings.
- If both sources are unavailable, the data endpoints return a service-unavailable response.
- Server-side source failures are logged.
- A shared error page handles unexpected application errors outside Development mode.

## Accessibility Features

- Labels are associated with form inputs and filter controls.
- Required answers are identified in text.
- Images have alternative text.
- Pages use headings and semantic sections.
- Interactive controls have visible keyboard focus styles.
- Status messages use live status regions.
- Charts include readable month labels and numeric totals.
- Marker recency is distinguished through fill style as well as colour.
- Navigation and layouts adapt to smaller screens.
- The timeline can receive keyboard focus for scrolling.

These features improve accessibility, but the MVP has not undergone a formal accessibility audit. A keyboard-accessible alternative to selecting map markers would be a valuable next improvement.

## Limitations and Future Improvements

### Location Coverage

The map currently uses a lookup of 14 city-centre coordinates. Reports for other locations are saved but cannot be mapped.

With more time, I would add validated location search and coordinates, allowing more locations while preserving appropriate location privacy.

### Data Quality

With more time, I would add checks to help identify repeated reports
and incorrectly entered tick species.

### Testing

I would test the application on more browsers and screen sizes,
and check that people can use it with a keyboard.

## Design Process

Initial wireframes were drawn before implementation to plan the reporting form and educational page.

The wireframe diagrams are available in
[wwwroot/images/wireframes](wwwroot/images/wireframes).

The interface uses white and blue (`#42a5f5`) consistently, with responsive layouts and different input widths to reflect the expected answer length.

## Sources and Image Credits

- [Elanco educational Tick Sightings API](https://dev-task.elancoapps.com/)
- [UKHSA tick species profiles](https://www.gov.uk/guidance/tick-species-profiles)
- [UKHSA tick awareness guidance](https://ukhsa.blog.gov.uk/2026/06/30/what-are-ticks-and-how-can-you-avoid-being-bitten/)
- [NHS Lyme disease guidance](https://www.nhs.uk/conditions/lyme-disease/)
- [Leaflet](https://leafletjs.com/)
- [OpenStreetMap contributors](https://www.openstreetmap.org/copyright)

## Video Walkthrough

The video walkthrough demonstrates the main features of the application, including reporting a sighting, viewing the map, and filtering sightings. It can be found in the repository under the file name Video presentation.mp4.
