"""Numerical PCM audit only. Does not listen, rate music, or modify audio."""
import argparse
import hashlib
import json
from pathlib import Path
import wave

import numpy as np


def audit(project):
    manifest = json.loads((project / 'Assets/_LASTRAIL/UI/Style/lastrail-audio-resources.json').read_text())
    rows = []
    for spec in manifest['clips']:
        path = project / spec['path']
        with wave.open(str(path), 'rb') as stream:
            assert stream.getsampwidth() == 2 and stream.getnchannels() == 1
            rate = stream.getframerate()
            data = stream.readframes(stream.getnframes())
        pcm = np.frombuffer(data, dtype='<i2')
        x = pcm.astype(np.float64) / 32768
        peak, rms = np.max(np.abs(x)), np.sqrt(np.mean(x*x))
        energy = np.abs(np.fft.rfft(x))**2
        frequencies = np.fft.rfftfreq(len(x), 1/rate)
        rms_window = max(1, rate // 100)
        windows = x[:len(x)//rms_window*rms_window].reshape(-1, rms_window)
        row = dict(id=spec['id'], category=spec['category'], seconds=len(x)/rate,
                   sampleRate=rate, frames=len(x), sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                   peakDbfs=float(20*np.log10(peak)), rmsDbfs=float(20*np.log10(rms)),
                   crestDb=float(20*np.log10(peak/rms)), dc=float(x.mean()),
                   clippedSamples=int(np.count_nonzero(np.abs(pcm.astype(int)) >= 32767)),
                   silentWindowFraction=float(np.mean(np.sqrt(np.mean(windows*windows, axis=1)) < 0.001)),
                   endpointStep=float(abs(x[-1]-x[0])), maxAdjacentStep=float(np.max(np.abs(np.diff(x)))),
                   dominantHz=float(frequencies[np.argmax(energy[1:])+1]),
                   energyCentroidHz=float(np.sum(energy*frequencies)/energy.sum()),
                   sub20HzEnergyFraction=float(energy[frequencies < 20].sum()/energy.sum()))
        if spec['loop']:
            row['loopSlopeMismatch'] = float(abs((x[0]-x[-1])-(x[1]-x[0])))
            lag = rate // 5
            row['repeatCorrelation200ms'] = float(np.corrcoef(x[:-lag], x[lag:])[0, 1])
        rows.append(row)
    return dict(listened=False, qualityVerdict='technical_measurements_only', clips=rows)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = audit(Path(__file__).resolve().parents[1])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    for row in result['clips']:
        print(f"{row['id']:28} peak={row['peakDbfs']:6.2f} RMS={row['rmsDbfs']:6.2f} DC={row['dc']:+.6f} sub20={row['sub20HzEnergyFraction']:.5f}")
