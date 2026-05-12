using System;
using System.Collections.Generic;
using System.Numerics;

namespace LookingGlass.Toolkit.Bridge
{
    /// <summary>
    /// Contains a playable list of <see cref="PlaylistItem"/>s.<br />
    /// Playlists are used to show images and videos on a given LKG display.
    /// </summary>
    public class Playlist
    {
        public string name;
        public bool loop;
        private List<PlaylistItem> items;

        public Playlist(string name, bool loop = false)
        {
            this.name = name;
            this.loop = loop;
            items = new List<PlaylistItem>();
        }

        public void AddQuiltItem(string URI, int rows, int cols, float aspect, int viewCount, string tag = "whatever whatever", int durationMS = 20000)
        {
            int id = items.Count;
            PlaylistItem p = new PlaylistItem(id, URI, rows, cols, aspect, viewCount, tag, durationMS);
            items.Add(p);
        }

        public void AddRGBDItem(string URI, int rows, int cols, float aspect, float depthiness, float depth_cutoff, float focus, int depth_loc, float cam_dist, float fov, string tag = "whatever whatever", float zoom = 1, Vector2 crop_pos = new Vector2(), Vector2 quilt_size = new Vector2(), bool doDepthInversion = false, bool chromaDepth = false, int durationMS = 20000)
        {
            int id = items.Count;
            PlaylistItem p = new PlaylistItem(id, URI, rows, cols, aspect, depthiness, depth_cutoff, focus, depth_loc, cam_dist, fov, tag, zoom, crop_pos, quilt_size, doDepthInversion, chromaDepth, durationMS);
            items.Add(p);
        }

        public void RemoveItem(int id)
        {
            items.RemoveAt(id);

            for(int i = 0; i < items.Count; i++)
            {
                items[i].id = i;
            }
        }

        public string GetPlayPlaylistJson(Orchestration session, int head)
        {
            string content =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""name"": ""{name}"",
                    ""head_index"": ""{head}""
                }}
                ";

            return content;
        }

        public string GetInstanceJson(Orchestration session)
        {
            string content =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""name"": ""{name}"",
                    ""loop"": ""{(loop ? "true" : "false")}""
                }}
                ";

            return content;
        }

        public string[] GetPlaylistItemsAsJson(Orchestration session)
        {
            string[] strings = new string[items.Count];

            for(int i = 0; i < items.Count; i++)
            {
                strings[i] = GetPlaylistItemJson(session, i);
            }

            return strings;
        }

        private string GetPlaylistItemJson(Orchestration session, int id)
        {
            PlaylistItem item = items[id];

            string URI = item.URI;

            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (new Uri(URI).IsFile)
                {
                    URI = URI.Replace("\\", "\\\\");
                }
            }

            string content =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""name"": ""{name}"",
                    ""index"": ""{id}"",
                    ""uri"": ""{URI}"",
                    ""rows"": ""{item.rows}"",
                    ""cols"": ""{item.cols}"",
                    ""aspect"": ""{item.aspect}"",
                    ""view_count"": ""{item.viewCount}"",
                    ""durationMS"": ""{item.durationMS}"",
                    ""isRGBD"": ""{item.isRGBD}"",
                    ""depth_inversion"": ""{item.depth_inversion}"",
                    ""chroma_depth"": ""{item.chroma_depth}"",
                    ""crop_pos_x"": ""{item.crop_pos_x}"",
                    ""crop_pos_y"": ""{item.crop_pos_y}"",
                    ""quilt_size_x"": ""{item.quilt_size_x}"",
                    ""quilt_size_y"": ""{item.quilt_size_y}"",
                    ""depthiness"": ""{item.depthiness}"",
                    ""depth_cutoff"": ""{item.depth_cutoff}"",
                    ""depth_loc"": ""{item.depth_loc}"",
                    ""focus"": ""{item.focus}"",
                    ""cam_dist"": ""{item.cam_dist}"",
                    ""fov"": ""{item.fov}"",
                    ""zoom"": ""{item.zoom}"",
                    ""tag"": ""{item.tag}""
                }}
                ";

            return content;
        }
    }

    /// <summary>
    /// <para>
    /// Represents a single playable image or video for a LKG display.
    /// This can be used in a <see cref="Playlist"/> to play or stream images or videos, from local or remote sources.
    /// </para>
    /// <para>
    /// Currently, the following are supported as playlist items:
    /// <list type="bullet">
    /// <item>Quilt</item>
    /// <item>RGBD images</item>
    /// </list>
    /// </para>
    /// </summary>
    public class PlaylistItem
    {
        /// <summary>
        /// The index of this item in the playlist.
        /// </summary>
        public int id = -1;

        /// <summary>
        /// The URI (Universal Resource Identifier) of the source image or video.
        /// </summary>
        /// <remarks>
        /// For example, this may be a local file path, or a URL to a file hosted online.
        /// </remarks>
        public string URI = "";

        public int rows = 1;
        public int cols = 1;
        public float aspect = 1;
        public int viewCount = 1;
        public int durationMS = 20000;

        public int isRGBD = 0;

        //NOTE: Up next will be configurable view dimming, and some extra stuff added to this class
        [Obsolete] public float cam_dist = 0;
        [Obsolete] public float fov = 0;

        public int depth_loc = 0;
        public int depth_inversion = 0;
        public int chroma_depth = 0;

        public float crop_pos_x = 0;
        public float crop_pos_y = 0;

        public float quilt_size_x = 0;
        public float quilt_size_y = 0;

        public float depthiness = 0;
        public float depth_cutoff = 0;
        public float focus = 0;
        public float zoom = 0;

        public string tag;

        internal PlaylistItem(int id, string URI, int rows, int cols, float aspect, int viewCount, string tag = "", int durationMS = 20000)
        {
            this.id = id;
            this.URI = URI;
            this.rows = rows;
            this.cols = cols;
            this.aspect = aspect;
            this.viewCount = viewCount;
            this.durationMS = durationMS;
            this.tag = tag;
        }

        internal PlaylistItem(int id, string URI, int rows, int cols, float aspect, 
            float depthiness, float depth_cutoff, float focus, int depth_loc, 
            float cam_dist, float fov, string tag = "", float zoom = 1, Vector2 crop_pos = new Vector2(),
            Vector2 quilt_size = new Vector2(), bool doDepthInversion = false, bool chromaDepth = false, int durationMS = 20000)
        {
            this.id = id;
            this.URI = URI;
            this.rows = rows;
            this.cols = cols;
            viewCount = rows * cols;
            this.aspect = aspect;
            this.tag = tag;

            isRGBD = 1;

            this.cam_dist = cam_dist;
            this.fov = fov;

            this.depth_loc = depth_loc;
            this.depthiness = depthiness;
            this.depth_cutoff = depth_cutoff;
            this.focus = focus;
            this.zoom = zoom;
            
            crop_pos_x = crop_pos.X;
            crop_pos_y = crop_pos.Y;
            
            quilt_size_x = quilt_size.X;
            quilt_size_y = quilt_size.Y;

            if(quilt_size_x == 0)
            {
                quilt_size_x = 4096;
            }

            if (quilt_size_y == 0)
            {
                quilt_size_y = 4096;
            }

            depth_inversion = doDepthInversion ? 1 : 0;
            chroma_depth = chromaDepth ? 1 : 0;
            this.durationMS = durationMS;
        }
    }
}
