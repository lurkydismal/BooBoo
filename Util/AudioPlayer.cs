using System;
using System.Collections.Generic;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Util
{
    internal class AudioPlayer
    {
        public Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();

        protected AudioPlayer() { }

        public AudioPlayer(Dictionary<string, Sound> sounds)
        {
            this.sounds = sounds;
        }

        public static AudioPlayer LoadSoundsInArchive(DPArc archive, string folderToCheck)
        {
            AudioPlayer rtrn = new AudioPlayer();
            ArchiveFile[] files = archive.GetFolder(folderToCheck);
            foreach(ArchiveFile file in files)
                if (file.fileName.EndsWith(".wav") || file.fileName.EndsWith(".ogg"))
                {
                    string fileName = file.fileName.Replace(folderToCheck, string.Empty);
                    fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                    Sound sound = Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(file.fileName.Substring(fileName.Length), file.data));
                    rtrn.sounds.Add(fileName, sound);
                }
            return rtrn;
        }

        public virtual void Play(string sound)
        {
            if(sounds.ContainsKey(sound))
                Raylib.PlaySound(sounds[sound]);
        }
    }
}
