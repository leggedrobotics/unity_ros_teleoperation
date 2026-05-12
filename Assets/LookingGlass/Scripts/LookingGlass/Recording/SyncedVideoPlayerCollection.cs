using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace LookingGlass {
    [Serializable]
    internal struct SyncedVideoPlayerCollection {
        [Serializable]
        public struct Pair {
            public VideoPlayer videoPlayer;
            public float initialPlaybackSpeed;

            private bool SetPlaybackSpeed(float value) {
                if (videoPlayer == null)
                    return false;
                videoPlayer.playbackSpeed = value;
                return true;
            }

            public bool Freeze() => SetPlaybackSpeed(0);
            public bool Restore() => SetPlaybackSpeed(initialPlaybackSpeed);

            public bool Step() {
                if (videoPlayer == null)
                    return false;
                videoPlayer.StepForward();
                videoPlayer.Play();
                return true;
            }
        }

        private List<Pair> pairs;

        public SyncedVideoPlayerCollection(IEnumerable<VideoPlayer> videoPlayers) {
            if (videoPlayers == null)
                throw new ArgumentNullException(nameof(videoPlayers));
            pairs = new List<Pair>();
            pairs = videoPlayers
                .Where(v => v != null)
                .Select(v => new Pair { videoPlayer = v, initialPlaybackSpeed = v.playbackSpeed })
                .ToList();
        }

        public void AddVideoPlayers(IEnumerable<VideoPlayer> videoPlayers, bool freezeOnAdd = false){
            if (videoPlayers == null)
                throw new ArgumentNullException(nameof(videoPlayers));
            Pair[] newPairs = videoPlayers
                .Where(v => v != null)
                .Select(v => new Pair { videoPlayer = v, initialPlaybackSpeed = v.playbackSpeed })
                .ToArray();
            if (freezeOnAdd && newPairs != null)
                foreach (Pair p in newPairs)
                    p.Freeze();
            pairs.AddRange(newPairs);
        }

        public void AddVideoPlayer(VideoPlayer videoPlayer, bool freezeOnAdd = false){
            AddVideoPlayers(new VideoPlayer[]{videoPlayer}, freezeOnAdd);
        }

        public void RemoveVideoPlayers(IEnumerable<VideoPlayer> videoPlayers, bool restoreOnRemove = true){
            Pair[] pairToRemove = GetPairs(videoPlayers).ToArray();
            foreach (Pair p in pairToRemove) {
                if (restoreOnRemove)
                    p.Restore();
                pairs.Remove(p);
            }
        }

        public void RemoveVideoPlayer(VideoPlayer videoPlayer, bool restoreOnRemove = true){
            RemoveVideoPlayers(new VideoPlayer[]{videoPlayer}, restoreOnRemove);
        }

        public IEnumerable<Pair> GetPairs(IEnumerable<VideoPlayer> videoPlayers) {
            foreach (Pair p in pairs)
                if (p.videoPlayer != null && videoPlayers.Contains(p.videoPlayer))
                    yield return p;
        }

        public IEnumerable<VideoPlayer> GetAll() {
            foreach (Pair p in pairs)
                if (p.videoPlayer != null)
                    yield return p.videoPlayer;
        }

        public bool DoAll(Action<Pair> action) {
            if (pairs == null)
                return false;
            foreach (Pair p in pairs)
                action(p);
            return true;
        }

        public void FreezeAll() => DoAll(p => p.Freeze());
        public void RestoreAll() => DoAll(p => p.Restore());
        public void StepAll() => DoAll(p => p.Step());
    }
}