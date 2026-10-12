# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

FDK (Financial Development Kit) — SoftFX/TickTrader trading client API for TickTrader servers, published as the NuGet package `SoftFx.FDK`. Windows-only, mixed native C++ / .NET Framework codebase using old-style (non-SDK) `.csproj` and `.vcxproj` projects built with MSBuild / Visual Studio 2022.

A toolset modernization is in progress in the working tree: `PlatformToolset` v141 → v143 (most vcxproj done), `TargetFrameworkVersion` v4.5 → v4.8 (only some csproj done; others still v4.0/v4.5/v4.6). Expect mixed toolsets between sibling projects until it completes.

## Build

`msbuild` is not on PATH in this shell. Use the full path (or a VS2022 Developer prompt):

```
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" <args>
```

```bash
# Full product (native C++ + managed), one platform at a time:
"C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/MSBuild.exe" FDK/FDK.sln /p:Configuration=Release /p:Platform=x64 /m
# C++ projects must be built per-platform (Win32 + x64); C# projects are AnyCPU.

# Managed library only (no C++ toolchain needed):
msbuild FDK/SoftFX.Extended.Net/SoftFX.Extended.Net.csproj /p:Configuration=Release
```

All projects in a solution share output folders `FDK\Debug\` and `FDK\Release\`.

Build side effects to know about:
- Pre-build events regenerate code and re-zip `External\Dll` + `External\Assembly` into `External\Archives\*.zip` (embedded into `SoftFX.Extended.Net.dll`, extracted at runtime to load native DLLs).
- Post-build events copy binaries to the repo-root `FRE\` folder (gitignored).
- `Build\FDK.build` + `FDK.properties` are legacy NAnt scripts wrapping MSBuild for release packaging (both platforms, zips, NuGet push) — not needed for normal development.

## Tests

Classic MSTest (`Microsoft.VisualStudio.TestTools.UnitTesting`), not xUnit/NUnit. Build `FDK\FDK.Tests.sln` first, then run the output DLLs:

```
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" FDK\Debug\SoftFX.Extended.Net.Tests.dll
```

- `SoftFX.Extended.Net.Tests` — offline (FinancialCalculator round-trip cases from embedded resources, protocol version checks). Safe to run anywhere.
- `SoftFX.Extended.Net.Functional.Tests` — **require a live connection** to demo server `ttdemo.fxopen.com` (credentials hardcoded in `SoftFX.Extended.Net.Functional.Tests\Configuration.cs`); also `SoftFX.Extended.Net.Tests\Configuration.cs` initializes paths. Long-running; don't run without network access.
- Single test: append `/Tests:MethodName` or `/TestCaseFilter:Name=...` to the vstest.console command.
- `LRP\LRP.sln` contains `Lrp.Core.Net.Units` (also MSTest) for the LRP runtime.

## Architecture

The repo is two halves joined by an LRP (Local/Remote Procedure-call) boundary. The FDK.sln solution folders are numbered by layer.

### Native C++ side (`FDK\`)

Dependency graph (ProjectReferences):

- **Sal** — system abstraction layer: sockets (plain + OpenSSL TLS), threading, logging.
- **Core** (→ Sal) — the `Fx*` entity model (quotes, orders, execution reports, symbol/account/currency info), handle pool, exception hierarchy, and the `IConnection` provider SPI.
- **LlCommon** — standalone financial calculator engine.
- **QuickFix** — vendored QuickFIX engine (no project refs).
- **FixProvider** (→ Core, QuickFix, Sal) — implements `IConnection` over FIX 4.4 to a TickTrader server.
- **AggrProvider** (→ Core, QuickFix, Sal) — "aggr" protocol connection to bridge/feeder services.
- **LrpCommon** + **LrpProvider** (→ Core, LrpCommon, Sal) — LRP wire protocol and LRP-transport provider.
- **LrpServer** (→ Core, LrpCommon, Sal) — native LRP server for hosting FDK server-side.
- **LlApi** (→ Core, LrpCommon, Sal) — the native FDK host, built as `SoftFX.LlApi.x86/x64.dll`. It exports only `LrpInvoke`/`LrpSignature` (see `LlApi.def`) and **dynamically loads** the connection provider DLLs (FixProvider/LrpProvider/AggrProvider) at runtime based on the connection string's protocol (`LlApi\ConnectionBuilder.cpp`, `DllLibrary.cpp`).

### Managed side

- **SoftFX.Extended.Net** — the public high-level API (namespace `SoftFX.Extended`). Entry classes `DataFeed` / `DataTrade` : `DataClient` (`Client\DataClient.cs`) — own the event-pump thread, `Initialize(connectionString)`, Start/Stop. Connection strings (`Library\`): `FixConnectionStringBuilder` (FIX.4.4, TargetCompId=EXECUTOR, ports 5001–5004) or `LrpConnectionStringBuilder` (port 5010) — the transport choice is made inside the native core, not in C#.
- **SoftFX.Net** — small facade over Extended (`SoftFX.Basic`: `Manager`, `Snapshot`/`SnapshotReader`).
- **SoftFX.Internal.Net**, **LrpServer.Net** — internal tooling: FIX parsing, LRP acceptor/server glue.
- **SoftFX.Lrp.dll** (prebuilt in `External\Dll`, sources in `LRP\Lrp.Core.Net`) — managed LRP runtime.

### The LRP interop mechanism (key to everything)

There is no P/Invoke of FDK functions. `SoftFX.Extended.Net\Core\Native.cs` creates in-process LRP `LocalClient`s bound to `SoftFX.LlApi.<Platform>.dll` and `SoftFX.LlCommon.<Platform>.dll`, validates an interface-signature hash, and exposes generated proxy objects. Native DLLs are extracted at runtime from zip resources embedded in the assembly (`Resources\ModulesManager.cs`).

Protocol interfaces are defined in XML IDLs and **code is generated at every build** by `External\bin\lrp.exe` (sources: `LRP\lrp\`):

- `FDK\FDK\FDK.xml` → C# client side `SoftFX.Extended.Net\Generated\`, C++ server side `FDK\LlApi\` (`Server.hpp`, `TypesSerializer.hpp`, …)
- `FDK\FDK\LlCommon.xml` → `SoftFX.Extended.Net\Financial\Generated\`
- Also `FDK\LocalCpp.xml` / `LocalCSharp.xml` (LrpServer ↔ LrpServer.Net) and `LrpProvider` / `BridgeCommon` protocol files.

**Never hand-edit files starting with `// This is always generated file. Do not change anything.`** Change the XML IDL (and/or the native implementation) instead; the build regenerates both sides. The managed and native signatures are validated against each other at runtime, so they must stay in sync.

To add/modify an API method: edit `FDK.xml` (type/component/method), regenerate happens on build, then implement the native side in `LlApi\*Impl.cpp` and add the managed wrapper in `SoftFX.Extended.Net`.

## Conventions & gotchas

- Managed assemblies are strong-name signed (`SignAssembly`, `FDK.snk` in each project's Resources or `External\FDK.snk`).
- Product version for packaging lives in `FDK\project.version.cfg` / `Build\FDK.nuspec`; assembly versions in `AssemblyInfo.cs` files are maintained separately and may drift.
- `Examples\CSharp\*` contain hardcoded demo credentials/hosts — treat as examples only.
- Solution-wide platform pairing: when building C++ pick `Platform=x64` or `Win32` (`Platform86`), C# uses `AnyCPU`/`Mixed Platforms` in solution contexts.

## Repository layout (beyond `FDK\`)

- `LRP\` — the LRP framework itself: `lrp` code generator, `Lrp.Core.Net`/`Lrp.Core.Cpp` runtimes, `LrpHost.x86/x64.exe` out-of-process hosts, sample local/remote client/server projects, protocol test definitions.
- `External\` — prebuilt binaries: managed runtime `SoftFX.Lrp.dll`, native `FDK.LRP.*` DLLs, OpenSSL (`libeay`/`ssleay`), `lrp.exe`, third-party assemblies.
- `Examples\` — C# and C++ sample apps (AccountInfo, DataFeed/DataTrade/DataClient/TradeFeed examples, FeedRebuild, FinancialExample WinForms).
- `Advisers\`, `Concepts\`, `Tools\` (PerformanceTest/Analyzer), `FDK.Setup\` (NSIS/WiX installers), `FDK2R\` (R-language wrapper), `MQL2FDK\` (MQL4 → FDK strategy translator), `TeX\` (FIX/LRP protocol docs), `Documentation\` (CHM).

## Git workflow

Work happens on the `develop` branch; PRs merge `SoftFx/develop` → `master`. Commit messages typically reference tickets as `TP-xxxx: description`.
