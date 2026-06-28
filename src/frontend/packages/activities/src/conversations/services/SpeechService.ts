/**
 * Speech-to-Text Service
 * Uploads recorded audio to the backend transcription endpoint
 * and checks STT feature availability.
 */

import { fetchWithAuth } from '../../shared/api';
import { config as appConfig } from '@sddp/shell/core';
import { isRecordingSupported } from './audioRecorder';

interface TranscriptionResult {
  text: string;
}

interface SpeechStatus {
  enabled: boolean;
}

/**
 * Transcribe a recorded audio blob. Returns the raw transcript text.
 */
export async function transcribeAudio(blob: Blob, mimeType: string): Promise<string> {
  const ext = mimeType.includes('mp4') ? 'mp4' : mimeType.includes('ogg') ? 'ogg' : 'webm';
  const formData = new FormData();
  formData.append('audio', blob, `recording.${ext}`);

  const result = await fetchWithAuth<TranscriptionResult>('/speech/transcriptions', {
    method: 'POST',
    formData,
  });
  return result?.text ?? '';
}

/** Cached availability check — feature flag + backend status + browser support. */
let speechAvailablePromise: Promise<boolean> | null = null;

/**
 * Whether speech-to-text input can be offered to the user.
 * False when the VITE_ENABLE_SPEECH_TO_TEXT flag is off, the backend
 * feature flag is disabled, or the browser cannot record audio
 * (no MediaRecorder, or insecure origin without `navigator.mediaDevices`).
 */
export function isSpeechToTextAvailable(): Promise<boolean> {
  if (!appConfig.get('enableSpeechToText') || !isRecordingSupported()) {
    return Promise.resolve(false);
  }

  if (!speechAvailablePromise) {
    speechAvailablePromise = fetchWithAuth<SpeechStatus>('/speech/status')
      .then((status) => status?.enabled ?? false)
      .catch(() => false);
  }
  return speechAvailablePromise;
}

/** Test-only: reset the cached availability result. */
export function resetSpeechAvailabilityCache(): void {
  speechAvailablePromise = null;
}
