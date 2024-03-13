using System.Collections;
using UnityEditor;
using UnityEngine;

public class SoundController : Singleton<SoundController> {
    [NonNullField] public AudioSource MenuMusic;
    [NonNullField] public AudioSource WarningSound;
    [NonNullField] public AudioSource GameMusic1;
    [NonNullField] public AudioSource GameMusic2;
    public float Volume = 0.25f;

    public void PlayMenuMusic(bool shouldPlay, bool shouldFadeIn = false) {
        SetPlayAudioSource(MenuMusic, shouldPlay, shouldFadeIn);
    }

    public void PlayWarningSound(bool shouldPlay, bool shouldFadeIn = false) {
        SetPlayAudioSource(WarningSound, shouldPlay, shouldFadeIn);
    }

    public void PlayGameMusic1(bool shouldPlay, bool shouldFadeIn = false) {
        SetPlayAudioSource(GameMusic1, shouldPlay, shouldFadeIn);
    }

    public void PlayGameMusic2(bool shouldPlay, bool shouldFadeIn = false) {
        SetPlayAudioSource(GameMusic2, shouldPlay, shouldFadeIn);
    }

    public void StopAllTracks() {
        if (MenuMusic.isPlaying) {
            SetPlayAudioSource(MenuMusic, false);
        }

        if (GameMusic1.isPlaying) {
            SetPlayAudioSource(GameMusic1, false);
        }

        if (GameMusic2.isPlaying) {
            SetPlayAudioSource(GameMusic2, false);
        }
    }

    private bool _menuMusicPaused = false;
    private bool _gameMusic1Paused = false;
    private bool _gameMusic2Paused = false;

    public void PauseAllTracks(bool pause) {
        if (pause) {
            if (MenuMusic.isPlaying) {
                MenuMusic.Pause();
                _menuMusicPaused = true;
            }

            if (GameMusic1.isPlaying) {
                GameMusic1.Pause();
                _gameMusic1Paused = true;
            }

            if (GameMusic2.isPlaying) {
                GameMusic2.Pause();
                _gameMusic2Paused = true;
            }
        } else {
            if (_menuMusicPaused) {
                MenuMusic.UnPause();
                _menuMusicPaused = false;
            }

            if (_gameMusic1Paused) {
                GameMusic1.UnPause();
                _gameMusic1Paused = false;
            }

            if (_gameMusic2Paused) {
                GameMusic2.UnPause();
                _gameMusic2Paused = false;
            }
        }
    }

    private void SetPlayAudioSource(AudioSource source, bool shouldPlay, bool shouldFadeIn = false) {
        if (shouldPlay) {
            if (!source.isPlaying) {
                if (shouldFadeIn) {
                    source.volume = 0;
                    StartCoroutine(StartFade(source, 1.0f, Volume));
                    source.Play();
                } else {
                    source.volume = Volume;
                    source.Play();
                }
            }
        } else {
            StartCoroutine(StartFade(source, 1.0f, 0));
        }
    }

    public static IEnumerator StartFade(AudioSource audioSource, float duration, float targetVolume) {
        bool isFadeOut = targetVolume < audioSource.volume;

        float currentTime = 0;
        float start = audioSource.volume;
        while (currentTime < duration) {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, targetVolume, currentTime / duration);
            if (isFadeOut) {
                if (audioSource.volume <= targetVolume) {
                    audioSource.Stop();
                }
            }

            yield return null;
        }

        yield break;
    }
}
