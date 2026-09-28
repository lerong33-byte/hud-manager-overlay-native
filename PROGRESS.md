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
| Ship list + search | ✅ | |
| Stats display (speed/hull/shield/weaponry) | ✅ | |
| Weapon mounts — swap + save | ✅ | |
| Turret/manned-turret weapon rows | 🔧 | Algorithm validated against all 126 turret ships in ships.json (0 null-size risks) + exact row/label output confirmed for Idris-M (55 rows); still no live UI click |
| Missile racks — swap + save | ✅ | |
| Components: power plant/shield/cooler/qdrive — swap + save | ✅ | |
| Components: flight controller — swap + save | 🔧 | Just built, not live-clicked |
| Radar / life support — read-only display | 🔧 | Just built, not live-clicked |

## Missions (panel)
| Module | Status | Notes |
|---|---|---|
| Data model + loader | 🔧 | Loads 1427 missions, app stays up (smoke-tested) |
| Category + mission list UI | 🔧 | Built, not live-clicked |
| Detail pane | 🔧 | Built, not live-clicked |
| Wired into tool rail + tray | 🔧 | Built, not live-clicked |
| AI mission-explain / video walkthroughs / briefings | ⬜ | Deliberately deferred — network/API-dependent, v1 is pure static-data browse |

## Blueprints (panel)
| Module | Status | Notes |
|---|---|---|
| Data model + loader | 🔧 | Loads 1606 blueprints, app stays up (smoke-tested) |
| List + search + detail (craft time, ingredients, sources) | 🔧 | Real ingredient list from crafting-enriched.json wired in; not live-clicked |
| Wired into tool rail + tray | 🔧 | Built, not live-clicked |
| Got-it tracking / mission cross-reference (old panel had this) | ⬜ | Deliberately deferred — v1 is browse-only |

## Weapons DB (panel)
| Module | Status | Notes |
|---|---|---|
| Data extraction (damage/fireRate/DPS computed from erkul-weapons.json) | 🔧 | 155 entries |
| List UI (search + DPS sort) | 🔧 | Built, not live-clicked |
| Wired into tool rail + tray | 🔧 | Built, not live-clicked |

## Materials (panel)
| Module | Status | Notes |
|---|---|---|
| Data extraction (26 materials, tier/RS/mineable) | 🔧 | |
| List UI (search + RS sort) | 🔧 | Built, not live-clicked |
| Wired into tool rail + tray | 🔧 | Built, not live-clicked |

## Acquisition (panel)
| Module | Status | Notes |
|---|---|---|
| Data (101 components, type + buy locations) | 🔧 | |
| List UI (search) | 🔧 | Built, not live-clicked |
| Wired into tool rail + tray | 🔧 | Built, not live-clicked |

## Not started
- Every other Electron panel: media, Live Kit, admin/live Game.log, etc.
- Head tracking, voice, license/premium gating
- Installer, code signing, CI, auto-update
- Visual/graphical polish (intentionally deferred per user)
- Real-hardware DPI verification (currently hardcoded EnvironmentScaleFactor=1.25 guess)
