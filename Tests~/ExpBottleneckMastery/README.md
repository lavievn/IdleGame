# EXP bottleneck & Mastery data regression

Run `bash Tests~/ExpBottleneckMastery/run.sh` from the repository. `CSC` and
`MONO` may select installed compiler/runtime wrappers. Requires Mono with
System.Web.Extensions and System.Numerics. No dependency installation or Unity
Editor is required. Scratch binaries use a temporary directory and are removed.

The runner compiles real EntityDataSO, EXP component, cultivation, equipment,
skill registry and Mastery receipt code. It reuses 29 PR #19 groups and adds 21
groups for this task. Engine/JSON services use the existing UnityStub; the
environment presentation boundary throws if reached. Mastery DTO roundtrip uses
.NET JavaScriptSerializer. No target progression formula is mocked.

PASS here means data API behavior only. It does **not** establish native
JsonUtility, SaveManager file transactions, MainGame callbacks, Retry/death,
Windows Player or Mastery progression. No save I/O is executed. See the report
for the Unity acceptance checklist and pre-fix characterization.
