#!/usr/bin/env python3
"""Validate the shipped Hero frames; optionally compare original upload bytes.
Standard library only. Does not write assets, .meta files, scenes or saves.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile
import zlib


def png_info(data):
    assert data[:8] == b'\x89PNG\r\n\x1a\n', 'PNG signature'
    offset = 8
    ihdr = None
    while offset < len(data):
        length = struct.unpack_from('>I', data, offset)[0]
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:offset + 8 + length]
        crc = struct.unpack_from('>I', data, offset + 8 + length)[0]
        assert zlib.crc32(kind + payload) & 0xffffffff == crc, 'PNG CRC'
        if kind == b'IHDR':
            ihdr = struct.unpack('>IIBBBBB', payload)
        offset += length + 12
        if kind == b'IEND':
            break
    assert offset == len(data) and kind == b'IEND', 'PNG end'
    assert ihdr[:4] == (360, 500, 8, 6), f'Expected 360x500 8-bit RGBA: {ihdr}'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-zip', type=Path)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[1]
    root = repo / 'Assets/Resources'
    folder = root / 'HeroAnimation'
    profile = json.loads((folder / 'SharedHero.json').read_text())
    source = zipfile.ZipFile(args.source_zip) if args.source_zip else None
    expected = []
    for clip, count in [('idle', 48), ('walk', 17), ('run', 11), ('hit', 27)]:
        assert profile[clip + 'Count'] == count, f'{clip} count'
        assert profile[clip + 'Fps'] > 0, f'{clip} FPS'
        prefix = profile[clip + 'Prefix']
        assert prefix.startswith(f'HeroAnimation/sword_{clip}/'), f'{clip} directory'
        paths = [root / f'{prefix}{index:05d}.png' for index in range(count)]
        assert paths == sorted(paths), 'Deterministic numeric order'
        assert set(paths) == set((folder / f'sword_{clip}').glob('*.png')), 'Gaps/extra frames'
        for path in paths:
            data = path.read_bytes()
            png_info(data)
            if source:
                original = source.read('hero/' + path.relative_to(folder).as_posix())
                assert hashlib.sha256(data).digest() == hashlib.sha256(original).digest(), path
        expected += paths
        print(f'PASS {clip}: {count} ordered RGBA PNGs; FPS={profile[clip + "Fps"]}')
    assert set(expected) == set(folder.rglob('*.png')), 'Unexpected PNG assets'
    assert 0 <= profile['feetX'] <= 360 and 0 <= profile['feetY'] <= 500
    assert profile['pixelsPerUnit'] > 0
    if source:
        source.close()
        print('PASS all 103 PNGs byte-identical to hero.zip; archer_run excluded')
    print('PASS profile paths, count/order, canvas, alpha format, PNG CRCs and registration')


if __name__ == '__main__':
    main()
