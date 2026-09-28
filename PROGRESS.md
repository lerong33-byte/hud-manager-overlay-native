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
| Weapon mounts — swap + save | ✅ | |
| Turret/manned-turret weapon rows | 🔧 | Algorithm validated against all 126 turret ships in ships.json (0 null-size risks) + exact row/label output confirmed for Idris-M (55 rows); still no live UI click |
| Missile racks — swap + save | ✅ | |
| Components: power plant/shield/cooler/qdrive — swap + save | ✅ | |
| Components: flight controller — swap + save | 🔧 | Just built, not live-clicked |
| Radar / life support — read-only display | 🔧 | Just built, not live-clicked |

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
- Every other Electron panel: media, Live Kit, admin/live Game.log, etc.
- Head tracking, voice, license/premium gating
- Installer, code signing, CI, auto-update
- Visual/graphical polish (intentionally deferred per user)
- Real-hardware DPI verification (currently hardcoded EnvironmentScaleFactor=1.25 guess)
