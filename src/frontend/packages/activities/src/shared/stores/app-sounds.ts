import { get, writable } from 'svelte/store';

const prefs = writable({ enabled: false, clicks: false, reading: null as string | null });
export const appSounds = { subscribe: prefs.subscribe };

let storageKey: string | undefined;
let audioContext: AudioContext | undefined;
let reading: { id: string; utterance?: SpeechSynthesisUtterance } | undefined;

export function setAppSoundUser(user: string | undefined): void {
  const key = user ? `sddp.app-sounds.${user}` : undefined;
  if (key === storageKey) return;
  stopReading();
  storageKey = key;
  let enabled = false;
  let clicks = false;
  if (key) {
    try {
      const saved = localStorage.getItem(key);
      enabled = saved === 'on' || saved === 'muted';
      clicks = saved === 'on';
    } catch {
      // Storage can be disabled by the browser.
    }
  }
  prefs.set({ enabled, clicks, reading: null });
  if (enabled) window.speechSynthesis?.getVoices();
}

function savePreferences(enabled: boolean, clicks: boolean): void {
  if (!storageKey) return;
  if (!enabled) stopReading();
  prefs.update((current) => ({ ...current, enabled, clicks }));
  try {
    localStorage.setItem(storageKey, enabled ? (clicks ? 'on' : 'muted') : 'off');
  } catch {
    // The change still applies in this tab.
  }
}

export function setAppSoundsEnabled(enabled: boolean): void {
  savePreferences(enabled, enabled);
  if (enabled) {
    window.speechSynthesis?.getVoices();
    playClickSound();
  }
}

export function toggleClickSounds(): void {
  const { enabled, clicks } = get(prefs);
  if (!enabled) return;
  savePreferences(true, !clicks);
  if (!clicks) playClickSound();
}

export function playClickSound(): void {
  const { enabled, clicks } = get(prefs);
  if (!enabled || !clicks || typeof AudioContext === 'undefined') return;
  const owner = storageKey;
  try {
    const context = (audioContext ??= new AudioContext());
    function play() {
      const current = get(prefs);
      if (owner !== storageKey || !current.enabled || !current.clicks) return;
      const at = context.currentTime;
      const oscillator = context.createOscillator();
      const volume = context.createGain();
      oscillator.type = 'triangle';
      oscillator.frequency.setValueAtTime(730, at);
      oscillator.frequency.exponentialRampToValueAtTime(340, at + 0.042);
      volume.gain.setValueAtTime(0.001, at);
      volume.gain.exponentialRampToValueAtTime(0.075, at + 0.006);
      volume.gain.exponentialRampToValueAtTime(0.001, at + 0.055);
      oscillator.connect(volume).connect(context.destination);
      oscillator.start(at);
      oscillator.stop(at + 0.06);
    }
    if (context.state === 'running') play();
    else
      void context
        .resume()
        .then(play)
        .catch(() => {});
  } catch {
    // Audio feedback is optional for the action that triggered it.
  }
}

export function stopReading(id?: string): void {
  if (!reading || (id && reading.id !== id)) return;
  reading = undefined;
  prefs.update((current) => ({ ...current, reading: null }));
  window.speechSynthesis.cancel();
}

export function toggleReading(
  id: string,
  getText: () => string,
  onError: (message: string) => void
): void {
  if (!get(prefs).enabled) return;
  if (reading?.id === id) {
    stopReading(id);
    return;
  }
  const synthesis = window.speechSynthesis;
  const voices = synthesis?.getVoices().filter((voice) => voice.localService) ?? [];
  const fallbackVoice = voices.find((voice) => voice.default) ?? voices[0];
  if (!fallbackVoice) {
    onError('Read aloud requires an on-device voice in this browser.');
    return;
  }
  const text = getText().trim();
  if (!text) {
    onError('There is no text to read aloud.');
    return;
  }
  const browserLanguage = navigator.language.split('-')[0];
  const language = /\p{Script=Hangul}/u.test(text)
    ? 'ko'
    : browserLanguage === 'ko'
      ? 'en'
      : browserLanguage;
  const matched = voices.filter((voice) => voice.lang.split('-')[0] === language);
  const voice = matched.find((voice) => voice.default) ?? matched[0] ?? fallbackVoice;
  const chunks = text.match(/[^\s][\s\S]{0,199}(?:\s|$)|[^\s][\s\S]{0,199}/gu) ?? [];

  stopReading();
  const session: NonNullable<typeof reading> = { id };
  reading = session;
  prefs.update((current) => ({ ...current, reading: id }));

  function finish() {
    if (reading !== session) return;
    reading = undefined;
    prefs.update((current) => ({ ...current, reading: null }));
  }

  function fail() {
    if (reading !== session) return;
    finish();
    onError('Could not read this content aloud.');
  }

  function speak(index: number) {
    if (reading !== session) return;
    if (index === chunks.length) {
      finish();
      return;
    }
    try {
      const utterance = new SpeechSynthesisUtterance(chunks[index]);
      session.utterance = utterance;
      utterance.voice = voice;
      utterance.lang = voice.lang;
      utterance.onend = () => speak(index + 1);
      utterance.onerror = fail;
      synthesis.speak(utterance);
    } catch {
      fail();
    }
  }

  speak(0);
}
