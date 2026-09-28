# Native Overlay Rebuild — Progress Tracker

Status legend: ✅ Verified live · 🔧 Built, not live-verified · 🚧 In progress · ⬜ Not started

Updated as work happens — check here any time for current state without needing to ask.

## Core shell
| Module | Status | Notes |
|---|---|---|
| Transparent click-through window | ✅ | |
| Single PanelHost (GPU-contention fix) | ✅ | |
| Global keyboard hook (WH_KEYBOARD_LL) | ✅ | |
| Hotbar (Right Ctrl + digits → real SC keystrokes) | ✅ | User-confirmed in-game |
| Tool rail (Right Alt) | ✅ | 3 real bugs found+fixed |
| Seat/foot detection (Game.log) | 🔧 | Built, not live-tested |
| Settings persistence | ✅ | |
| Tray icon | ✅ | |

## Ship Loadout Bay (panel)
| Module | Status | Notes |
|---|---|---|
| Ship list + search | ✅ | Selection + detail pane both confirmed firing correctly (verified 2026-09-28 with a blocking diagnostic dialog) — the earlier "doesn't work" reports were the detail pane taking noticeably longer to render than Missions/Blueprints (blank under ~1s, populated after a few seconds), not a real failure. All catalog lookups it uses are cached, so the lag is in WPF layout of the ComboBox-heavy rows, not I/O. |
| Stats display (speed/hull/shield/weaponry) | ✅ | |
| Weapon mounts — swap + save | ✅ | Dropdown contrast bug fixed 2026-09-28: closed weapon combo boxes showed washed-out text on multi-mount ships (e.g. Gladius Pirate's 3 gun rows). Root cause: the default ComboBox ControlTemplate hardcodes a muted color for the closed box's selected-item display when unfocused — only one combo can hold focus at a time, so the others always showed it; a Style.Setter can't override a trigger baked into the template. Fixed with a full custom ControlTemplate (no default theme, no selection/focus trigger to fight). Verified all 3 gun rows render correctly. |
| Turret/manned-turret weapon rows | ✅ | Live-verified 2026-09-28 on Idris-M — all turret rows (front/rear/upper/lower, gun 1+2 each) render correctly with the fixed dark ComboBox style, real weapon names populate (Revenant Gatling, CF-557 Galdereen Repeater, M9A Cannon) |
| Missile racks — swap + save | ✅ | |
| Components: power plant/shield/cooler/qdrive — swap + save | ✅ | |
| Components: flight controller — swap + save | ✅ | Live-verified 2026-09-28 on Idris-M ("Flight Blade") |
| Radar / life support — read-only display | ✅ | Live-verified 2026-09-28 — renders, but shows raw internal class names (e.g. `radr_gnrp_s03_idris_tem...`) instead of a friendly name for at least some parts. Cosmetic data-quality issue, same class as the Missions/Blueprints template-string notes below — not a functional bug. |

✅ 2026-09-28: found and fixed the real cause of "works once then breaks" across every panel — `PanelHost` used to Hide() itself when the last panel closed and Show() itself again on next open; that hide/show cycle on the layered click-through window was corrupting state (caught a visible corrupted-paint artifact as proof). Fixed by never hiding the window at all — shown once at startup, stays shown, an empty canvas already looks/behaves identical to hidden. User-confirmed working across repeated open/close cycles on Ship Loadout Bay and Missions. (Several earlier fixes this same day — click-through timing, ItemsSource refresh, Dispatcher render-flush — were all real but were treating symptoms of this one root cause, not the cause itself.)

🐢 Perf TODO: ship detail pane render lag (see Ship list row above) — worth profiling `ShipDetailView.ShowShip` if it keeps feeling slow, likely the ComboBox-per-slot rows rather than data loading (all catalogs are cached).

## Missions (panel)
| Module | Status | Notes |
|---|---|---|
| Data model + loader | ✅ | Fixed 2026-09-28: crashed on open (`time_to_complete_minutes` is a float for 150/1427 missions, model had it as `int?`) — now `double?`, rounded for display |
| Category + mission list UI | ✅ | Live-verified 2026-09-28 — categories + counts + mission list render |
| Detail pane | ✅ | Live-verified 2026-09-28 — title/giver/reward/time/enemies/systems all populate on click |
| Wired into tool rail + tray | ✅ | Live-verified 2026-09-28 |
| AI mission-explain / video walkthroughs / briefings | ⬜ | Deliberately deferred — network/API-dependent, v1 is pure static-data browse |

⚠️ Data quality (not a crash, cosmetic): some mission titles are unresolved game templates, e.g. `[Contractor|BountyTitleSuper]`, `Green light on [TargetName|Last]` — raw source data, not a app bug. Low priority cleanup: could regex-strip or reword these.

## Blueprints (panel)
| Module | Status | Notes |
|---|---|---|
| Data model + loader | ✅ | 1606 blueprints load fine |
| List + search + detail (craft time, ingredients, sources) | ✅ | Live-verified 2026-09-28 end to end — select a row, ingredients + unlock sources populate correctly |
| Wired into tool rail + tray | ✅ | Live-verified 2026-09-28 |
| Got-it tracking / mission cross-reference (old panel had this) | ⬜ | Deliberately deferred — v1 is browse-only |

⚠️ Data quality (cosmetic, not a bug): a few blueprint names are raw placeholder strings, e.g. `<= PLACEHOLDER =>`. Same class of issue as the Missions template-string note above.

## Weapons DB (panel)
| Module | Status | Notes |
|---|---|---|
| Data extraction (damage/fireRate/DPS computed from erkul-weapons.json) | ✅ | 155 entries |
| List UI (search + DPS sort) | 🔧 | Search-refresh bug fixed 2026-09-28 (same ItemsSource issue as other panels); not yet live-clicked to confirm |
| Wired into tool rail + tray | ✅ | Live-verified 2026-09-28 (opens, doesn't crash) |

## Materials (panel)
| Module | Status | Notes |
|---|---|---|
| Data extraction (26 materials, tier/RS/mineable) | ✅ | |
| List UI (search + RS sort) | 🔧 | Search-refresh bug fixed 2026-09-28; not yet live-clicked to confirm |
| Wired into tool rail + tray | ✅ | Live-verified 2026-09-28 (opens, doesn't crash) |

## Acquisition (panel)
| Module | Status | Notes |
|---|---|---|
| Data (101 components, type + buy locations) | ✅ | Fixed crash 2026-09-28: 4 stray comment strings (`_weapons_doc` etc.) mixed into the items dictionary were crashing the whole app on open |
| List UI (search) | 🔧 | Search-refresh bug fixed 2026-09-28; not yet live-clicked to confirm |
| Wired into tool rail + tray | ✅ | Live-verified 2026-09-28 (opens, doesn't crash) |

## Not started
- **Media/recording suite** — checked the Electron source 2026-09-28: this is NOT a simple video player, it's a full screen-recording + Twitch/YouTube streaming + mic/voice-over + video editing + YouTube/TikTok/X upload-with-OAuth suite (~4500 lines in the Electron version). Treat as its own multi-session project, not a quick port.
- **Live Kit** — checked 2026-09-28: the website's `overlay-live-kit.html` is a UI-only approved mockup with NO real backend; it expects gear/server data via `postMessage` from somewhere that was never built, even in Electron. Porting this means building the Game.log parser from scratch (armor/weapon loadout detection, shard/region tracking) with no reference implementation to verify against — and no way to validate parsing regexes without Star Citizen actually running. Don't build blind; needs either live-game testing access or the real spec.
- admin/live Game.log panel — same live-data-needed caveat as Live Kit.
- Head tracking, voice, license/premium gating — all need live SC/hardware to build against meaningfully.
- Installer, code signing, CI, auto-update — 🔧 **in progress 2026-09-28**:
  - ✅ Self-contained single-file publish config, verified end-to-end (standalone exe runs, no .NET install needed)
  - ✅ Repo created and pushed: `github.com/lerong33-byte/hud-manager-overlay-native` (private)
  - ✅ CI workflow (`.github/workflows/build-release.yml`, manual dispatch): builds + publishes a draft release on this repo, tagged from `HudManagerOverlay.csproj`'s `<Version>` (now `0.1.0`). **Real end-to-end verified, not just "didn't error"** — triggered a live run, downloaded the resulting release asset, extracted it, and launched that exact CI-built exe: it opened correctly and the tool rail worked. First draft release live at tag `v0.1.0`.
  - ✅ **Real installer, via Velopack** (`vpk pack` — installed as a dotnet tool, `Velopack` NuGet package added, `VelopackApp.Build().Run()` wired as the first line of `App()`'s constructor). Verified completely end-to-end, both locally and in CI:
    - Ran the generated `HudManagerOverlay-win-Setup.exe` — it installed to `%LocalAppData%\HudManagerOverlay\current`, created a real desktop shortcut, and registered a proper "Programs and Features" uninstall entry (`Update.exe --uninstall`)
    - Launched the app from that installed copy (not the dev build) and confirmed Ship Loadout Bay opens correctly
    - Updated the CI workflow to build the same installer on a clean `windows-latest` runner — succeeded on the first try, confirming a local snag (below) was dev-machine-specific
    - Also produces the update-feed files (`RELEASES`, `releases.win.json`, the `-full.nupkg`) that auto-update will need
  - ⚠️ **Local packaging hit a real Bitdefender false-positive** on Velopack's generated (unsigned) execution stub — a live real-time-protection block, not a quarantine event (confirmed no matching quarantine entry). Resolved locally with a Bitdefender folder exclusion; did NOT recur on the CI runner (no third-party AV there). **This will affect real end users too** until code signing is in place — an unsigned generic native bootstrapper is a common AV/SmartScreen heuristic target.
  - ⬜ Still open: **code signing** (needs Azure Trusted Signing secrets added to this repo — same service `hud-manager-overlay-src` uses, but scoped per-repo, not shared; this is also the real fix for the AV false-positive above, not just a SmartScreen nicety), **auto-update client logic** (Velopack is wired in and the update feed is published, but nothing in the app calls `UpdateManager.CheckForUpdatesAsync()` yet — it won't actually find or apply updates).
- Visual/graphical polish (intentionally deferred per user)
- Real-hardware DPI verification — investigated 2026-09-28: `ScreenHelper`'s hardcoded `EnvironmentScaleFactor=1.25` was suspected to be a dev-environment artifact, but this machine's registry (`HKCU\Control Panel\Desktop\WindowMetrics\AppliedDPI` = 120, i.e. 125%) shows it's genuinely a 125%-scaled real display — the hack may be masking a real PerMonitorV2 manifest issue rather than compensating for a virtual/remote session as originally suspected. Not resolved; app.manifest already declares `PerMonitorV2` correctly on its face, so the actual runtime DPI behavior of the compiled exe itself (not tools like PowerShell probing it externally) still needs checking before deciding whether to remove the hardcoded factor.

## 2026-09-28 (post-crash catch-up) — installed copy vanishes after install/update
- Applied 0.1.3 via `Update.exe apply`: log says success and hook ran, but afterwards `%LocalAppData%\HudManagerOverlay\current` had no exe and an empty `Data`. Re-running the release's `HudManagerOverlay-win-Setup.exe --silent` then left NO `current` and no `Update.exe` at all (only `packages`). Release assets are healthy (nupkg 74MB, Setup 82MB).
- Hypothesis (NOT verified — couldn't read Bitdefender logs): Bitdefender real-time protection deleting the unsigned exe/Update.exe in the install dir (same false-positive as the packaging one above). Needs the user to check Bitdefender notifications/quarantine or add an exclusion for `%LocalAppData%\HudManagerOverlay`; real fix = code signing.
- Correction: auto-update client logic IS implemented (commit 897f978, update detection+download verified); the "nothing calls CheckForUpdatesAsync" line above is stale.

## 2026-09-28 — signing PARKED (user chose to defer)
- CI workflow now signs via Azure Trusted Signing IF secrets are set (commit 65905bc). The three secrets AZURE_TENANT_ID/CLIENT_ID/CLIENT_SECRET exist on this repo but are EMPTY (set via `gh secret set` with no stdin) — real values must be entered by the user (Azure portal → App registrations; or browser Settings → Secrets → Actions). v0.1.4 built UNSIGNED and was published non-draft by mistake (private repo).
- Bitdefender blocks installer writes (Update.exe / shortcut / uninstall registry key); portable install at C:\HudOverlayTest\current runs fine. Use portable for dev testing until signed.

## 2026-09-28 (late)
- ✅ Radar/life-support raw class ids now display as "Stock Radar"/"Stock Life Support" (only `radr_gnrp_s03_idris_temp` was affected of 32 distinct names). Debug build compiles clean; not live-viewed.
- Weapons DB / Materials / Acquisition search handlers code-reviewed: identical to the verified pattern (ItemsSource reset + case-insensitive Contains). Still not live-clicked.
- Local `dotnet build -c Release` fails (access denied writing bin\...\HudManagerOverlay.exe — Bitdefender blocks the single-file host). Use `-c Debug` locally; CI builds Release fine.

## 2026-09-28 — stopped for user PC restart
Installer/portable extract still denied after AV exclusions (per-process blocking, cause unverified). No installed copy on disk right now; use `dotnet build -c Debug`. Full detail + next steps in Claude memory `RESUME-2026-09-28-native-installer-av.md`.
