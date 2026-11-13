"""Boundary replacements only: engine JSON/Instantiate and OS file services.
The current production origin host, SaveManager, validators and CAS writer run.
"""
from pathlib import Path
import sys

out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True)
stub=Path('Tests~/UnityStub.cs').read_text()
start=stub.index(' public static class JsonUtility {')
def end_block(s,start):
    # JsonUtility is followed by the explicit namespace close and next namespace.
    return s.index('\n}\n',start)
end=end_block(stub,start)
stub=stub[:start]+stub[end:]
needle='return (T)source.MemberwiseClone();'
assert needle in stub
stub=stub.replace(needle,'var clone=(T)source.MemberwiseClone(); var hook=MemoryFiles.CloneHook; MemoryFiles.CloneHook=null; hook?.Invoke(); return clone;',1)
(out/'EngineStub.cs').write_text(stub)
boundary=Path('Tests~/HanVuN3/Boundary.cs').read_text()
needle='    void CommitOriginChange(string narration) { CommitObservations++; }'
assert needle in boundary
(out/'PresentationBoundary.cs').write_text(boundary.replace(needle,''))
host=Path('Assets/Script/GameManager.cs').read_text()
fragment=host[host.index('    internal bool TryOriginInteraction()'):host.index('    private void CallNextWave()')]
stance=host[host.index('    private void CaptureSelectedStance()'):host.index('    private void RestoreSelectedStance()')]
load=host[host.index('    private bool TryLoadSlot('):host.index('    bool ApplyLoadedCandidate(')]
(out/'OriginHost.cs').write_text('using TuTienCore; using UnityEngine;\npublic partial class GameManager {\n'+fragment+stance+load+'}\n')
sources=['SaveManager.cs','SaveManager.Cultivation.cs','SaveManager.CultivationMigration.cs','SaveManager.Tribulation.cs','CultivationMigrationTransaction.cs','CultivationConfigurationUpgrade.cs','TribulationSaveTransaction.cs']
for name in sources:
    source=Path('Assets/Script',name).read_text()
    assert 'System.IO.File.' not in source and 'System.IO.Directory.' not in source
    aliases='using File = MemoryFiles; using FileStream = MemoryFileStream; using Directory = MemoryDirectories;\n'
    (out/name).write_text(aliases+source)
