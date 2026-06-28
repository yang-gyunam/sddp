/**
 * Microphone audio recorder for speech-to-text input.
 * Wraps MediaRecorder with a max-duration countdown and cancel support.
 */

const MIME_CANDIDATES = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4'];

const DEFAULT_MAX_DURATION_MS = 60_000;
const TICK_INTERVAL_MS = 250;
const LEVEL_INTERVAL_MS = 100;

export interface AudioRecorderOptions {
  /** Maximum recording duration in ms (default 60,000). Recording auto-stops when reached. */
  maxDurationMs?: number;
  /** Called ~4 times per second with the remaining time in ms. */
  onTick?: (remainingMs: number) => void;
  /**
   * Called ~10 times per second with the current input level (0..1).
   * Never called when Web Audio (AudioContext) is unavailable.
   */
  onLevel?: (level: number) => void;
  /** Called with the finalized audio blob after stop() or auto-stop. Not called after cancel(). */
  onStop: (blob: Blob, mimeType: string) => void;
}

export interface AudioRecorderHandle {
  readonly mimeType: string;
  /** Finalize the recording — triggers onStop with the collected audio. */
  stop(): void;
  /** Discard the recording — onStop is never called. */
  cancel(): void;
}

/**
 * Pick the first MediaRecorder-supported mime type
 * (webm/opus → webm → mp4 for Safari). Returns null when recording is unsupported.
 */
export function pickSupportedMimeType(): string | null {
  if (typeof MediaRecorder === 'undefined' || typeof MediaRecorder.isTypeSupported !== 'function') {
    return null;
  }
  return MIME_CANDIDATES.find((mime) => MediaRecorder.isTypeSupported(mime)) ?? null;
}

/**
 * Whether this browser can record audio at all.
 * False when MediaRecorder is missing or `navigator.mediaDevices` is unavailable —
 * the latter happens on insecure (non-HTTPS, non-localhost) origins.
 */
export function isRecordingSupported(): boolean {
  return (
    typeof navigator !== 'undefined' &&
    typeof navigator.mediaDevices?.getUserMedia === 'function' &&
    pickSupportedMimeType() !== null
  );
}

/**
 * Sample the stream's input level via an AnalyserNode and report it through onLevel.
 * Returns a teardown function, or null when Web Audio is unavailable (level metering
 * is a progressive enhancement — recording itself works without it).
 */
function createLevelMeter(stream: MediaStream, onLevel: (level: number) => void): (() => void) | null {
  const AudioContextCtor =
    typeof AudioContext !== 'undefined'
      ? AudioContext
      : (globalThis as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!AudioContextCtor) return null;

  try {
    const audioContext = new AudioContextCtor();
    const source = audioContext.createMediaStreamSource(stream);
    const analyser = audioContext.createAnalyser();
    analyser.fftSize = 256;
    source.connect(analyser);
    const samples = new Uint8Array(analyser.frequencyBinCount);

    const timer = setInterval(() => {
      analyser.getByteTimeDomainData(samples);
      let sumSquares = 0;
      for (const sample of samples) {
        const v = (sample - 128) / 128;
        sumSquares += v * v;
      }
      // Speech RMS sits well below full scale — boost so normal speech fills the meter
      onLevel(Math.min(1, Math.sqrt(sumSquares / samples.length) * 4));
    }, LEVEL_INTERVAL_MS);

    return () => {
      clearInterval(timer);
      source.disconnect();
      void audioContext.close();
    };
  } catch {
    return null;
  }
}

/**
 * Request microphone access and start recording.
 * Rejects with the getUserMedia error (e.g. NotAllowedError) when access is denied.
 */
export async function startAudioRecorder(options: AudioRecorderOptions): Promise<AudioRecorderHandle> {
  const mimeType = pickSupportedMimeType();
  if (!mimeType) {
    throw new Error('Audio recording is not supported in this browser');
  }

  const maxDurationMs = options.maxDurationMs ?? DEFAULT_MAX_DURATION_MS;
  const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
  const recorder = new MediaRecorder(stream, { mimeType });
  const chunks: Blob[] = [];
  let cancelled = false;
  let tickTimer: ReturnType<typeof setInterval> | null = null;
  const stopLevelMeter = options.onLevel ? createLevelMeter(stream, options.onLevel) : null;

  const startedAt = Date.now();

  function cleanup(): void {
    if (tickTimer !== null) {
      clearInterval(tickTimer);
      tickTimer = null;
    }
    stopLevelMeter?.();
    stream.getTracks().forEach((track) => track.stop());
  }

  recorder.ondataavailable = (event: BlobEvent) => {
    if (event.data.size > 0) {
      chunks.push(event.data);
    }
  };

  recorder.onstop = () => {
    cleanup();
    if (!cancelled) {
      options.onStop(new Blob(chunks, { type: mimeType }), mimeType);
    }
  };

  recorder.start();

  tickTimer = setInterval(() => {
    const remaining = maxDurationMs - (Date.now() - startedAt);
    if (remaining <= 0) {
      options.onTick?.(0);
      if (recorder.state !== 'inactive') {
        recorder.stop();
      }
      return;
    }
    options.onTick?.(remaining);
  }, TICK_INTERVAL_MS);

  return {
    mimeType,
    stop(): void {
      if (recorder.state !== 'inactive') {
        recorder.stop();
      }
    },
    cancel(): void {
      cancelled = true;
      if (recorder.state !== 'inactive') {
        recorder.stop();
      } else {
        cleanup();
      }
    },
  };
}
