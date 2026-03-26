// AudioCueService — Web Audio API synthetic tones for agent events
// No audio files needed. All sounds are generated programmatically.

let audioCtx = null;

function getAudioContext() {
    if (!audioCtx || audioCtx.state === 'closed') {
        audioCtx = new AudioContext();
    }
    if (audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
    return audioCtx;
}

/**
 * Play a frequency sweep (rising or descending tone).
 * @param {number} startFreq - Starting frequency in Hz
 * @param {number} endFreq - Ending frequency in Hz
 * @param {number} duration - Duration in seconds
 * @param {string} type - Oscillator type: 'sine', 'triangle', 'square', 'sawtooth'
 * @param {number} volume - Gain value (0.0 - 1.0)
 */
export function playSweep(startFreq, endFreq, duration, type = 'sine', volume = 0.1) {
    if (isMuted()) return;

    const ctx = getAudioContext();
    const now = ctx.currentTime;

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(startFreq, now);
    osc.frequency.linearRampToValueAtTime(endFreq, now + duration);

    gain.gain.setValueAtTime(volume, now);
    // Fade out in the last 30% to avoid clicks
    gain.gain.setValueAtTime(volume, now + duration * 0.7);
    gain.gain.linearRampToValueAtTime(0, now + duration);

    osc.connect(gain);
    gain.connect(ctx.destination);

    osc.start(now);
    osc.stop(now + duration);
}

/**
 * Play a sequence of notes (chime / arpeggio).
 * @param {number[]} frequencies - Array of frequencies in Hz
 * @param {number} noteDuration - Duration of each note in seconds
 * @param {number} volume - Gain value (0.0 - 1.0)
 * @param {string} type - Oscillator type
 */
export function playChime(frequencies, noteDuration, volume = 0.1, type = 'sine') {
    if (isMuted()) return;

    const ctx = getAudioContext();
    const now = ctx.currentTime;

    frequencies.forEach((freq, i) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();

        osc.type = type;
        osc.frequency.setValueAtTime(freq, now);

        const noteStart = now + i * noteDuration;
        gain.gain.setValueAtTime(0, noteStart);
        gain.gain.linearRampToValueAtTime(volume, noteStart + 0.01);
        gain.gain.setValueAtTime(volume, noteStart + noteDuration * 0.7);
        gain.gain.linearRampToValueAtTime(0, noteStart + noteDuration);

        osc.connect(gain);
        gain.connect(ctx.destination);

        osc.start(noteStart);
        osc.stop(noteStart + noteDuration);
    });
}

/**
 * Play a chord (multiple notes simultaneously).
 * @param {number[]} frequencies - Array of frequencies to play together
 * @param {number} duration - Duration in seconds
 * @param {number} volume - Per-note gain (will be divided among notes)
 * @param {string} type - Oscillator type
 */
export function playChord(frequencies, duration, volume = 0.1, type = 'sine') {
    if (isMuted()) return;

    const ctx = getAudioContext();
    const now = ctx.currentTime;
    const perNoteVolume = volume / frequencies.length;

    frequencies.forEach((freq) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();

        osc.type = type;
        osc.frequency.setValueAtTime(freq, now);

        gain.gain.setValueAtTime(0, now);
        gain.gain.linearRampToValueAtTime(perNoteVolume, now + 0.015);
        gain.gain.setValueAtTime(perNoteVolume, now + duration * 0.6);
        gain.gain.linearRampToValueAtTime(0, now + duration);

        osc.connect(gain);
        gain.connect(ctx.destination);

        osc.start(now);
        osc.stop(now + duration);
    });
}

/** Check if audio is muted via localStorage. */
export function isMuted() {
    try {
        return localStorage.getItem('squad-audio-muted') !== 'false';
    } catch {
        return true; // default muted if localStorage unavailable
    }
}

/** Set mute state in localStorage. */
export function setMuted(muted) {
    try {
        localStorage.setItem('squad-audio-muted', muted ? 'true' : 'false');
    } catch {
        // localStorage unavailable — ignore
    }
}
