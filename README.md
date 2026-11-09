# RaceDayWeb

ASP.NET Core MVC front end for the RaceDay platform. It has no database access and no business rules of its own. Everything goes through the RaceDayAPI, including all image uploads to Azure Blob Storage. 

## Run it locally

1. Start RaceDayAPI (the https profile, `https://localhost:49556`).
2. Check `Api:BaseUrl` in `appsettings.json` points at it.
3. Run RaceDayWeb with the `https` profile, or from a terminal:

```
dotnet run --launch-profile https
```

The site opens on `https://localhost:49600`.

Seeded test accounts (password `Password123!`):

| Role | Email |
| --- | --- |
| Organiser | thandiwe@raceday.co.za |
| Participant | tina@example.com |

## Run it in Docker

Start RaceDayAPI first (the http profile on port 49557 must be reachable), then from this folder:

```
docker build -t raceday-web .
docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:49557/ raceday-web
```

Open `http://localhost:8080`.

Notes:
- `host.docker.internal` is how the container reaches the API running on your machine. The `--add-host` flag is only needed on Linux and is harmless elsewhere.
- Point `Api__BaseUrl` at a deployed API instead and the same command works with no other change.
- The container runs as a non-root user, and HTTPS redirection is switched off inside it because it listens on plain http.

## How it hangs together

- `Services/ApiClient.cs` is the only class that talks to the API. It attaches the JWT from the session.
- `Services/ApiRoutes.cs` lists every route used.
- `Filters/SessionAuthorizeAttribute.cs` protects pages by login and role.
- Session holds the JWT, role and display name (30 minute idle timeout).
