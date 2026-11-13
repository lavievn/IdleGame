# Hàn Vụ N3 regression

Run `bash Tests~/HanVuN3/run.sh` with Python3 and a C#7.2+ Mono compiler/runtime.
`CSC`/`MONO` can select existing wrappers. No installation/Editor/home asset
cache is required. Temporary source/binaries are removed on exit; no save I/O.

The runner compiles the real HanVuOrigin, HanVuNarrative, HanVuEncounters,
UIManager.StoryOrigin and related production data classes. It reuses all11
groups from #17 and adds domain and interaction regressions. Its host fragment
is copied **verbatim at test time** from the current GameManager origin methods;
no target handler is rewritten in the fixture. Extraction fails if its anchors
move, so update the extraction deliberately when the host is reorganized.

Boundary.cs supplies only readiness/lifecycle inputs, layout services and a
commit observation sink. Hero replacement and SpawnVersion changes are modeled
inputs, not execution of actual Load/Retry/Travel handlers. Actual story UI
callbacks and host/domain code execute against those inputs. Visual layout,
GameManager orchestration, native lifecycle, SaveManager and durable transaction
behavior are **NOT TESTED**. Commit observations mean an attempted origin
commit, not a successful save. Do not claim native Save/Load PASS.

Before the context guard,26 groups produce20 PASS/6 FAIL; all six stale-context
regressions fail on the actual #17 interaction logic. The original standalone
data runner `Tests~/run-hanvu-story.sh` remains unchanged and passes11 groups.

Final runner:30 PASS/0 FAIL, including19 new groups. A second reproduction
retains an old choice delegate after a fresh conversation opens: the first
guard alone gives28 PASS/2 FAIL. Callbacks must capture the individual intent,
not read the mutable context of a future view. Binding uses the existing
BindClick path; presentation doubles model event binding but do not test native
input or renderer behavior.

Reproduce with `bash Tests~/HanVuN3/baseline.sh original` or `retained` (both
expected exit1). They extract pinned source into scratch, without switching
branches or merging. The retained probe uses the current boundary/regression
against runtime checkpoint1df0d46. The Qi case executes the real cultivation
binding with an explicit test profile, not a replacement Qi formula. Weather
runtime #16 is absent from this base and is not integration-tested here.
