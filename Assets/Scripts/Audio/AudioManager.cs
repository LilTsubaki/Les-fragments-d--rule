﻿using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// The class managing the AudioSources. It uses MaxAudioContainers different sources. The AudioClips have to be registered in AudioDatas, by group of sound.
/// </summary>
public class AudioManager : Manager<AudioManager>
{
    /// <summary>
    /// Max AudioContainer pool size.
    /// </summary>
    private static int _MaxAudioContainers = 20;

    /// <summary>
    /// The different spatializations possible to use when playing a sound.
    /// </summary>
    public enum spatialization : int { AUDIO_2D = 0, AUDIO_3D = 1 };
    /// <summary>
    /// The Pool containing the AudioContainers.
    /// </summary>
    private Pool<AudioContainer> _audioContainers;
    /// <summary>
    /// The mapping between names of sound groups and the sounds.
    /// </summary>
    private Dictionary<string, AudioData> _sounds;
    /// <summary>
    /// The mapping between AudioMixerGroups and previous volumes, before being muted.
    /// </summary>
    private Dictionary<string, float> _volumes;
    /// <summary>
    /// The mapping representing the mute by group.
    /// </summary>
    private Dictionary<string, bool> _isMuted;
    /// <summary>
    /// The ids used to keep track of the AudioPlayers that are actually being used.
    /// </summary>
    private int _nextIdPool;
    /// <summary>
    /// The mapping between ids given and AudioPlayers, so we have to use the AudioManager to modify them.
    /// </summary>
    private Dictionary<int, AudioPlayer> _idPlayers;

    private Dictionary<string, ProceduralMusic> _musics;

    /// <summary>
    /// Constructor of the AudioManager. Must only be called from GetInstance.
    /// </summary>
    public AudioManager()
    {
        if (_instance != null)
            throw new ManagerException();

        // Creation of the root Game Object containing every audio related Game Object
        GameObject parent = new GameObject();
        parent.name = "AudioContainers";

        // Creation of the intial Game Object for the Pool, containing an AudioSource and a AudioPlayer

        GameObject obj = new GameObject();
        AudioSource asource = obj.AddComponent<AudioSource>();
        AudioPlayer ap = obj.AddComponent<AudioPlayer>();
        obj.AddComponent<DontDestroyOnLoad>();
        ap._audio = asource;
        obj.transform.name = "Audiosource";
        //obj.transform.parent = parent.transform;

        // Creation of the initial AudioContainer

        AudioContainer ac = new AudioContainer();
        ac._audioGameObject = obj;
        _audioContainers = new Pool<AudioContainer>(ac, _MaxAudioContainers);
        _sounds = new Dictionary<string, AudioData>();

        _isMuted = new Dictionary<string, bool>();

        _volumes = new Dictionary<string, float>();

        _nextIdPool = 0;
        _idPlayers = new Dictionary<int, AudioPlayer>();
        _musics = new Dictionary<string, ProceduralMusic>();
    }

    /// <summary>
    /// Mutes a group of sounds.
    /// </summary>
    /// <param name="amg">The AudioMixerGroup to mute.</param>
    /// <param name="m">Is the group to be muted or unmuted.</param>
    public void Mute(AudioMixerGroup amg, bool m)
    {
        string groupName = "Volume" + amg.name;

        if (m)
        {
            float volGroup;
            amg.audioMixer.GetFloat(groupName, out volGroup);
            _volumes[groupName] = volGroup;
            amg.audioMixer.SetFloat(groupName, -80);
        }
        else
        {
            amg.audioMixer.SetFloat("Volume" + amg.name, _volumes[groupName]);
        }
        _isMuted[groupName] = m;
    }

    /// <summary>
    /// Switches the AudioMixerGroup on muted or unmuted.
    /// </summary>
    /// <param name="amg">The AudioMixerGroup to switch the volume of.</param>
    public void Mute(AudioMixerGroup amg)
    {
        string groupName = "Volume" + amg.name;
        _isMuted[groupName] = !_isMuted[groupName];

        if (_isMuted[groupName])
        {
            float volGroup;
            amg.audioMixer.GetFloat(groupName, out volGroup);
            _volumes[groupName] = volGroup;
            amg.audioMixer.SetFloat(groupName, -80);
        }
        else
        {
            amg.audioMixer.SetFloat("Volume" + amg.name, _volumes[groupName]);
        }
    }

    /// <summary>
    /// Registers an AudioData in _sounds. Used by AudioData on Awake().
    /// </summary>
    /// <param name="name">The name of the group of sounds the AudioData contains.</param>
    /// <param name="data">The AudioData to register.</param>
    /// <returns>True if the name is free, else false.</returns>
    public bool Register(string name, AudioData data)
    {
        if (_sounds.ContainsKey(name))
        {
            return false;
        }
        _sounds.Add(name, data);
        return true;
    }

    /// <summary>
    /// Registers a ProceduralMusic in _musics. Used by ProceduralMusic on Awake().
    /// </summary>
    /// <param name="name"></param>
    /// <param name="music"></param>
    /// <returns></returns>
    public bool RegisterMusic(string name, ProceduralMusic music)
    {
        if (_musics.ContainsKey(name))
        {
            return false;
        }
        _musics.Add(name, music);
        return true;
    }

    /// <summary>
    /// Removes an AudioData from _sounds. Used by AudioData on OnDestroy().
    /// </summary>
    /// <param name="name">The name of the group to remove.</param>
    /// <returns>True if the name was registered, else false.</returns>
    public bool Withdraw(string name)
    {
        if (!_sounds.ContainsKey(name))
        {
            return false;
        }
        _sounds.Remove(name);
        return true;
    }

    /// <summary>
    /// Gets a free AudioPlayer and grants it an id.
    /// </summary>
    /// <returns>The id given to the reserved AudioPlayer.</returns>
    private int GetFreeAudioPlayerId()
    {
        AudioContainer ac = _audioContainers.GetPoolable();
        if (ac != null)
        {
            AudioPlayer ap = ac._audioGameObject.GetComponent<AudioPlayer>();

            // If we already have given an id the the AudioPlayer, we have to remove it and reassign a new one.
            if (_idPlayers.ContainsValue(ap))
            {
                int idRemove = -1;
                foreach (int i in _idPlayers.Keys)
                {
                    if (_idPlayers[i].Equals(ap))
                    {
                        idRemove = i;
                        break;
                    }
                }
                // We remove the previous id we gave.
                if (idRemove != -1)
                    _idPlayers.Remove(idRemove);
            }
            _idPlayers[++_nextIdPool] = ap;
            return _nextIdPool;
        }
        return -1;
    }

    /// <summary>
    /// Gets an AudioPlayer by its id, if still exists.
    /// </summary>
    /// <param name="id">The id of the AudioPlayer to get.</param>
    /// <returns>The AudioPlayer corresponding to the id. Null if doesn't exist.</returns>
    private AudioPlayer GetFreeAudioPlayer(int id)
    {
        if(_idPlayers.ContainsKey(id))
        {
            AudioPlayer ap = _idPlayers[id];
            return ap;
        }
        return null;

    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <returns>The id of the AudioPlayer that plays the sound.</returns>
    public int Play(string soundName)
    {
        return Play(soundName, false);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="loop">Does the sound have to be played in a loop ?</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int Play(string soundName, bool loop)
    {
        return Play(soundName, false, loop);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="loop">Does the sound have to be played in a loop ?</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int Play(string soundName, bool randPitch, bool loop)
    {
        return Play(soundName, randPitch, loop, spatialization.AUDIO_2D, Vector3.zero, 0, 0);
    }


    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="loop">Does the sound have to be played in a loop ?</param>
    /// <param name="space">Is the sound to be played in 2D or in 3D ?</param>
    /// <param name="pos">The position the sound has to be played at (use if played in 3D).</param>
    /// <param name="distMin">The minimum distance the sound can be heard from (use if played in 3D).</param>
    /// <param name="distMax">The maximal distance the sound can be heard from (use if played in 3D).</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int Play(string soundName, bool randPitch, bool loop, spatialization space, Vector3 pos, float distMin = 0, float distMax = 0)
    {
        // If an AudioData is registered with soundName
        if (_sounds.ContainsKey(soundName))
        {
            AudioData ad = _sounds[soundName];
            if (ad != null)
            {
                int id = GetFreeAudioPlayerId();
                AudioPlayer ap = GetFreeAudioPlayer(id);
                // If we could get a free AudioPlayer
                if (ap != null)
                {
                    AudioSource auSo = ap._audio;
                    auSo.clip = ad.GetClip();
                    auSo.outputAudioMixerGroup = ad.group;
                    auSo.spatialBlend = (float)space;
                    auSo.gameObject.transform.position = pos;
                    auSo.minDistance = distMin;
                    auSo.maxDistance = distMax;
                    auSo.loop = loop;
                    ap._randPitch = randPitch;
                    ap._minPitch = ad.pitchMin;
                    ap._maxPitch = ad.pitchMax;
                    if (randPitch)
                    {
                        auSo.pitch = EruleRandom.RangeValue(ad.pitchMin, ad.pitchMax);
                    }
                    else
                    {
                        auSo.pitch = 1;
                    }
                    auSo.Play();
                }
                return id;
            }
        }
        return -1;
    }

    /// <summary>
    /// Plays a sound on a given AudioPlayer id. Should not be used unless sure the id has an AudioPlayer associated.
    /// </summary>
    /// <param name="playerId">The allocated id of the AudioPlayer.</param>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="space">Is the sound to be played in 2D or in 3D ?</param>
    /// <param name="pos">The position the sound has to be played at (use if played in 3D).</param>
    /// <param name="distMin">The minimum distance the sound can be heard from (use if played in 3D).</param>
    /// <param name="distMax">The maximal distance the sound can be heard from (use if played in 3D).</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    /// <returns></returns>
    public int PlayOnPlayer(int playerId, bool loopClip, string soundName, bool randPitch, spatialization space, Vector3 pos, float distMin = 0, float distMax = 0)
    {
        // If an AudioData is registered with soundName
        if (_sounds.ContainsKey(soundName))
        {
            AudioData ad = _sounds[soundName];
            if (ad != null)
            {
                AudioPlayer ap = GetFreeAudioPlayer(playerId);
                // If we could get a free AudioPlayer
                if (ap != null)
                {
                    AudioSource auSo = ap._audio;
                    auSo.clip = ad.GetClip();
                    auSo.outputAudioMixerGroup = ad.group;
                    auSo.spatialBlend = (float)space;
                    auSo.gameObject.transform.position = pos;
                    auSo.minDistance = distMin;
                    auSo.maxDistance = distMax;
                    auSo.loop = false;
                    ap._randPitch = randPitch;
                    ap._minPitch = ad.pitchMin;
                    ap._maxPitch = ad.pitchMax;
                    ap._loopClips = loopClip;
                    if (randPitch)
                    {
                        auSo.pitch = EruleRandom.RangeValue(ad.pitchMin, ad.pitchMax);
                    }
                    else
                    {
                        auSo.pitch = 1;
                    }
                    auSo.Play();
                }
                return playerId;
            }
        }
        return -1;
    }

    /// <summary>
    /// Fades a sound in.
    /// </summary>
    /// <param name="id">The id of the sound to fade in.</param>
    /// <returns>True if the id exists, else false.</returns>
    public bool FadeIn(int id)
    {
        if (_idPlayers.ContainsKey(id))
            _idPlayers[id].FadeIn();
        return false;
    }

    /// <summary>
    /// Fades a sound out.
    /// </summary>
    /// <param name="id">The id of the sound to fade in.</param>
    /// <returns>True if the id exists, else false.</returns>
    public bool FadeOut(int id)
    {
        if (_idPlayers.ContainsKey(id))
            _idPlayers[id].FadeOut();
        return false;
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name. The sound is faded in.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <returns>The id of the AudioPlayer that plays the sound.</returns>
    public int PlayFadeIn(string soundName)
    {
        return PlayFadeIn(soundName, false);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing. The sound is faded in.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="loop">Does the sound have to be played in a loop ?</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int PlayFadeIn(string soundName, bool loop)
    {
        return PlayFadeIn(soundName, false, loop);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing. The sound is faded in.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="loop">Does the sound have to be played in a loop ?</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int PlayFadeIn(string soundName, bool randPitch, bool loop)
    {
        return PlayFadeIn(soundName, randPitch, loop, spatialization.AUDIO_2D, Vector3.zero, 0, 0);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing. The sound is faded in.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="loop">Does the sound have to be played in a loop ?</param>
    /// <param name="space">Is the sound to be played in 2D or in 3D ?</param>
    /// <param name="pos">The position the sound has to be played at (use if played in 3D).</param>
    /// <param name="distMin">The minimum distance the sound can be heard from (use if played in 3D).</param>
    /// <param name="distMax">The maximal distance the sound can be heard from (use if played in 3D).</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int PlayFadeIn(string soundName, bool randPitch, bool loop, spatialization space, Vector3 pos, float distMin = 0, float distMax = 0)
    {
        int id = Play(soundName, randPitch, loop, space, pos, distMin, distMax);
        FadeIn(id);
        return id;
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <returns>The id of the AudioPlayer that plays the sound.</returns>
    public int PlayLoopingClips(string soundName)
    {
        return PlayLoopingClips(soundName, false, false);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="fadeIn">Does the sound have to be played fading in ?</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int PlayLoopingClips(string soundName, bool randPitch, bool fadeIn)
    {
        return PlayLoopingClips(soundName, randPitch, fadeIn, spatialization.AUDIO_2D, Vector3.zero, 0, 0);
    }

    /// <summary>
    /// Plays a sound contained in the AudioData that corresponds to a name, given differents parameters of playing.
    /// </summary>
    /// <param name="soundName">The name of the AudioData to play a sound of.</param>
    /// <param name="randPitch">Is the sound to be played at a random pitch ?</param>
    /// <param name="fadeIn">Does the sound have to be played fading in ?</param>
    /// <param name="space">Is the sound to be played in 2D or in 3D ?</param>
    /// <param name="pos">The position the sound has to be played at (use if played in 3D).</param>
    /// <param name="distMin">The minimum distance the sound can be heard from (use if played in 3D).</param>
    /// <param name="distMax">The maximal distance the sound can be heard from (use if played in 3D).</param>
    /// <returns>The id given to the AudioPlayer that plays the sound. -1 if couldn't.</returns>
    public int PlayLoopingClips(string soundName, bool randPitch, bool fadeIn, spatialization space, Vector3 pos, float distMin = 0, float distMax = 0)
    {
        int id = Play(soundName, randPitch, false, space, pos, distMin, distMax);
        _idPlayers[id].Looping(id, soundName, randPitch, space, pos, distMin, distMax);
        if (fadeIn)
            FadeIn(id);
        return id;
    }

    /// <summary>
    /// Stops the loop on a player.
    /// </summary>
    /// <param name="playerId">The id of the player to stop the loop of.</param>
    public void StopPlayLoopingClips(int playerId)
    {
        if (!_idPlayers.ContainsKey(playerId))
        {
            return ;
        }
        _idPlayers[playerId].StopLooping(false);
    }

    public void StopPlayLoopingClipsFading(int playerId)
    {
        if (!_idPlayers.ContainsKey(playerId))
        {
            return;
        }
        _idPlayers[playerId].StopLooping(true);
    }

    public void StopPlayer(int idPlayer)
    {
        if (!_idPlayers.ContainsKey(idPlayer))
        {
            return ;
        }
        _idPlayers[idPlayer]._audio.Stop();
    }

    public bool IsPlayerPlaying(int idPlayer)
    {
        if (!_idPlayers.ContainsKey(idPlayer))
        {
            return false;
        }
        return _idPlayers[idPlayer]._audio.isPlaying;
    }

    public float GetPlayerDuration(int idPlayer)
    {
        if (!_idPlayers.ContainsKey(idPlayer))
        {
            return -1;
        }
        return _idPlayers[idPlayer]._audio.clip.length;
    }

    public void SetPlayerPower(int playerId, float power)
    {
        if (!_idPlayers.ContainsKey(playerId))
        {
            return;
        }
        _idPlayers[playerId]._power = power;
    }

    public float GetPlayerPower(int playerId)
    {
        if (!_idPlayers.ContainsKey(playerId))
        {
            return -1;
        }
        return _idPlayers[playerId]._power;
    }

    public void FadePanoramicStereo(int playerId, float panStereo)
    {
        if (!_idPlayers.ContainsKey(playerId))
        {
            return;
        }
        _idPlayers[playerId].GoToPanoramicPosition(panStereo);
    }


    public void PlayMusic(string music)
    {
        if (!_musics.ContainsKey(music))
        {
            return;
        }
        _musics[music].StartPlaying();
    }

    public void StopMusic(string music)
    {
        if (!_musics.ContainsKey(music))
        {
            return;
        }
        _musics[music].Stop();
    }
}
