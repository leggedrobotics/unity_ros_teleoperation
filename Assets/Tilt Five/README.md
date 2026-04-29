# Tilt Five Unity SDK Package

`com.tiltfive.sdk` 1.4.2 "Jousting Jackalope"

This package is also distributed in an importable asset form in the
[Tilt Five SDK](https://docs.tiltfive.com/latest_release.html) installer or tarball.
Both distributions are made available for ease of reference and installation via the
Unity package manager.

## Dependencies

### Unity Editor and Build

- Requires: [Unity 2019.4 LTS](https://unity.com/releases/2019-lts) or later
- Recommended: [Unity Input System 1.4.4](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html)
  for Unity 2019.4 (or an appropriate version for later Unity versions)

The input system is not required (there is a legacy input event mechanism in the SDK)
but it is highly recommended for new projects and for local multiple Glasses
applications.

_Note: The Tilt Five Unity SDK is not a [Unity XR plug-in](https://docs.unity3d.com/Manual/XRPluginArchitecture.html),
though it shares controller device specifics with XR plug-ins in the Input System
and some rendering optimization paths. It originated before the 3rd party plug-in
system and multiple local Glasses support is not yet possible with that
architecture._

### Runtime

- [Tilt Five Drivers](https://docs.tiltfive.com/latest_release.html) version 1.4.2 or later

The Unity SDK is released with every version of the full Tilt Five SDK package and Driver install. Distributed content will require that the Driver installed on the platform is at least the same version of the SDK or later.

## Hardware and Unity Targets supported
- Host with USB 3.0 high speed or faster port (some may require a USB 3.0 powered hub)
- Windows 10+ x86_64 platforms
- Popular ARM-based (32/64 bit) Android 10+ devices (**BETA**)
- Ubuntu Linux amd64 (**PREVIEW**)

## Documentation

[On-line documentation](https://docs.tiltfive.com/unity_api/index.html) of the current version of this plug-in is available.

## More Information

Please visit the [developers section](https://www.tiltfive.com/make/home) of Tilt Five's website for more information and resources on this and other development tools for the
Tilt Five system and our developer program. You can also find [news](https://www.tiltfive.com/news) and [support](https://www.tiltfive.com/support) for the Tilt Five system there.

## Legal Notices

Copyright 2023 Tilt Five, Inc.

Per the [Tilt Five SDK License](https://docs.tiltfive.com/t5_license_agreement.html) the object libraries contained in this plugin are re-distributable with derivative works but cannot be modified. All other source, configuration, assets and other files in this package may be modified and redistributed as per the Apache License 2.0 as indicated in the package [license](LICENSE.md) file and source code files.
