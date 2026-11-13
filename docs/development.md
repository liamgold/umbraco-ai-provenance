# AI Provenance: development

This guide explains how to run, test and change the package locally.

## Requirements

- .NET 10 SDK
- Node.js 22 or later

## Solution layout

| Path | Contents |
|---|---|
| `src/Umbraco.Community.AIProvenance` | The package: C#, migrations, API, tag helper |
| `src/Umbraco.Community.AIProvenance/Client` | The backoffice extension (TypeScript, Lit, Vite) |
| `src/Umbraco.Community.AIProvenance.TestSite` | An Umbraco 17 site for local testing |
| `src/Directory.Packages.props` | Package versions for the whole solution |
| `tests/` | Automated tests (from Phase 1 of the [roadmap](roadmap.md)) |
| `docs/` | Design, roadmap, discovery notes and this guide |

The solution started from Lotte Pitcher's [Opinionated Package Starter](https://github.com/LottePitcher/opinionated-package-starter), which uses Umbraco's `umbraco-extension` template for the package project.

## Run the test site

The test site has a project reference to the package. Running the site builds the package with it, and Umbraco loads the package's composers, API controllers and backoffice files. You do not need to install anything from NuGet.

1. Build the backoffice extension:

   ```sh
   cd src/Umbraco.Community.AIProvenance/Client
   npm install
   npm run build
   ```

2. Run the site:

   ```sh
   cd src/Umbraco.Community.AIProvenance.TestSite
   dotnet run
   ```

3. Log in to the backoffice with the credentials in `appsettings.json`.

Before the first run, give the test site its own image-signing key in .NET user secrets. Otherwise, Umbraco generates one and writes it into `appsettings.json`, which is committed:

The key must be Base64. With Git Bash or another shell that has OpenSSL:

```sh
cd src/Umbraco.Community.AIProvenance.TestSite
dotnet user-secrets set "Umbraco:CMS:Imaging:HMACSecretKey" "$(openssl rand -base64 64 | tr -d '\n')"
```

With PowerShell:

```powershell
cd src/Umbraco.Community.AIProvenance.TestSite
dotnet user-secrets set "Umbraco:CMS:Imaging:HMACSecretKey" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)))
```

The test site uses [uSync](https://marketplace.umbraco.com/package/usync). It saves document types and other schema to disk, so test content types are in source control.

## Change the code

- **C# changes:** restart the site. `dotnet watch` restarts it when you save.
- **Backoffice changes:** run `npm run watch` in the `Client` folder. Vite rebuilds the extension when you save. Then refresh the browser.

## Test

- **Automated tests:** run `dotnet test` from `src`. The integration tests use `Umbraco.Cms.Tests.Integration` and start Umbraco with a real database.
- **Manual tests:** use the test site.
- **The NuGet package:** before a release, check the package itself, because a project reference cannot show a missing file:
  1. Pack it to a local folder: `dotnet pack -c Release /p:Version=0.0.1 -o c:\nuget.local`
  2. Add the folder as a NuGet source: `dotnet nuget add source c:\nuget.local`
  3. Install the package into a clean Umbraco 17 site and test it there.

## Test automatic marking with a real model

The test site includes Umbraco.AI, Agents, Copilot and the Microsoft Foundry provider. The integration tests simulate an agent run, so you only need a real model to check the whole flow.

1. Store your provider's API key in user secrets, under `Umbraco:AI:Secrets`:

   ```sh
   cd src/Umbraco.Community.AIProvenance.TestSite
   dotnet user-secrets set "Umbraco:AI:Secrets:FoundryApiKey" "<your key>"
   ```

2. Sign out of the backoffice and sign in again, so your access token includes the AI section.
3. In the AI section, create a connection. For the API key, enter `$Umbraco:AI:Secrets:FoundryApiKey`. Umbraco.AI reads the key from configuration, so the database stores only the reference.
4. Create a profile for a chat model deployment, and an agent with the `get_umbraco_content` and `set_umbraco_content_value` tools.
5. Ask the agent in the Copilot chat to change a property. The property's action menu and the Info view then show the mark, with source `UmbracoAI`.

The non-interactive `/run` endpoint skips write tools by default, so use the Copilot chat or the interactive stream.

## Umbraco versions and branches

- `main` targets **Umbraco 17**, the current Long Term Support (LTS) version.
- The package's Umbraco dependency is `[17.6.0, 17.999.999)`. NuGet then refuses to install it on Umbraco 18 or later, including 18 pre-releases. `[17.6.0, 18.0.0)` would still allow pre-releases such as `18.0.0-rc1`.
- The test site runs the latest Umbraco 17 release.

Umbraco 18 changed how the Management API generates its OpenAPI documents. A build for 17 with Management API endpoints does not run on 18. When work on 18 support starts:

1. Create `v17/main` from `main`. It receives fixes for the Umbraco 17 version.
2. Upgrade `main` to the new Umbraco version.

Short-term Umbraco versions do not get their own branches. Package versions follow the Umbraco version: `17.x` supports Umbraco 17, `18.x` supports Umbraco 18.

To keep the version-specific code small, keep the Management API code in one folder.

## Code rules

- **Write all code from scratch.** The package follows the documented behaviour of the products in [Prior art](design.md#prior-art). It does not copy their code, samples or API names.
- **Check before you rely on an API.** Confirm Umbraco and Umbraco.AI APIs in their source, their docs or the running test site.
- **Keep Umbraco.AI code in the add-on.** Only `Umbraco.Community.AIProvenance.UmbracoAI` references Umbraco.AI. See [`design.md`](design.md#umbracoai-integration-is-a-separate-package).
- **Discuss design changes first.** A change to a decision in [`design.md`](design.md) needs an issue or a discussion before the pull request.
