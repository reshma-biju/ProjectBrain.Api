# Project Brain API: Task 1 Observations

**Author:** Reshma Biju
**Started:** 2 November 2026 · **Completed:** 3 November 2026
**Task:** Environment and hosting smoke test

## Summary

Task 1 is complete. SQL Server runs locally in Docker. A free MonsterASP.NET account provides a hosted SQL Server database and a Windows (IIS) website. The default ASP.NET Core Web API (.NET 9) was published with Web Deploy and is publicly reachable over HTTPS, returning JSON at `/weatherforecast`. An initial India-based speed test gave response times between 0.2 s and 1.8 s, which needs more measurements. Database access through the deployed API and CORS from the frontend are still to be tested.

## Results at a glance

| Check | Result |
|---|---|
| SQL Server in Docker (local) | Works. Connected from SSMS at `localhost,1433` as `sa`. |
| Hosted SQL Server database | Works from SSMS after enabling remote access and using the `tcp:` prefix. |
| Create a table and a stored procedure on the host | Works. Both were created, run and dropped. |
| Host runs a .NET app | Works. Windows / IIS (`Microsoft-IIS/10.0`). The default API responds at `/weatherforecast`. |
| HTTPS on the free subdomain | Works after enabling Let's Encrypt in the control panel. |
| Response time from India | 0.2 s to 1.8 s over five tests. More testing needed. |
| Database access through the deployed API | Not tested yet. |
| CORS from the Vercel frontend | Not tested yet. |

## 1. Local development environment

- SQL Server 2022 image in Docker Desktop, started with Docker Compose. The compose file uses a named volume, and the password lives in a git-ignored `.env` file.
- Connect from SSMS with server `localhost,1433`, SQL Server Authentication, login `sa`, and Trust server certificate ticked.
- `sa` is the built-in System Administrator login. It is for local development only and must never be used on a real host. Plan: create a limited login for the API.
- LocalDB works on the work laptop but only runs on this machine, and `.\SQLEXPRESS` has restricted permissions. Decision: Docker replaces LocalDB, which is dropped from the plan.
- Tools: Visual Studio 2022 17.14.41. Installed SDKs: 9.0.318, 10.0.113, 10.0.303, 10.0.402.

## 2. .NET version decision

- Visual Studio 2022 cannot properly target `net10.0`. That needs Visual Studio 2026 (version 18.0 or later), so the template dropdown only offers .NET 8 and 9.
- .NET 8 and .NET 9 both reach end of support on 10 November 2026.
- The project targets .NET 9 for now, because the installed SDK includes that runtime.
- Plan: retarget to `net10.0` before the public deployment (Task 13). Change `TargetFramework` in the project file, update the NuGet packages, and build with Visual Studio 2026 or the command line.
- The host's example applications included .NET 10 demos, so the host runs .NET 10.

## 3. Project and repository

- Project `ProjectBrain.Api`: ASP.NET Core Web API with controllers, OpenAPI, HTTPS, and container support (a Dockerfile was generated).
- A separate GitHub repository from the React frontend. Vercel stays connected to the frontend repository only.
- `.gitignore`: `.env` must stay ignored. The publish profile (`*.pubxml`) should be ignored too.

## 4. MonsterASP.NET hosting and deployment

- Hosting environment: Windows with IIS. Hosting server: `Microsoft-IIS/10.0`.
- Deployment options in the panel: FTP, WebDeploy, and Git.
- Free plan, as shown on the pricing page: one website with 5 GB storage and 256 MB dedicated RAM, one database with 1 GB storage, no custom domain, no e-mail accounts, limited features and traffic, no guarantees or warranties, EU datacenter only.
- The dashboard offered up to 5 free websites and 5 free databases, while the pricing page listed one of each. The limits are unclear, so read the terms.
- Database: type MSSQL, 1 GB limit, compatibility level shown as SqlServer2025, EU server.
- Remote access is disabled by default and has to be switched on in the panel before SSMS can connect.
- The website uses a free `runasp.net` subdomain, created as "My own application" (an empty site).

### Publishing the default API

- Downloaded the publish profile from the panel, imported it in Visual Studio (Publish, then Import Profile), and published with Web Deploy.
- Visual Studio created a `.pubxml` file under `Properties/PublishProfiles`. It contains server details, so it must stay out of Git.
- Result: `https://project-brain-api.runasp.net/weatherforecast` returned five forecasts as JSON, so the .NET 9 app runs on the host and is publicly accessible.
- A 404 at the root address of the default template is normal. The sample endpoint is `/weatherforecast`.

## 5. HTTPS configuration

- Initially, the API was accessible over HTTP, but HTTPS returned a connection error.
- Found the **Let's Encrypt** option in the MonsterASP.NET control panel and enabled HTTPS there.
- HTTPS now works, and the endpoint returns a valid JSON response over HTTPS.
- This matters for the project: the React frontend on Vercel is served over HTTPS, and browsers block insecure calls from a secure page, so the API has to use HTTPS too.

## 6. API response time (India-based speed tests)

Tested with ServerCake's website speed test, which measures from a server in India.

### First test in detail

| Metric | Result |
|---|---:|
| Total load time | 1.25 seconds |
| Time to first byte (TTFB) | 1250 ms |
| Data transferred | 0.4 KB |
| HTTP status | 200 OK |
| Server | Microsoft-IIS/10.0 |
| Content type | application/json; charset=utf-8 |
| Test location | ServerCake server, India |

### All measurements

| Test | Total response time |
|---|---:|
| 1 | 1.25 s |
| 2 | 1.8 s |
| 3 | 1.2 s |
| 4 | 0.2 s |
| 5 | 0.2 s |

### Observations

- The API returned HTTP 200 OK, so the endpoint responds correctly.
- The response was very small (0.4 KB), so the slow first results are not caused by transferring data.
- In the first test, TTFB and total load time were both 1250 ms, so most of the time was spent waiting for the server to start responding.
- The first three requests took between 1.2 s and 1.8 s. The last two took about 200 ms.
- Possible causes include an initial application startup delay (cold start), server load, network distance (the host is in the EU), or normal variation. The cause cannot be confirmed from these tests.
- These numbers are a starting baseline, not a conclusion. They come from a single testing service, and the endpoint does no real work yet.

## 7. Problems met and what they taught

1. **SSMS error 64 (network name no longer available).** Remote access was disabled in the panel, and Encrypt was set to Strict in SSMS. Fix: enable remote access, set Encrypt to Optional or Mandatory, and tick Trust server certificate.
2. **SSMS error 18456 (login failed).** The server was reached, but the login was rejected. Copy the credentials from the panel instead of retyping them.
3. **Connection fixed with the `tcp:` prefix.** The connection finally worked after adding `tcp:` before the server name.
4. **HTTPS connection error.** HTTPS failed until Let's Encrypt was enabled in the control panel.
5. **404 on the root address.** Normal for the default template. Use `/weatherforecast`.

## 8. Concepts learned

- A smoke test is a quick go or no-go check of the basics before investing more time.
- A Docker image is the template, and a container is a running instance. Docker Compose saves the run settings in a file.
- Kestrel is the web server built into the app. IIS is the Windows web server that hosts the app on Windows hosting.
- The operating system, the web server and the app are separate layers.
- Windows hosting uses IIS and Web Deploy. Linux hosting and containers use Kestrel, usually behind a reverse proxy.
- HTTPS on a shared host is configured in the control panel, not in the app.

## 9. Decisions

- Two databases with the same schema: Docker locally for development, MonsterASP for production. EF Core migrations keep them in sync.
- Docker locally only. No container deployment on MonsterASP.
- Separate repositories for the frontend and the API.
- EF Core for CRUD, plus Dapper and a stored procedure for the dashboard statistics. Stored procedures are confirmed to work on the host.

## 10. Target architecture

```
React frontend (Vercel, HTTPS)
        |
        v
ASP.NET Core Web API (MonsterASP.NET, HTTPS)
        |
        v
SQL Server database (MonsterASP.NET)
```

Status: the frontend and the API are deployed separately. The frontend still uses localStorage and is not connected to the API yet.

## 11. Open items

1. Test database access through the deployed API (a health endpoint that runs `SELECT 1`, planned for Task 3).
2. Test CORS from the Vercel frontend to the API.
3. Repeat the speed test after the API has been idle for 10 to 15 minutes, to check whether the first request is slow again. Record the range, not a single number.
4. Confirm that the `.pubxml` file and `.env` are not committed to Git.
5. Turn remote access off on the host when it is not needed, and keep the database password private.
6. Read the free-plan limits and terms (256 MB RAM, 1 GB database, EU only, no guarantees).
7. Retarget to .NET 10 before Task 13.
8. Create a limited SQL login for the API instead of using `sa`.

## 12. Conclusion

The initial API deployment and hosting smoke test was successful. SQL Server works locally and on the host, stored procedures are supported, the API is publicly accessible, HTTPS is enabled, and the test endpoint returns valid JSON. The India-based tests give an initial performance baseline of 0.2 s to 1.8 s. Database connectivity through the deployed API, CORS configuration, and performance under real application usage still need to be tested.

**Task 1 status: completed. MonsterASP.NET is confirmed as the deployment target for now.**

## Next: Task 2

Design the schema (Users, Projects, Items, Notes): list the queries the app needs, define the entities and attributes, choose keys, constraints and indexes, draw the ERD, and write the DDL by hand.
