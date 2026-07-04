# URL Shortener

A full-stack URL shortener built with **ASP.NET Core Web API (.NET)**, **ADO.NET**, **SQL Server**, and a vanilla **HTML/CSS/JavaScript** frontend. No Entity Framework, no frontend frameworks — raw `SqlConnection`/`SqlCommand` on the backend, raw `fetch()` on the frontend.

College project. Runs entirely locally against a local SQL Server instance.

---

## Tech stack

| Layer    | Technology |
|----------|------------|
| Backend  | ASP.NET Core Web API (.NET 10), C# |
| Data access | ADO.NET (`SqlConnection`, `SqlCommand`, `SqlDataReader`) — no EF |
| Database | SQL Server (LocalDB / Express / full) |
| Frontend | HTML5, CSS3, vanilla JavaScript (Fetch API) — no React/Angular/Vue/jQuery |

---

## Project structure

```
UrlShortener/
├── Controllers/
│   └── UrlController.cs        # POST/GET/DELETE /api/url
├── Models/
│   └── UrlModel.cs              # DB row + request/response DTOs
├── Data/
│   └── DatabaseHelper.cs        # all raw ADO.NET SQL access
├── Services/
│   ├── ShortCodeGenerator.cs    # random 6-char code + uniqueness check
│   └── UrlShortenerService.cs   # validation + business logic
├── wwwroot/
│   ├── index.html
│   ├── style.css
│   └── script.js
├── Database/
│   └── schema.sql               # run once in SSMS to create DB + table
├── appsettings.json             # connection string + base URL
├── Program.cs                   # DI wiring, static files, redirect route
└── UrlShortener.csproj
```

Layering: **Controllers** handle HTTP → **Services** validate and orchestrate → **Data** talks to SQL Server. Controllers never touch `DatabaseHelper` directly.

---

## Prerequisites

1. **.NET SDK** — install from https://dotnet.microsoft.com/download. Check with:
   ```
   dotnet --version
   ```
2. **SQL Server** — any of these work:
   - SQL Server Express (recommended for this project, free): https://www.microsoft.com/sql-server/sql-server-downloads
   - SQL Server LocalDB (comes with Visual Studio)
   - A full SQL Server instance you already have
   - Via `winget`: `winget install --id Microsoft.SQLServer.2022.Express`
3. **SQL Server Management Studio (SSMS)** (optional, for running the schema script visually) — https://aka.ms/ssmsfullsetup. You can also run the script with `sqlcmd` (comes with SQL Server) instead of installing SSMS.

---

## Setup (step by step)

### 1. Confirm SQL Server is running

After installing SQL Server Express, check the service is up:

```powershell
Get-Service -Name 'MSSQL*'
```

You should see something like `MSSQL$SQLEXPRESS` with status `Running`. The instance name to use in your connection string is `.\SQLEXPRESS` (or just `.` / `localhost` if you installed a default, unnamed instance).

### 2. Create the database and table

Run `Database/schema.sql` against your instance. Either:

**Using SSMS:** open the file, connect to your server, hit Execute.

**Using sqlcmd:**
```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i "Database\schema.sql"
```
(`-E` uses Windows integrated auth; drop it and add `-U`/`-P` if you use SQL auth instead.)

This creates the `UrlShortenerDB` database and the `Urls` table:

| Column      | Type           | Notes                     |
|-------------|----------------|---------------------------|
| Id          | INT IDENTITY   | Primary key                |
| OriginalUrl | NVARCHAR(MAX)  | NOT NULL                   |
| ShortCode   | VARCHAR(8)     | UNIQUE, NOT NULL           |
| CreatedAt   | DATETIME       | DEFAULT GETDATE()          |
| ClickCount  | INT            | DEFAULT 0                 |

### 3. Set the connection string

Edit `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=UrlShortenerDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

- `Server=` — match your instance name from step 1 (`.\SQLEXPRESS`, `localhost`, `.\MSSQLSERVER`, etc).
- Using SQL auth instead of Windows auth? Replace `Trusted_Connection=True` with `User Id=...;Password=...;`.

Also check `AppSettings:BaseUrl` matches the address you'll run the app on (default `http://localhost:5000`) — this is used to build the short links returned to the frontend.

### 4. Run the app

```powershell
dotnet restore
dotnet run
```

Open the printed URL (e.g. `http://localhost:5000`) in a browser. The frontend, API, and redirect endpoint are all served from the same app — no separate frontend server needed.

---

## Using the app

1. Paste a URL (must start with `http://` or `https://`) into the input box, click **Shorten URL**.
2. The generated short URL appears in a card below with a **Copy** button.
3. The **Recent Links** table lists every shortened URL (newest first) with click counts.
4. Click a short URL (in the table or the result card) to be redirected to the original URL — this also increments its click count.
5. Click **Delete** on a row to remove that link immediately (no page refresh).

---

## API reference

| Method | Route                    | Description                              |
|--------|--------------------------|-------------------------------------------|
| POST   | `/api/url`                | Create a shortened URL. Body: `{ "originalUrl": "https://..." }` |
| GET    | `/api/url`                 | List all URLs, newest first               |
| DELETE | `/api/url/{shortCode}`      | Delete a URL by its shortcode             |
| GET    | `/{shortCode}`              | Redirect to the original URL, track click |

Example:

```bash
curl -X POST http://localhost:5000/api/url \
  -H "Content-Type: application/json" \
  -d "{\"originalUrl\":\"https://github.com\"}"
```

```json
{
  "id": 1,
  "originalUrl": "https://github.com",
  "shortCode": "Ab3X9Q",
  "shortUrl": "http://localhost:5000/Ab3X9Q",
  "createdAt": "2026-07-04T21:39:13.467",
  "clickCount": 0
}
```

---

## Troubleshooting

**`System.NotSupportedException: Globalization Invariant Mode is not supported`** when calling any API endpoint
→ Make sure `<InvariantGlobalization>` is **not** set to `true` in `UrlShortener.csproj`. `Microsoft.Data.SqlClient` needs ICU globalization; invariant mode breaks every `SqlConnection.Open()` call.

**`Could not copy ... UrlShortener.exe ... being used by another process`** on `dotnet build`
→ A previous `dotnet run` is still holding the executable. Stop it first (find the process and kill it, or just close the terminal running it) then rebuild.

**Connection string errors / "A network-related or instance-specific error"**
→ Confirm the SQL Server service is running (`Get-Service -Name 'MSSQL*'`) and that `Server=` in `appsettings.json` matches the actual instance name.

**"Please enter a valid URL"**
→ The URL must be absolute and use `http://` or `https://`. Validation happens both in the browser (`script.js`) and on the server (`UrlShortenerService.IsValidUrl`) — invalid URLs are rejected before ever reaching the database.
