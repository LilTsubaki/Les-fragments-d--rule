﻿using UnityEngine;

/// <summary>
/// The container of AudioPlayers in the pool of AudioManager.
/// </summary>
public class AudioContainer : Poolable<AudioContainer>
{
    /// <summary>
    /// The GameObject that has an AudioPlayer.
    /// </summary>
    internal GameObject _audioGameObject;

    public void Copy(AudioContainer t)
    {
        _audioGameObject = UnityEngine.Object.Instantiate(t._audioGameObject);
        _audioGameObject.transform.parent = t._audioGameObject.transform.parent;
    }

    public bool IsReady()
    {
        if (_audioGameObject != null && _audioGameObject.GetComponent<AudioPlayer>() != null)
        {
            return !_audioGameObject.GetComponent<AudioPlayer>()._audio.isPlaying;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// The reset of the instance. Puts the power of he volume to 1 and disables fading behaviours.
    /// </summary>
    public void Pick()
    {
        AudioPlayer ap = _audioGameObject.GetComponent<AudioPlayer>();
        ap._power = 1;
        ap._panoramicPosition = 0;
        ap._fadeIn = false;
        ap._fadeOut = false;
    }
}