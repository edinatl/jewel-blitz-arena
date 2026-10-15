# JewelBlitzArena.Analyzers (S5 scaffold / S8 enforcement)

S5 lands the **ban list and project placeholder**. S8 owns turning this into a Roslyn analyzer
that fails the build when `JewelBlitzArena.Sim` (and later `Data`) uses forbidden APIs.

## Banned in Sim (architecture §2.2)

| Banned | Use instead |
| --- | --- |
| `float`, `double`, `decimal`, floating `System.Math` | `Fix` (Q48.16) / Sim math |
| `System.Random`, `DateTime`, `Stopwatch`, `Environment.TickCount` | Sim RNG, frame counter |
| `Dictionary` / `HashSet` iteration in Step | Flat arrays by slot |
| `string.GetHashCode()` | Registry ids |
| Unstable sorts / LINQ `OrderBy` | Sim stable sort |
| Threads / `Parallel` inside `Step` | Single-threaded Step |
| Managed allocation inside `Step` | Fixed pools in `MatchState` |
| Float → `Fix` inside Sim | Convert at data export (`DecimalFixParser`) |

See also `SimDeterminismBans.md`.
