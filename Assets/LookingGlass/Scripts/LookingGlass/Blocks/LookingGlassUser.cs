using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using GraphQLClient;

using Object = UnityEngine.Object;
using LookingGlass.Toolkit;
using LookingGlass.Toolkit;

namespace LookingGlass.Blocks {
    /// <summary>
    /// Provides access into logging in with your <a href="https://lookingglassfactory.com">Looking Glass</a> account.
    /// </summary>
    public static class LookingGlassUser {
        private static class PlayerPrefKeys {
            public const string AccessToken = "accessToken";
            public const string RefreshToken = "refreshToken";
            public const string UserId = "userId";
            public const string Username = "username";
            public const string DisplayName = "displayName";
            public const string LoginTime = "loginTime";
            public const string ExpireIn = "expireIn";
            public const string PermanentLogin = "permanentLogin";
        }

        private static Lazy<Regex> QuiltSettingsSuffix = new Lazy<Regex>(() =>
            new Regex("_qs(?<quiltColumns>[0-9]*)x(?<quiltRows>[0-9]*)a(?<aspect>[0-9]*\\.?[0-9]*)"));

        private static int maxLoginTime = 60000 * 5;
        private static int checkInterval = 1000;

        private static bool isLoggingIn = false;

        public static event Action onLoginSucceeded;
        public static event Action onLoginFailed;
        public static event Action onLogout;

        public static bool IsRefreshable => !string.IsNullOrEmpty(RefreshToken);
        public static bool IsLoggedIn => !IsSessionExpired || (isPermanentLogin && IsRefreshable);
        public static bool IsSessionExpired => string.IsNullOrEmpty(AccessToken) || IsLoginTokenExpired();
        public static string AccessToken {
            get { return PlayerPrefs.GetString(PlayerPrefKeys.AccessToken); }
            private set {
                PlayerPrefs.SetString(PlayerPrefKeys.AccessToken, value);
                if (value != null) LoginTime = GetTimeString();
            }
        }

        public static string RefreshToken {
            get { return PlayerPrefs.GetString(PlayerPrefKeys.RefreshToken); }
            private set {
                PlayerPrefs.SetString(PlayerPrefKeys.RefreshToken, value);
                if (value != null) LoginTime = GetTimeString();
            }
        }

        public static int UserId {
            get { return PlayerPrefs.GetInt(PlayerPrefKeys.UserId); }
            private set { PlayerPrefs.SetInt(PlayerPrefKeys.UserId, value); }
        }

        public static bool isPermanentLogin {
            get { return PlayerPrefs.GetInt(PlayerPrefKeys.PermanentLogin, 0) > 0; }
            set {
                PlayerPrefs.SetInt(PlayerPrefKeys.PermanentLogin, value ? 1 : 0);
            }
        }

        public static string Username {
            get { return PlayerPrefs.GetString(PlayerPrefKeys.Username); }
            private set { PlayerPrefs.SetString(PlayerPrefKeys.Username, value); }
        }

        public static string DisplayName {
            get { return PlayerPrefs.GetString(PlayerPrefKeys.DisplayName); }
            private set { PlayerPrefs.SetString(PlayerPrefKeys.DisplayName, value); }
        }

        public static string LoginTime {
            get { return PlayerPrefs.GetString(PlayerPrefKeys.LoginTime); }
            private set { PlayerPrefs.SetString(PlayerPrefKeys.LoginTime, value); }
        }
        // value of when the login will be expired in minute
        public static float ExpireInMin {
            get { return ExpireInSec / 60f; }
        }

        public static int ExpireInSec {
            get { return PlayerPrefs.GetInt(PlayerPrefKeys.ExpireIn, 0); }
            private set { PlayerPrefs.SetInt(PlayerPrefKeys.ExpireIn, value); }
        }

        internal static int MaxLoginTime {
            get { return maxLoginTime; }
            set { maxLoginTime = Mathf.Max(checkInterval, value); }
        }

        internal static int CheckInterval {
            get { return checkInterval; }
            set { checkInterval = Mathf.Clamp(value, 10, maxLoginTime); }
        }
        public static bool IsTokenExpired(string key, float expireTimeInMin) {
            if (!PlayerPrefs.HasKey(key)) {
                return true;
            }

            DateTime lastTokenFetchTime = ParseTimeString(PlayerPrefs.GetString(key));

            //Debug.Log($"lastTokenFetchTime:{lastTokenFetchTime.ToString()} - {(DateTime.UtcNow - lastTokenFetchTime).TotalMinutes} : {expireTimeInMin}");

            // expires in x mins but changing it at x - 1 to avoid conflicts
            return (DateTime.UtcNow - lastTokenFetchTime).TotalMinutes > expireTimeInMin - 1;
        }

        public static bool IsLoginTokenExpired() {
            return IsTokenExpired(PlayerPrefKeys.LoginTime, ExpireInMin);
        }

        private static DateTime ParseTimeString(string timeString) {
            long lastTokenFetchTimeUnix = long.Parse(timeString);
            return DateTimeOffset.FromUnixTimeSeconds(lastTokenFetchTimeUnix).UtcDateTime;
        }

        public static string GetTimeString() {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        public static async Task LogIn(Action<object> responseCallback = null) {
            if (isLoggingIn)
                throw new InvalidOperationException("Failed to log in: logging in is already in-progress!");
            isLoggingIn = true;
            int maxLoginTime = LookingGlassUser.maxLoginTime;
            int checkInterval = LookingGlassUser.checkInterval;

            try {
                UnityWebRequest loginRequest = LookingGlassWebRequests.CreateRequestForUserAuthorization(isPermanentLogin);
                OAuthDeviceCodeResponse response = null;
                try {
                    response = await loginRequest.SendAsync<OAuthDeviceCodeResponse>(NetworkErrorBehaviour.Exception, 30000);
                } finally {
                    loginRequest.FullyDispose();
                }
                responseCallback?.Invoke(response);

                if (response != null) {
                    Application.OpenURL(response.verification_uri_complete);

                    await Task.Delay(checkInterval);
                    int awaitedTime = checkInterval;

                    while (isLoggingIn && awaitedTime <= maxLoginTime) {
                        UnityWebRequest isDoneRequest = LookingGlassWebRequests.CreateRequestToCheckDeviceCode(response.device_code);
                        Task delay = Task.Delay(checkInterval);
                        AccessTokenResponse accessTokenResponse;
                        try {
                            accessTokenResponse = await isDoneRequest.SendAsync<AccessTokenResponse>(NetworkErrorBehaviour.Silent, checkInterval);
                        } finally {
                            isDoneRequest.FullyDispose();
                        }

                        if (accessTokenResponse != null && !string.IsNullOrEmpty(accessTokenResponse.access_token)) {
                            UserData userData = await LookingGlassWebRequests.SendRequestToGetUserData(accessTokenResponse.access_token);
                            ReLogInImmediate(accessTokenResponse.access_token, userData.id, userData.username, userData.displayName,
                                accessTokenResponse.expires_in, false, isPermanentLogin ? accessTokenResponse.refresh_token : "");
                            isLoggingIn = false;
                            responseCallback?.Invoke(accessTokenResponse);
                            onLoginSucceeded?.Invoke();
                        } else await delay;

                        awaitedTime += checkInterval;
                    }
                    if (!IsLoggedIn)
                        throw new TimeoutException("Login wait time exceed the maximum of " + ((float)maxLoginTime / 1000).ToString("F2") + "sec!");
                }
            } catch (Exception e) {
                Debug.LogException(e);
                // LogOut();
                onLoginFailed?.Invoke();
                throw;
            } finally {
                isLoggingIn = false;
            }
        }

        public static void ReLogInImmediate(string accessToken, int userId, string username, string displayName, int expiresIn = 0, bool raiseEvent = false, string refreshToken = null) {
            try {
                AccessToken = accessToken;
                UserId = userId;
                Username = username;
                DisplayName = displayName;
                ExpireInSec = expiresIn;
                RefreshToken = refreshToken;

                Assert.IsFalse(string.IsNullOrEmpty(Username), "The username must be valid to be logged in! Did we parse the data correctly from the user data request?");
                Assert.IsFalse(string.IsNullOrEmpty(DisplayName), "The display name must be valid to be logged in! Did we parse the data correctly from the user data request?");

                if (raiseEvent && IsLoggedIn)
                    onLoginSucceeded?.Invoke();
            } catch (AssertionException e) {
                Debug.LogException(e);
                LogOut();
            }
        }

        public static void LogOut() {
            bool wasLoggedIn = IsLoggedIn;
            ExpireInSec = 0;
            AccessToken = null;
            RefreshToken = null;

            if (wasLoggedIn)
                onLogout?.Invoke();
        }

        public static BlockUploadProgress UploadFileToBlocks(string filePath, CreateQuiltHologramArgs args) => BlockUploadProgress.StartInternal(filePath, args);
        internal static async Task UploadFileToBlocksInternal(string filePath, CreateQuiltHologramArgs args, bool useQuiltSuffix,
            Action<UnityWebRequest> uploadRequestSetter, Action<string> updateProgressText, Action<LogType, string> printResult, Action<CreateQuiltHologramArgs> setArgs, Action<HologramData> setResult) {

            string GetErrorPrefix() => "There was an error uploading.\n\n";
            bool printedExceptionText = false;
            bool hasWarnings = false;
            int hologramId = -1;
            UnityWebRequest request = null;
            try {
                if (string.IsNullOrWhiteSpace(args.title))
                    throw new ArgumentException("The title must not be empty!", nameof(args.title));
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("", filePath);

                Match m = null;
                if (useQuiltSuffix) {
                    m = QuiltSettingsSuffix.Value.Match(filePath);
                    if (!m.Success) {
                        hasWarnings = true;
                        useQuiltSuffix = false;

                        bool foundDefault = false;
                        ILKGDeviceTemplateSystem system = LookingGlass.Toolkit.ServiceLocator.Instance.GetSystem<ILKGDeviceTemplateSystem>();
                        if (system != null) {
                            LKGDeviceTemplate template = system.GetDefaultTemplate();
                            LKGDeviceType defaultType = template.calibration.GetDeviceType();
                            if (template != null) {
                                foundDefault = true;
                                args.quiltCols = template.defaultQuilt.columns;
                                args.quiltRows = template.defaultQuilt.rows;
                                args.aspectRatio = template.defaultQuilt.renderAspect;
                                printResult(LogType.Warning, "No quilt settings in file name detected.\n\nThe hologram was given default values for the " + defaultType.GetNiceName() + ".\n" +
                                    "(Ex: _qs" + template.defaultQuilt.columns + "x" + template.defaultQuilt.rows + "a" + template.defaultQuilt.renderAspect + " defines " +
                                    template.defaultQuilt.columns + " columns x " + template.defaultQuilt.rows + " rows at " + template.defaultQuilt.renderAspect + " aspect ratio)");
                            }
                        }

                        if (!foundDefault) {
                            //REVIEW: [CRT-4039] What happens when the user doesn't provide quilt settings in the file name?
                            printResult(LogType.Warning, "No quilt settings in file name detected.\n\nNo default template was found to fallback to!");
                        }
                    }
                }

                updateProgressText("Getting Upload URL...");
                string fileName = Path.GetFileName(filePath);

                S3Upload uploadInfo;
                {
                    UnityWebRequest getURLRequest = LookingGlassWebRequests.CreateRequestToGetFileUploadURL(fileName);
                    try {
                        Debug.Log("Getting upload file URL from " + getURLRequest.url);
                        try {
                            //TODO: Handle exception from 404 error invalid session, """{"error":"Invalid session"}"""
                            //Is there a way to check if our session is valid first, rather than this API saying we're logged in?
                            uploadInfo = await getURLRequest.SendAsync<S3Upload>(NetworkErrorBehaviour.Exception, 0);
                            args.imageUrl = uploadInfo.url + "/" + uploadInfo.key;
                        } catch (Exception e) {
                            printedExceptionText = true;
                            printResult(LogType.Error, GetErrorPrefix() + "An error occurred while trying to get the AWS S3 upload URL!\n\n" + e.GetType().Name + ": " + e.Message);
                            Debug.LogError("An error occurred while trying to get the AWS S3 upload URL!\n" + e);
                            Debug.LogException(e);
                            throw;
                        }
                    } finally {
                        getURLRequest.FullyDispose();
                    }
                }
                Debug.Log("Retrieved upload URL!\n" + uploadInfo.preSignedUrl + "\n");
                updateProgressText("Reading File...");

                //NOTE: We need .NET Standard 2.1+ for File.ReadAllBytesAsync() unfortunately, so we do this instead:
                byte[] fileBytes;
                using (FileStream stream = File.OpenRead(filePath)) {
                    fileBytes = new byte[stream.Length];
                    await stream.ReadAsync(fileBytes, 0, fileBytes.Length);
                }

                args.fileSize = fileBytes.Length;
                updateProgressText("Uploading...");

                //NOTE: We want no timeout for uploading files. They could be quite large
                request = LookingGlassWebRequests.CreateRequestToUploadFile(uploadInfo.preSignedUrl, fileBytes);
                Task uploadTask = request.SendAsync(NetworkErrorBehaviour.Exception, 0);
                uploadRequestSetter(request);

                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileBytes);

                args.width = tex.width;
                args.height = tex.height;

                if (useQuiltSuffix) {
                    GroupCollection groups = m.Groups;
                    args.quiltCols = int.Parse(groups["quiltColumns"].Value);
                    args.quiltRows = int.Parse(groups["quiltRows"].Value);
                    args.aspectRatio = float.Parse(groups["aspect"].Value);
                }

                Object.DestroyImmediate(tex);

                try {
                    setArgs(args);
                    await uploadTask;
                } catch (Exception e) {
                    printedExceptionText = true;
                    printResult(LogType.Error, GetErrorPrefix() + "An error occurred while trying to upload to " + uploadInfo.preSignedUrl + "!\n\n" + e.GetType().Name + ": " + e.Message);
                    Debug.LogError("An error occurred while trying to upload to " + uploadInfo.preSignedUrl + "!\naccessToken = " + AccessToken);
                    Debug.LogException(e);
                    throw;
                }

                updateProgressText("Creating Hologram...");
                Debug.Log("Upload complete, now sending a request to create the hologram with your user account...");
                try {
                    setArgs(args);
                    HologramData data = await LookingGlassWebRequests.SendRequestToCreateQuiltHologram(args);
                    hologramId = data.id;
                    setResult(data);
                } catch (Exception e) {
                    printedExceptionText = true;
                    printResult(LogType.Error, GetErrorPrefix() + "An error occurred during the GraphQL request to create the hologram!\n\n" + e.GetType().Name + ": " + e.Message);
                    Debug.LogError("An error occurred during the GraphQL request to create the hologram!");
                    Debug.LogException(e);
                    throw;
                }
                updateProgressText("DONE!");
                if (!hasWarnings)
                    printResult(LogType.Log, "Your hologram has been successfully uploaded and can be viewed here:\n" + LookingGlassWebRequests.GetViewURL(LookingGlassUser.Username, hologramId));
                Debug.Log("DONE!");
            } catch (Exception e) {
                if (!printedExceptionText) {
                    Debug.LogException(e);
                    printResult(LogType.Error, GetErrorPrefix() + e.Message);
                }
                throw;
            } finally {
                if (request != null)
                    request.FullyDispose();
            }
        }

        public static async Task DeleteBlock(int id) => await LookingGlassWebRequests.SendRequestToDeleteHolograms(Enumerable.Repeat(id, 1));
        public static async Task DeleteBlocks(IEnumerable<int> ids) => await LookingGlassWebRequests.SendRequestToDeleteHolograms(ids);
    }
}
