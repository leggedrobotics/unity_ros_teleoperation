using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace LookingGlass {
    public class ScreenshotProgress {
        private bool isQuilt;
        private string filePath;
        private Task<Texture2D> screenshotTask;
        private Task task;

        /// <summary>
        /// Is this a 3D screenshot (of a quilt texture)?<br />
        /// If not, the screenshot is just one 2D capture.
        /// </summary>
        public bool IsQuilt => isQuilt;

        /// <summary>
        /// <para>The file path where the screenshot is being saved to.</para>
        /// </summary>
        public string FilePath => filePath;

        /// <summary>
        /// <para>
        /// A task representing only the screenshot process.<br />
        /// A relatively-long task to write out PNG file metadata may still be running after this. (See <seealso cref="Task"/>)
        /// </para>
        /// <para>
        /// Note that the <see cref="Texture2D"/> asset returned is NOT connected
        /// as a persistent asset to the file given by <see cref="FilePath"/> path, since
        /// there are folder path options that exist outside your Assets directory.
        /// </para>
        /// </summary>
        public Task<Texture2D> ScreenshotTask => screenshotTask;

        /// <summary>
        /// A task representing the entire screenshot and PNG metadata processes.
        /// </summary>
        public Task Task => task;

        internal static ScreenshotProgress Create(bool isQuilt, string filePath, Task<Texture2D> screenshotTask, Task task) {
            Assert.IsFalse(string.IsNullOrWhiteSpace(filePath));
            Assert.IsNotNull(screenshotTask);
            if (task == null)
                task = screenshotTask;

            ScreenshotProgress p = new ScreenshotProgress();
            p.isQuilt = isQuilt;
            p.filePath = filePath;
            p.screenshotTask = screenshotTask;
            p.task = task;

            return p;
        }
    }
}