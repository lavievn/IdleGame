# Hàn Vụ N4 persistence boundary regression

Run `bash Tests~/HanVuN4/run.sh` with Python3, a C#7.2+ compiler, Mono and
System.Web.Extensions. CSC/MONO may point to existing wrappers.

The current production origin host methods, SaveManager.SaveGame, load validators,
WriteToFile and CultivationMigrationTransaction.ReplaceExpected execute. The
runner extracts host methods verbatim and adds I/O aliases to temporary copies
of persistence sources. Engine JSON/Instantiate and OS files/streams/directories
are boundary services in this fixture. No production transition, validation,
write/CAS algorithm is replaced by a successful mock. Tests never call native
Save directories; all save paths refer to a dictionary of bytes.

This is **not** Unity JsonUtility, native cloning, fsync, Windows file sharing,
actual Load/Retry/Travel scene orchestration or crash recovery. GameManager's
Load candidate decision runs; its final scene publication and cultivation setup
are modeled boundaries. Do not label these results native Save/Load PASS.

Before production changes:15 PASS/8 FAIL, including the11 existing story/world
groups and12 N4 groups. Assertions in stream hooks only collect observations;
they are checked after Save returns, so the writer's catch cannot swallow a
failed test assertion. The N3 suite is run separately without changing its
assertions or turning its commit sink into a native Save claim.
