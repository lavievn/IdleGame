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

Before production changes: **15 PASS/8 FAIL**, including the 11 existing story/world
groups and 12 N4 groups. After fixes and added coverage: **34 PASS/0 FAIL**
(11 existing groups + 23 N4 groups). Assertions in stream hooks only collect observations;
they are checked after Save returns, so the writer's catch cannot swallow a
failed test assertion. The N3 suite is run separately without changing its
assertions or turning its commit sink into a native Save claim. Its test boundary acknowledges the candidate write,
while its original commit-count and stale-context assertions remain unchanged.

The post-write context-change test explicitly demonstrates a limit: if an injected
callback changes context inside the otherwise synchronous writer, the old
candidate is never applied to RAM, but the file can already be committed. This
is not a global atomicity/exactly-once claim. Automatic arrival/wave/clue tests
execute the production domain mutation through the real transaction helper;
they do not execute the native actor/death/exploration lifecycle.
