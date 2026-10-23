"""Extract the uploaded MP3s using embedded ID3 CHAP regions, not guessed cuts."""
import csv
import hashlib
import io
import json
from pathlib import Path
import subprocess
import tempfile
import uuid
import wave
import zipfile

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "SourceAssets~/W22l/sfx.zip"
OUTPUT = ROOT / "Assets/Resources/CombatSfx"
RATE = 48000


def metadata(path, folder=False):
    p = Path(str(path) + ".meta")
    if p.exists():
        return
    text = "fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\n"
    if folder:
        text += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
    else:
        text += """AudioImporter:
  serializedVersion: 8
  externalObjects: {}
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 48000
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {}
  forceToMono: 0
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 0
"""
    p.write_text(text)


def main():
    OUTPUT.mkdir(exist_ok=True)
    metadata(OUTPUT, True)
    report = []
    hashes = {}
    with zipfile.ZipFile(SOURCE) as archive:
        for name in sorted(archive.namelist()):
            if not name.endswith(".mp3"):
                continue
            if Path(name).name != name or "\\" in name:
                raise ValueError("Unexpected archive path: " + name)
            raw = archive.read(name)
            # ffprobe receives the original bytes: ID3 markers are not stripped.
            # Seekable temporary input preserves MP3 gapless/end-padding info.
            with tempfile.TemporaryDirectory(prefix="idlegame-sfx-") as temporary:
                original=Path(temporary)/name
                original.write_bytes(raw)
                probe = json.loads(subprocess.check_output([
                    "ffprobe", "-v", "error", "-show_format", "-show_streams",
                    "-show_chapters", "-of", "json", str(original)]))
                decoded = subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(original),
                    "-f", "f32le", "-acodec", "pcm_f32le", "pipe:1"])
            stream = probe["streams"][0]
            assert int(stream["sample_rate"]) == RATE and stream["channels"] == 2
            samples = np.frombuffer(decoded, np.float32).reshape(-1, 2)
            chapters = probe["chapters"]
            regions = chapters or [dict(start_time=0, end_time=len(samples)/RATE, tags={"title":"single"})]
            group = "Shoot" if name.startswith("shoot") else "Magic" if name.startswith("magic") else "Sword"
            folder = OUTPUT/group
            folder.mkdir(exist_ok=True);metadata(folder, True)
            clips = []
            for index, region in enumerate(regions):
                begin = round(float(region["start_time"])*RATE)
                end = min(len(samples), round(float(region["end_time"])*RATE))
                section = samples[begin:end]
                activity = np.flatnonzero(np.max(np.abs(section), axis=1) > 10**(-70/20))
                if not len(activity):
                    raise ValueError("Empty/near-silent region: " + name + str(index))
                # Trim ONLY inside the original region: preserve 10ms pre-roll
                # and 80ms tail beyond the last -70dBFS sample. No normalization.
                left = max(0, int(activity[0]) - 480)
                right = min(len(section), int(activity[-1]) + 1 + 3840)
                clip = section[left:right].copy()
                fades = []
                for side in ["start", "end"]:
                    edge = clip[0] if side == "start" else clip[-1]
                    if np.max(np.abs(edge)) > .0001:
                        ramp = np.linspace(0, 1, min(48,len(clip)), dtype=np.float32)[:,None]
                        if side == "start": clip[:len(ramp)] *= ramp
                        else: clip[-len(ramp):] *= ramp[::-1]
                        fades.append(side+":1ms")
                assert np.max(np.abs(clip)) < 1, "Source clips; do not silently normalize"
                pcm = np.round(clip * 32767).astype("<i2").tobytes()
                digest = hashlib.sha256(pcm).hexdigest()
                duplicate = hashes.get(digest)
                label = Path(name).stem + (f"_ch{index:02}" if chapters else "")
                dest = folder/(label+".wav")
                with wave.open(str(dest),"wb") as wav:
                    wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(RATE);wav.writeframes(pcm)
                metadata(dest);hashes[digest] = str(dest.relative_to(ROOT))
                clips.append(dict(marker=region.get("tags",{}).get("title"),
                    marker_start=float(region["start_time"]), marker_end=float(region["end_time"]),
                    actual_start=(begin+left)/RATE, actual_end=(begin+right)/RATE,
                    duration=len(clip)/RATE, fade=fades, peak_dbfs=float(20*np.log10(np.max(np.abs(clip)))),
                    duplicate_of=duplicate, pcm_sha256=digest, output=str(dest.relative_to(ROOT))))
            report.append(dict(source=name, source_sha256=hashlib.sha256(raw).hexdigest(),
                codec=stream["codec_name"],sample_rate=RATE,channels=2,
                container_duration=float(probe["format"]["duration"]), decoded_duration=len(samples)/RATE,
                markers="embedded ID3 CHAP via ffprobe" if chapters else "none; standalone sound",
                id3_chap_count=raw.count(b"CHAP"),group=group,clips=clips))
    (SOURCE.parent/"sfx-audit.json").write_text(json.dumps(report,indent=2)+"\n")
    with (SOURCE.parent/"sfx-cuts.csv").open("w",newline="") as out:
        writer=csv.writer(out,lineterminator="\n");writer.writerow(["source","marker","region_start","region_end","actual_start","actual_end","output","fade"])
        for item in report:
            for clip in item["clips"]:
                writer.writerow([item["source"],clip["marker"],clip["marker_start"],clip["marker_end"],
                    clip["actual_start"],clip["actual_end"],clip["output"],";".join(clip["fade"])])
    print([(item["source"],len(item["clips"])) for item in report])


if __name__ == "__main__":
    main()
