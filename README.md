# Veda Infinity School — ASP.NET Core (.NET 10)

Razor Pages website converted from the approved HTML design.

## Local database setup

The web project connects to the local `veda_infinity` MySQL database. In MySQL
Workbench, create a dedicated account for the app (rather than using `root`):

```sql
CREATE USER 'veda_app'@'localhost' IDENTIFIED BY '<choose-a-new-strong-password>';
GRANT SELECT, UPDATE ON veda_infinity.LoginMaster TO 'veda_app'@'localhost';
GRANT SELECT ON veda_infinity.RoleMaster TO 'veda_app'@'localhost';
```

Configure its password with .NET user-secrets; do not put credentials in
`appsettings.json`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnectionPassword" "<the-veda_app-password>" --project src\VedaInfinity.Web\VedaInfinity.Web.csproj
```

In deployed environments, set the `ConnectionStrings__DefaultConnectionPassword`
environment variable instead. The local connection requires TLS.

## Open in Visual Studio

1. Install **Visual Studio 2022 (17.14 or later)** or **Visual Studio 2026**.
2. In the installer, select workload **ASP.NET and web development**.
3. Install the **.NET 10 SDK**: https://dotnet.microsoft.com/download/dotnet/10.0
4. Extract this folder.
5. Double-click `VedaInfinity.sln` (or File → Open → Project/Solution).
6. If Visual Studio asks to trust the project, choose Trust.
7. Set startup project to **VedaInfinity.Web** if it is not already.
8. Press **F5** (or the green play button). The browser opens the login page.
9. Sign in with a valid user from the configured database.

## Pages

| URL | File |
|-----|------|
| /Login | Pages/Login.cshtml |
| /Dashboard | Pages/Dashboard.cshtml |
| /StudentMaster | Pages/StudentMaster.cshtml |
| /Portfolio | Pages/Portfolio.cshtml |
| /Teachers | Pages/Teachers/Index.cshtml |
| /Teachers/Courses | Pages/Teachers/Courses.cshtml |
| /Payment | Pages/Payment.cshtml |
| /Reports/Student | Pages/Reports/Student.cshtml |
| /Settings | Pages/Settings.cshtml |

Shared menu and header: `Pages/Shared/_Layout.cshtml`.

## Next coding step

Open the `.cshtml.cs` file for the page you want and replace the remaining `TODO` methods.
