// UI Toolkit controller for SensorImageViewer.uxml -- drives the options menu
// shared by all three ImageView subclasses (CameraViewer's own ImageView,
// PanoViewer's PanoImageStreamer, StereoViewer's StereoStreamer). One panel
// for all three, because their menu is one prefab structure (CameraViewer.prefab)
// that the other two are VARIANTS of, differing only in a few materials/meshes
// and which concrete type their buttons target.
//
// THIS DOCUMENT IS ALWAYS ACTIVE, unlike the other five ported viewer panels.
// It has to keep running (and polling) while visually hidden, so the whole
// GameObject can no longer simply be a child that some flag switches on and
// off the way the others ride a GameObject's own SetActive(...). Instead,
// ImageView.MenuOpen is a plain bool ImageView's own code already flips
// (Start() closes it, OnClick/OnTopicChange/ToggleTrack open and close it),
// polled here to drive "is-open" on the whole OptionsMenu element instead of
// the document's own active state. See the swap tool for how this is parented.
//
// There is no more TopMenu GameObject at all -- it used to be dead uGUI
// weight kept alive purely to serve as that open/closed flag (via its own
// activeSelf) and to hold a tracking-mode icon (Track/Image/Image) neither of
// which needed a whole GameObject once ImageView exposed MenuOpen/
// TrackingState as plain properties instead.
//
// Flip/ScaleUp/ScaleDown/ToggleTrack/Clear all dispatch as plain virtual
// calls now -- Flip() used to need reflection because ImageView/Pano/Stereo
// each declared a separate NON-virtual same-named method (hiding, not
// overriding), but ImageView.Flip() is virtual now and Pano/Stereo properly
// override it, so a normal call already reaches the right one.
using UnityEngine.UIElements;
using RSL.Core.Menu;

namespace RSL.Sensors.Camera
{
    public class ImageViewerPanel : SensorViewerPanel
    {
        private ImageView Stream => stream as ImageView;

        private VisualElement _optionsMenu;
        private Button _track;
        private bool _isOpen;
        private int _lastTrackingState = -1;

        protected override void BindExtra(VisualElement root)
        {
            ImageView s = Stream;
            if (s == null) return;

            _optionsMenu = root.Q<VisualElement>("OptionsMenu");

            var flip = Require<Button>(root, "Flip");
            if (flip != null) flip.clicked += s.Flip;

            var minus = Require<Button>(root, "Minus");
            if (minus != null) minus.clicked += s.ScaleDown;

            var plus = Require<Button>(root, "Plus");
            if (plus != null) plus.clicked += s.ScaleUp;

            // Parameterless ToggleTrack(), not the (int) overload SensorStream
            // declares: this is the cycling one the uGUI Track button called
            // (ToggleTrack(_trackingState + 1), then closes the menu).
            _track = Require<Button>(root, "Track");
            if (_track != null) _track.clicked += s.ToggleTrack;

            // Starts closed and unpickable -- a zero-scaled element still
            // occupies its layout rect for hit-testing (the same fix
            // ConnectionSettingsPanel's EntryHost needed).
            SetOpen(false);
            RefreshTrackIcon(s.TrackingState);
        }

        // Polls MenuOpen rather than reacting to an event: ImageView has no
        // "menu opened" notification, only the plain bool its own
        // OnClick/ToggleTrack/OnTopicChange already flip -- the same thing
        // every other ported panel used to ride via the document's own active
        // state, now watched from the outside instead.
        protected override void Update()
        {
            base.Update();

            ImageView s = Stream;
            if (s == null) return;

            bool open = s.MenuOpen;
            if (open != _isOpen) SetOpen(open);

            RefreshTrackIcon(s.TrackingState);
        }

        // Mirrors the OLD uGUI tracking icon (Track/Image/Image inside the
        // now-deleted TopMenu, swapped via a Sprite[3]) with CSS classes on
        // the Track button itself instead -- .track-button's own three looks
        // (free/head-locked/axis-locked) were already authored in main.uss
        // for exactly this, just never wired up until now.
        private void RefreshTrackIcon(int state)
        {
            if (_track == null || state == _lastTrackingState) return;
            _lastTrackingState = state;
            _track.EnableInClassList("is-track-1", state == 1);
            _track.EnableInClassList("is-track-2", state == 2);
        }

        // Picking is switched on BEFORE the grow and off AFTER the shrink so
        // the panel is clickable for exactly as long as it looks clickable,
        // matching EntryHost's own ordering.
        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (_optionsMenu == null) return;
            if (open) _optionsMenu.pickingMode = PickingMode.Position;
            _optionsMenu.EnableInClassList("is-open", open);
            if (!open) _optionsMenu.pickingMode = PickingMode.Ignore;
        }
    }
}
