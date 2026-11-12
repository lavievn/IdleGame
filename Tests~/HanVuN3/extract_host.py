"""Compile the current origin host methods verbatim, without SaveManager/I/O.

The surrounding readiness/lifecycle/presentation services are in Boundary.cs.
No tested origin method is replaced or reimplemented by this extraction.
"""
from pathlib import Path
import sys

source = Path('Assets/Script/GameManager.cs').read_text()
start = source.index('    internal bool TryOriginInteraction()')
end = source.index('    private void CommitOriginChange(', start)
fragment = source[start:end]
assert 'TryChooseHerbs' in fragment and 'TryTalkToWoodcutter' in fragment
Path(sys.argv[1]).write_text('using TuTienCore;\npublic partial class GameManager\n{\n' + fragment + '}\n')
