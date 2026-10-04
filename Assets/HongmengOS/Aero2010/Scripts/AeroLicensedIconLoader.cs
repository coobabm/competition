using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace HongmengOS.Aero2010
{
    /// <summary>
    /// Loads replaceable PNG artwork without embedding it in a serialized library or sprite atlas.
    /// Place this on the desktop source root that contains its AppElements and ButtonManagers.
    /// IsReady means loading finished, including graceful fallbacks; inspect Errors for failures.
    /// </summary>
    [DefaultExecutionOrder(-1500)]
    [DisallowMultipleComponent]
    public sealed class AeroLicensedIconLoader : MonoBehaviour
    {
        [Serializable]
        public struct ImageBinding
        {
            public Image image;
            public string iconKey;
        }

        public AppLibrary originalLibrary;
        public ImageBinding[] bindings = Array.Empty<ImageBinding>();
        public bool IsReady { get; private set; }
        public IReadOnlyList<string> Errors => errors;
        public AppLibrary RuntimeLibrary => runtimeLibrary;
        public int LoadedIconCount => loaded.Count;

        private const string RelativeDirectory = "HongmengOS-Aero2010/Oxygen";
        private const int MaxFileBytes = 4 * 1024 * 1024;
        private const int MaxDimension = 2048;
        private const long MaxPixels = 1024L * 1024L;
        private static readonly string[] IconKeys = {
            "Calculator", "Commander", "Game Hub", "Mail", "Messaging", "Music Player", "Notepad",
            "Photo Gallery", "Reminder", "Settings", "Video Player", "Web Browser", "Widget Library"
        };
        private static readonly HashSet<string> AllowedKeys = new HashSet<string>(IconKeys, StringComparer.Ordinal);

        private sealed class LoadedIcon
        {
            public Texture2D texture;
            public Sprite sprite;
        }
        private struct ImageState
        {
            public Sprite sprite;
            public Color color;
            public bool preserveAspect;
        }

        private readonly Dictionary<string, LoadedIcon> loaded = new Dictionary<string, LoadedIcon>(StringComparer.Ordinal);
        private readonly Dictionary<Sprite, string> originalSpriteKeys = new Dictionary<Sprite, string>();
        private readonly HashSet<Sprite> ambiguousSprites = new HashSet<Sprite>();
        private readonly HashSet<Sprite> ownedSprites = new HashSet<Sprite>();
        private readonly Dictionary<Sprite, string> ownedSpriteKeys = new Dictionary<Sprite, string>();
        private readonly Dictionary<AppElement, AppLibrary> previousLibraries = new Dictionary<AppElement, AppLibrary>();
        private readonly Dictionary<ButtonManager, Sprite> previousButtonIcons = new Dictionary<ButtonManager, Sprite>();
        private readonly Dictionary<ButtonManager, string> buttonKeys = new Dictionary<ButtonManager, string>();
        private readonly Dictionary<Image, ImageState> previousImages = new Dictionary<Image, ImageState>();
        private readonly List<string> errors = new List<string>();
        private readonly HashSet<string> uniqueErrors = new HashSet<string>(StringComparer.Ordinal);
        private AppLibrary runtimeLibrary;
        private Coroutine routine;
        private UnityWebRequest request;
        private bool useWebRequests;
        private bool initialized;
        private bool destroying;
        private int nextWebIcon;

        public static bool IsSupportedIconKey(string key) => key != null && AllowedKeys.Contains(key);

        private void Awake()
        {
            useWebRequests = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer;
            try
            {
                if (originalLibrary == null)
                {
                    AddError("AppLibrary was not assigned; existing artwork was retained.");
                    FinishLoading();
                    return;
                }
                runtimeLibrary = Instantiate(originalLibrary);
                runtimeLibrary.name = originalLibrary.name + " (replaceable Oxygen runtime icons)";
                runtimeLibrary.hideFlags = HideFlags.DontSave;
                // Explicit copies guarantee that mutations cannot reach the source asset's managed items.
                runtimeLibrary.apps = new List<AppLibrary.AppItem>();
                if (originalLibrary.apps != null)
                    foreach (AppLibrary.AppItem app in originalLibrary.apps)
                    {
                        if (app == null) { AddError("AppLibrary contained an empty entry; that entry was skipped."); continue; }
                        runtimeLibrary.apps.Add(new AppLibrary.AppItem {
                            appTitle = app.appTitle, localizationKey = app.localizationKey,
                            appIconPreview = app.appIconPreview, appIconBig = app.appIconBig,
                            appIconMedium = app.appIconMedium, appIconSmall = app.appIconSmall,
                            gradientLeft = app.gradientLeft, gradientRight = app.gradientRight
                        });
                        if (!IsSupportedIconKey(app.appTitle)) continue;
                        IndexOriginalSprite(app.appIconBig, app.appTitle);
                        IndexOriginalSprite(app.appIconMedium, app.appTitle);
                        IndexOriginalSprite(app.appIconSmall, app.appTitle);
                    }
                BindAppLibraries();
                initialized = true;
                if (useWebRequests) return;
                foreach (string key in IconKeys)
                {
                    try { LoadLocalIcon(key); }
                    catch (Exception exception) { AddError(key + ": " + exception.GetType().Name + " — " + exception.Message); }
                }
                FinishLoading();
            }
            catch (Exception exception)
            {
                AddError("Icon initialization: " + exception.GetType().Name + " — " + exception.Message);
                FinishLoading();
            }
        }

        private void OnEnable()
        {
            if (initialized && useWebRequests && !IsReady && routine == null)
                routine = StartCoroutine(LoadWebIcons());
        }

        private void OnDisable()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            AbortRequest();
        }

        private void LoadLocalIcon(string key)
        {
            if (!IsSupportedIconKey(key)) { AddError("Unsupported icon filename was rejected."); return; }
            string path = Path.Combine(Application.streamingAssetsPath, "HongmengOS-Aero2010", "Oxygen", key + ".png");
            byte[] bytes;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long length = stream.Length;
                if (length < 24 || length > MaxFileBytes) throw new InvalidDataException("PNG file size is outside the permitted range.");
                bytes = new byte[(int)length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count == 0) throw new EndOfStreamException("PNG changed or was truncated during reading.");
                    offset += count;
                }
            }
            DecodeIcon(key, bytes);
        }

        private void DecodeIcon(string key, byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24 || bytes.Length > MaxFileBytes)
                throw new InvalidDataException("PNG file size is outside the permitted range.");
            // Both transports must validate the header before any native image decoding/allocation.
            ValidatePngHeader(bytes);
            Texture2D texture = null;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                texture.hideFlags = HideFlags.DontSave;
                if (!ImageConversion.LoadImage(texture, bytes, true)) throw new InvalidDataException("PNG decoding failed.");
                RegisterIcon(key, texture);
                texture = null; // Ownership moved into loaded.
            }
            finally { if (texture != null) Destroy(texture); }
        }

        private IEnumerator LoadWebIcons()
        {
            while (nextWebIcon < IconKeys.Length)
            {
                string key = IconKeys[nextWebIcon];
                UnityWebRequestAsyncOperation operation = null;
                try
                {
                    string uri = Application.streamingAssetsPath.TrimEnd('/', '\\') + "/" + RelativeDirectory + "/"
                        + UnityWebRequest.EscapeURL(key).Replace("+", "%20") + ".png";
                    request = UnityWebRequest.Get(uri);
                    request.timeout = 15;
                    operation = request.SendWebRequest();
                }
                catch (Exception exception)
                {
                    AddError(key + ": request initialization failed — " + exception.Message);
                    AbortRequest();
                }
                if (operation == null) { nextWebIcon++; continue; }
                try
                {
                    bool tooLarge = false;
                    while (!operation.isDone)
                    {
                        if (request.downloadedBytes > MaxFileBytes) { tooLarge = true; request.Abort(); break; }
                        yield return null;
                    }
                    try
                    {
                        if (tooLarge || request.downloadedBytes > MaxFileBytes)
                            AddError(key + ": downloaded icon exceeded the file size limit.");
                        else if (request.result != UnityWebRequest.Result.Success)
                            AddError(key + ": " + request.error);
                        else DecodeIcon(key, request.downloadHandler.data);
                    }
                    catch (Exception exception) { AddError(key + ": " + exception.GetType().Name + " — " + exception.Message); }
                }
                finally
                {
                    if (request != null) { request.Dispose(); request = null; }
                }
                nextWebIcon++;
            }
            routine = null;
            FinishLoading();
        }

        private void RegisterIcon(string key, Texture2D texture)
        {
            ValidateDimensions(texture.width, texture.height);
            texture.name = "Oxygen replaceable PNG — " + key;
            texture.hideFlags = HideFlags.DontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "Oxygen runtime icon — " + key;
            sprite.hideFlags = HideFlags.DontSave;
            loaded.Add(key, new LoadedIcon { texture = texture, sprite = sprite });
            ownedSprites.Add(sprite);
            ownedSpriteKeys.Add(sprite, key);
        }

        /// <summary>Can be called after adding new scene-local AppElements or button prefabs.</summary>
        public void RefreshBindings()
        {
            if (destroying || runtimeLibrary == null) return;
            BindAppLibraries();
            foreach (AppLibrary.AppItem app in runtimeLibrary.apps)
                if (app != null && app.appTitle != null && loaded.TryGetValue(app.appTitle, out LoadedIcon icon))
                {
                    app.appIconBig = icon.sprite;
                    app.appIconMedium = icon.sprite;
                    app.appIconSmall = icon.sprite;
                    app.appIconPreview = icon.texture;
                }
            foreach (AppElement element in GetComponentsInChildren<AppElement>(true))
            {
                try
                {
                    element.UpdateLibrary();
                    element.UpdateElement();
                    if (element.elementType == AppElement.ElementType.Icon && element.appID != null
                        && loaded.TryGetValue(element.appID, out LoadedIcon icon))
                        AssignImage(element.GetComponent<Image>(), icon.sprite);
                }
                catch (Exception exception) { AddError("AppElement " + element.name + ": " + exception.Message); }
            }
            foreach (ButtonManager button in GetComponentsInChildren<ButtonManager>(true))
            {
                if (!buttonKeys.TryGetValue(button, out string key))
                {
                    if (button.buttonIcon == null || (!originalSpriteKeys.TryGetValue(button.buttonIcon, out key)
                        && !ownedSpriteKeys.TryGetValue(button.buttonIcon, out key))) continue;
                    buttonKeys.Add(button, key);
                    previousButtonIcons.Add(button, GetRestorableSprite(button.buttonIcon));
                }
                if (!loaded.TryGetValue(key, out LoadedIcon icon)) continue;
                button.buttonIcon = icon.sprite;
                // ButtonManager caches/initializes other fields in OnEnable. Avoid invoking UpdateUI early.
                AssignImage(button.normalImageObj, icon.sprite);
                AssignImage(button.highlightImageObj, icon.sprite);
                AssignImage(button.pressedImageObj, icon.sprite);
                AssignImage(button.disabledImageObj, icon.sprite);
            }
            if (bindings == null) return;
            foreach (ImageBinding binding in bindings)
            {
                if (binding.image == null) continue;
                if (!IsSupportedIconKey(binding.iconKey)) { AddError("Rejected binding icon key: " + (binding.iconKey ?? "<null>")); continue; }
                if (loaded.TryGetValue(binding.iconKey, out LoadedIcon icon)) AssignImage(binding.image, icon.sprite);
            }
        }

        private void BindAppLibraries()
        {
            foreach (AppElement element in GetComponentsInChildren<AppElement>(true))
            {
                if (!previousLibraries.ContainsKey(element)) previousLibraries.Add(element,
                    element.appLibrary == runtimeLibrary ? originalLibrary : element.appLibrary);
                // Cache the fallback before UpdateElement can overwrite an already-awake Image.
                if (element.elementType == AppElement.ElementType.Icon) RememberImage(element.GetComponent<Image>());
                element.appLibrary = runtimeLibrary;
            }
        }

        private void AssignImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            RememberImage(image);
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        private void RememberImage(Image image)
        {
            if (image != null && !previousImages.ContainsKey(image)) previousImages.Add(image,
                new ImageState { sprite = GetRestorableSprite(image.sprite, image.GetComponent<AppElement>()),
                    color = image.color, preserveAspect = image.preserveAspect });
        }

        private Sprite GetRestorableSprite(Sprite sprite, AppElement element = null)
        {
            if (sprite == null || !ownedSpriteKeys.TryGetValue(sprite, out string key)) return sprite;
            // A runtime clone may already carry our icon. Never remember it as its own fallback.
            if (originalLibrary == null || originalLibrary.apps == null) return null;
            foreach (AppLibrary.AppItem app in originalLibrary.apps)
            {
                if (app == null || app.appTitle != key) continue;
                AppElement.IconSize size = element != null ? element.iconSize : AppElement.IconSize.Medium;
                Sprite fallback = size == AppElement.IconSize.Small ? app.appIconSmall
                    : size == AppElement.IconSize.Big ? app.appIconBig : app.appIconMedium;
                return fallback != null && !ownedSprites.Contains(fallback) ? fallback : null;
            }
            return null;
        }

        private void IndexOriginalSprite(Sprite sprite, string key)
        {
            if (sprite == null || ambiguousSprites.Contains(sprite)) return;
            if (originalSpriteKeys.TryGetValue(sprite, out string existing) && existing != key)
            {
                originalSpriteKeys.Remove(sprite);
                ambiguousSprites.Add(sprite);
            }
            else originalSpriteKeys[sprite] = key;
        }

        private static void ValidatePngHeader(byte[] bytes)
        {
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (bytes.Length < 24) throw new InvalidDataException("PNG header is incomplete.");
            for (int i = 0; i < signature.Length; i++) if (bytes[i] != signature[i]) throw new InvalidDataException("Only PNG artwork is accepted.");
            if (bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82)
                throw new InvalidDataException("PNG IHDR header is missing.");
            uint width = ReadUInt32(bytes, 16), height = ReadUInt32(bytes, 20);
            if (width > int.MaxValue || height > int.MaxValue) throw new InvalidDataException("PNG dimensions are invalid.");
            ValidateDimensions((int)width, (int)height);
        }

        private static uint ReadUInt32(byte[] bytes, int at)
        {
            return ((uint)bytes[at] << 24) | ((uint)bytes[at + 1] << 16) | ((uint)bytes[at + 2] << 8) | bytes[at + 3];
        }

        private static void ValidateDimensions(int width, int height)
        {
            if (width < 1 || height < 1 || width > MaxDimension || height > MaxDimension || (long)width * height > MaxPixels)
                throw new InvalidDataException("PNG dimensions exceed 2048 per side or 1 megapixel in total.");
        }

        private void FinishLoading()
        {
            try { RefreshBindings(); }
            catch (Exception exception) { AddError("Applying runtime icons: " + exception.Message); }
            IsReady = true;
            if (errors.Count > 0) Debug.LogWarning("[Aero Oxygen icons] Existing artwork was retained where needed:\n" + string.Join("\n", errors), this);
        }

        private void AddError(string error)
        {
            if (uniqueErrors.Add(error)) errors.Add(error);
        }

        private void AbortRequest()
        {
            if (request == null) return;
            request.Abort();
            request.Dispose();
            request = null;
        }

        private void OnDestroy()
        {
            destroying = true;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            AbortRequest();
            foreach (var entry in previousImages)
                if (entry.Key != null && entry.Key.sprite != null && ownedSprites.Contains(entry.Key.sprite))
                {
                    entry.Key.sprite = entry.Value.sprite;
                    entry.Key.color = entry.Value.color;
                    entry.Key.preserveAspect = entry.Value.preserveAspect;
                }
            foreach (var entry in previousButtonIcons)
                if (entry.Key != null && entry.Key.buttonIcon != null && ownedSprites.Contains(entry.Key.buttonIcon))
                    entry.Key.buttonIcon = entry.Value;
            foreach (var entry in previousLibraries)
                if (entry.Key != null && entry.Key.appLibrary == runtimeLibrary)
                    entry.Key.appLibrary = entry.Value != null ? entry.Value : originalLibrary;
            foreach (LoadedIcon icon in loaded.Values)
            {
                if (icon.sprite != null) Destroy(icon.sprite);
                if (icon.texture != null) Destroy(icon.texture);
            }
            if (runtimeLibrary != null) Destroy(runtimeLibrary);
            loaded.Clear();
            ownedSprites.Clear();
            ownedSpriteKeys.Clear();
        }
    }
}
