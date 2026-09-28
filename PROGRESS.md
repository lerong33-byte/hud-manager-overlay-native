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
- Installer, code signing, CI, auto-update — 🔧 **started 2026-09-28**: self-contained single-file publish config added and verified (`dotnet publish -c Release -r win-x64 --self-contained true` → one working standalone .exe, no .NET install required). Still open: actual installer (MSI/Squirrel/etc.), code signing cert, GitHub Actions CI wiring, auto-update server + client check-in logic.
- Visual/graphical polish (intentionally deferred per user)
- Real-hardware DPI verification — investigated 2026-09-28: `ScreenHelper`'s hardcoded `EnvironmentScaleFactor=1.25` was suspected to be a dev-environment artifact, but this machine's registry (`HKCU\Control Panel\Desktop\WindowMetrics\AppliedDPI` = 120, i.e. 125%) shows it's genuinely a 125%-scaled real display — the hack may be masking a real PerMonitorV2 manifest issue rather than compensating for a virtual/remote session as originally suspected. Not resolved; app.manifest already declares `PerMonitorV2` correctly on its face, so the actual runtime DPI behavior of the compiled exe itself (not tools like PowerShell probing it externally) still needs checking before deciding whether to remove the hardcoded factor.
