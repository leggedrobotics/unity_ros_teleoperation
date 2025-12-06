[Home](../README.md)
# Building the App

Since v0.0.94 app updates can be pushed over Release Channels on the [Meta Horizon Dashboard](https://developers.meta.com/horizon/).


# Channels
There are currently 2 channels available for the app:
- **Alpha**: This is the default channel and used for development and testing. It is updated frequently with the latest changes and often contains new features and bug fixes.
- **Beta**: This channel is more stable and is used for testing and should generally be ready for "plug and play" demos.

# Setup
In order to push the app, you first need to install the CLI from the [Oculus Platform Utility](https://developers.meta.com/horizon/resources/publish-reference-platform-command-line-utility/#download-and-install-the-utility) page, or on linux use the one in the root repo.

Then update the Bundle Version Code in the Player settings > Publishing Settings > Bundle Version Code. This should be incremented with every build you want to push to the channels with the least significant digit being the patch version. For example, if the current version is 0.0.94, patch 2, the next build should have a Bundle Version Code of 952.

Next build the apk locally (ensure Remove Internet Permission is unchecked!) and sign it with the keystore at the root of the repo, `user.keystore`. Use `main` for the alias and `rslteleopproject` for the password.

Then you can use the script `./upload_build` to upload the app to the desired channel. 

# Gaining Access
To get access to the app on a new account, message Max (mwilder@ethz.ch) with your account email, desired channel, and group affiliation. You will then get an email invite to join the group and can access the app from the Library in the Quest.