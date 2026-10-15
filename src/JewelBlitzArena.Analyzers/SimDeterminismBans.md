# Sim determinism ban list (scaffold)

Stable diagnostic IDs for S8:

- `JBA0001` — floating-point type or `System.Math` float API in Sim
- `JBA0002` — non-deterministic time/RNG API in Sim
- `JBA0003` — managed allocation pattern inside `Step` (heuristic)
- `JBA0004` — unstable sort / hash-order iteration
- `JBA0005` — `UnityEngine` reference in Sim or Data

Enforcement: not active in S5. Content export already rejects float authored speeds via
`DecimalFixParser` (decimal strings only).
