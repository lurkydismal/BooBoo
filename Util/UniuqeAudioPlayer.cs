using System;
using System.Collections.Generic;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Util
{
    //Same as audio player but it will stop the currently playing sound when starting a new one. used mostly for voices
    internal class UniqueAudioPlayer : AudioPlayer
    {
        Sound currentlyPlayingSound;

        protected UniqueAudioPlayer() { }

        public UniqueAudioPlayer(Dictionary<string, Sound> sounds) 
            : base(sounds)
        {

        }

        public static new UniqueAudioPlayer LoadSoundsInArchive(DPArc archive, string folderToCheck)
        {
            UniqueAudioPlayer rtrn = new UniqueAudioPlayer();
            ArchiveFile[] files = archive.GetFolder(folderToCheck);
            foreach (ArchiveFile file in files)
                if (file.fileName.EndsWith(".wav") || file.fileName.EndsWith(".ogg"))
                {
                    string fileName = file.fileName.Replace(folderToCheck, string.Empty);
                    fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                    Sound sound = Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(file.fileName.Substring(fileName.Length), file.data));
                    rtrn.sounds.Add(fileName, sound);
                }
            return rtrn;
        }

        public override void Play(string sound)
        {
            Raylib.StopSound(currentlyPlayingSound);
            if (sounds.ContainsKey(sound))
                currentlyPlayingSound = sounds[sound];
            base.Play(sound);
        }
    }
}
