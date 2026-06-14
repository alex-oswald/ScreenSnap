using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace ScreenSnap.Core.Displays;

/// <summary>
/// Display engine built on the Windows Connecting and Configuring Displays (CCD) API
/// (<c>QueryDisplayConfig</c> / <c>SetDisplayConfig</c>). Monitors are identified by their
/// stable <c>monitorDevicePath</c> so presets survive reboots and reconnects even though the
/// underlying adapter LUIDs are not stable.
/// </summary>
public sealed class CcdDisplayService : IDisplayService
{
    private const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;
    private const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xFFFFFFFF;

    /// <inheritdoc />
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var (paths, modes) = Query(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ALL_PATHS);
        var map = new Dictionary<string, MonitorInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            string devicePath = GetDevicePath(path.targetInfo.adapterId, path.targetInfo.id, out string friendly);
            if (string.IsNullOrEmpty(devicePath))
                continue;

            bool active = (path.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0;
            int x = 0, y = 0;
            uint width = 0, height = 0;
            bool primary = false;
            var orientation = DisplayOrientation.Landscape;

            if (active)
            {
                orientation = FromRotation(path.targetInfo.rotation);

                uint idx = path.sourceInfo.Anonymous.modeInfoIdx;
                if (idx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID && idx < (uint)modes.Length &&
                    modes[idx].infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE)
                {
                    var sourceMode = modes[idx].Anonymous.sourceMode;
                    x = sourceMode.position.x;
                    y = sourceMode.position.y;
                    // The source surface is already rotated; report the native (landscape) resolution.
                    (width, height) = IsPortrait(orientation)
                        ? (sourceMode.height, sourceMode.width)
                        : (sourceMode.width, sourceMode.height);
                    primary = x == 0 && y == 0;
                }
            }

            // Prefer the active path when a monitor is reachable through several paths.
            if (!map.TryGetValue(devicePath, out var existing) || (active && !existing.IsActive))
            {
                map[devicePath] = new MonitorInfo
                {
                    DevicePath = devicePath,
                    FriendlyName = string.IsNullOrWhiteSpace(friendly) ? "Display" : friendly,
                    IsActive = active,
                    IsPrimary = primary,
                    Orientation = orientation,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                };
            }
        }

        return map.Values
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.X)
            .ThenBy(m => m.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<DisplayMode> GetAvailableModes(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return Array.Empty<DisplayMode>();

        string gdiName = GetSourceGdiName(devicePath);
        if (string.IsNullOrEmpty(gdiName))
            return Array.Empty<DisplayMode>();

        return EnumerateModes(gdiName);
    }

    /// <inheritdoc />
    public DisplayConfiguration CaptureCurrent()
    {
        var configuration = new DisplayConfiguration();
        foreach (var monitor in GetMonitors())
        {
            configuration.Monitors.Add(new MonitorState
            {
                DevicePath = monitor.DevicePath,
                FriendlyName = monitor.FriendlyName,
                Enabled = monitor.IsActive,
                IsPrimary = monitor.IsPrimary,
                Orientation = monitor.Orientation,
                X = monitor.X,
                Y = monitor.Y,
                Width = monitor.Width,
                Height = monitor.Height,
            });
        }

        return configuration;
    }

    /// <inheritdoc />
    public DisplayApplyResult Apply(DisplayConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var (paths, modes) = Query(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ALL_PATHS);

        // Map each attached monitor's stable device path to a live path index,
        // preferring an active path when one exists.
        var indexByDevice = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < paths.Length; i++)
        {
            string devicePath = GetDevicePath(paths[i].targetInfo.adapterId, paths[i].targetInfo.id, out _);
            if (string.IsNullOrEmpty(devicePath))
                continue;

            bool active = (paths[i].flags & DISPLAYCONFIG_PATH_ACTIVE) != 0;
            if (!indexByDevice.TryGetValue(devicePath, out int existing) ||
                (active && (paths[existing].flags & DISPLAYCONFIG_PATH_ACTIVE) == 0))
            {
                indexByDevice[devicePath] = i;
            }
        }

        var enabled = configuration.Monitors.Where(m => m.Enabled).ToList();
        if (enabled.Count == 0)
            return new DisplayApplyResult { Success = false, Error = "A preset must keep at least one display enabled." };

        // Normalize so the primary display sits at the desktop origin (0,0).
        var primary = enabled.FirstOrDefault(m => m.IsPrimary) ?? enabled[0];
        int originX = primary.X;
        int originY = primary.Y;

        var missing = new List<string>();

        // First attempt: re-use the live target modes for monitors that are already active
        // (preserves their exact resolution / refresh rate); validate before applying.
        bool built = TryBuild(paths, modes, enabled, indexByDevice, originX, originY,
            reuseLiveTargetModes: true, out var newPaths, out var newModes, missing);

        if (!built || SetConfig(newPaths, newModes, validateOnly: true) != 0)
        {
            // Fallback: let the OS compute every target mode from the supplied source modes.
            missing.Clear();
            built = TryBuild(paths, modes, enabled, indexByDevice, originX, originY,
                reuseLiveTargetModes: false, out newPaths, out newModes, missing);

            if (!built)
                return new DisplayApplyResult
                {
                    Success = false,
                    Error = "None of the preset's displays are currently attached.",
                    MissingMonitors = missing,
                };

            int validateCode = SetConfig(newPaths, newModes, validateOnly: true);
            if (validateCode != 0)
                return new DisplayApplyResult
                {
                    Success = false,
                    Error = $"The display configuration is not valid (error {validateCode}).",
                    MissingMonitors = missing,
                };
        }

        int applyCode = SetConfig(newPaths, newModes, validateOnly: false);
        if (applyCode != 0)
            return new DisplayApplyResult
            {
                Success = false,
                Error = $"Applying the display configuration failed (error {applyCode}).",
                MissingMonitors = missing,
            };

        return new DisplayApplyResult { Success = true, MissingMonitors = missing };
    }

    private static bool TryBuild(
        DISPLAYCONFIG_PATH_INFO[] paths,
        DISPLAYCONFIG_MODE_INFO[] modes,
        List<MonitorState> enabled,
        Dictionary<string, int> indexByDevice,
        int originX,
        int originY,
        bool reuseLiveTargetModes,
        out DISPLAYCONFIG_PATH_INFO[] newPaths,
        out DISPLAYCONFIG_MODE_INFO[] newModes,
        List<string> missing)
    {
        var pathList = new List<DISPLAYCONFIG_PATH_INFO>(enabled.Count);
        var modeList = new List<DISPLAYCONFIG_MODE_INFO>(enabled.Count * 2);

        foreach (var monitor in enabled)
        {
            if (!indexByDevice.TryGetValue(monitor.DevicePath, out int idx))
            {
                missing.Add(string.IsNullOrWhiteSpace(monitor.FriendlyName) ? "Display" : monitor.FriendlyName);
                continue;
            }

            var path = paths[idx];
            bool wasActive = (path.flags & DISPLAYCONFIG_PATH_ACTIVE) != 0;
            uint liveSourceIdx = path.sourceInfo.Anonymous.modeInfoIdx;
            uint liveTargetIdx = path.targetInfo.Anonymous.modeInfoIdx;

            bool haveLiveSource = wasActive &&
                liveSourceIdx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID && liveSourceIdx < (uint)modes.Length &&
                modes[liveSourceIdx].infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE;

            // Resolve the live native (un-rotated) resolution and rotation when the monitor is active.
            var liveRotation = wasActive
                ? path.targetInfo.rotation
                : DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_IDENTITY;
            uint liveNativeW = 0, liveNativeH = 0;
            if (haveLiveSource)
            {
                var liveSource = modes[liveSourceIdx].Anonymous.sourceMode;
                (liveNativeW, liveNativeH) = IsPortrait(liveRotation)
                    ? (liveSource.height, liveSource.width)
                    : (liveSource.width, liveSource.height);
            }

            // Resolve the requested rotation and native resolution (falling back to the live values).
            var reqRotation = ToRotation(monitor.Orientation);
            uint reqNativeW = monitor.Width != 0 ? monitor.Width : liveNativeW;
            uint reqNativeH = monitor.Height != 0 ? monitor.Height : liveNativeH;

            // The desktop source surface is sized in rotated pixels for portrait orientations.
            (uint desktopW, uint desktopH) = IsPortrait(reqRotation)
                ? (reqNativeH, reqNativeW)
                : (reqNativeW, reqNativeH);

            bool noChange = haveLiveSource &&
                reqRotation == liveRotation &&
                reqNativeW == liveNativeW &&
                reqNativeH == liveNativeH;

            // Build the source mode that fixes this display's position and size.
            var sourceMode = new DISPLAYCONFIG_MODE_INFO
            {
                infoType = DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE,
                id = path.sourceInfo.id,
                adapterId = path.sourceInfo.adapterId,
            };
            sourceMode.Anonymous.sourceMode = new DISPLAYCONFIG_SOURCE_MODE
            {
                width = desktopW,
                height = desktopH,
                pixelFormat = DISPLAYCONFIG_PIXELFORMAT.DISPLAYCONFIG_PIXELFORMAT_32BPP,
                position = new POINTL { x = monitor.X - originX, y = monitor.Y - originY },
            };

            int sourceModeIndex = modeList.Count;
            modeList.Add(sourceMode);

            path.flags |= DISPLAYCONFIG_PATH_ACTIVE;
            path.sourceInfo.Anonymous.modeInfoIdx = (uint)sourceModeIndex;
            path.targetInfo.rotation = reqRotation;

            // Only reuse the live target mode (which preserves the exact refresh rate) when neither
            // the resolution nor the orientation is changing; otherwise let the OS compute one that
            // matches the supplied source mode.
            if (reuseLiveTargetModes && wasActive && noChange &&
                liveTargetIdx != DISPLAYCONFIG_PATH_MODE_IDX_INVALID && liveTargetIdx < (uint)modes.Length &&
                modes[liveTargetIdx].infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
            {
                int targetModeIndex = modeList.Count;
                modeList.Add(modes[liveTargetIdx]);
                path.targetInfo.Anonymous.modeInfoIdx = (uint)targetModeIndex;
            }
            else
            {
                path.targetInfo.Anonymous.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            }

            pathList.Add(path);
        }

        newPaths = pathList.ToArray();
        newModes = modeList.ToArray();
        return newPaths.Length > 0;
    }

    private static int SetConfig(DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes, bool validateOnly)
    {
        var flags = SET_DISPLAY_CONFIG_FLAGS.SDC_USE_SUPPLIED_DISPLAY_CONFIG |
                    SET_DISPLAY_CONFIG_FLAGS.SDC_ALLOW_CHANGES |
                    SET_DISPLAY_CONFIG_FLAGS.SDC_SAVE_TO_DATABASE |
                    (validateOnly ? SET_DISPLAY_CONFIG_FLAGS.SDC_VALIDATE : SET_DISPLAY_CONFIG_FLAGS.SDC_APPLY);

        return SetDisplayConfig(paths, modes, flags);
    }

    private static (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes) Query(QUERY_DISPLAY_CONFIG_FLAGS flags)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var sizeResult = GetDisplayConfigBufferSizes(flags, out uint pathCount, out uint modeCount);
            if (sizeResult != WIN32_ERROR.ERROR_SUCCESS)
                throw new Win32Exception((int)sizeResult, "GetDisplayConfigBufferSizes failed.");

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            var queryResult = QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes);
            if (queryResult == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                continue; // Topology changed between the two calls; retry with fresh sizes.

            if (queryResult != WIN32_ERROR.ERROR_SUCCESS)
                throw new Win32Exception((int)queryResult, "QueryDisplayConfig failed.");

            if (pathCount < (uint)paths.Length)
                Array.Resize(ref paths, (int)pathCount);
            if (modeCount < (uint)modes.Length)
                Array.Resize(ref modes, (int)modeCount);

            return (paths, modes);
        }

        throw new InvalidOperationException("The display configuration kept changing while it was being read.");
    }

    private static unsafe string GetDevicePath(LUID adapterId, uint targetId, out string friendlyName)
    {
        var request = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
        request.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        request.header.size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
        request.header.adapterId = adapterId;
        request.header.id = targetId;

        int result = DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&request);
        if (result != 0)
        {
            friendlyName = string.Empty;
            return string.Empty;
        }

        friendlyName = request.monitorFriendlyDeviceName.ToString();
        return request.monitorDevicePath.ToString();
    }

    private static unsafe string GetSourceGdiName(string devicePath)
    {
        var (paths, _) = Query(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ALL_PATHS);
        foreach (var path in paths)
        {
            string candidate = GetDevicePath(path.targetInfo.adapterId, path.targetInfo.id, out _);
            if (!string.Equals(candidate, devicePath, StringComparison.OrdinalIgnoreCase))
                continue;

            var request = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            request.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            request.header.size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
            request.header.adapterId = path.sourceInfo.adapterId;
            request.header.id = path.sourceInfo.id;

            if (DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&request) == 0)
                return request.viewGdiDeviceName.ToString();
        }

        return string.Empty;
    }

    private static unsafe IReadOnlyList<DisplayMode> EnumerateModes(string gdiDeviceName)
    {
        var seen = new HashSet<(uint, uint)>();
        var result = new List<DisplayMode>();

        var dm = new DEVMODEW();
        for (uint i = 0; ; i++)
        {
            dm.dmSize = (ushort)sizeof(DEVMODEW);
            // dwFlags = 0 returns each supported mode in the display's default (landscape) orientation.
            if (!EnumDisplaySettingsEx(gdiDeviceName, (ENUM_DISPLAY_SETTINGS_MODE)i, ref dm, 0))
                break;

            uint w = dm.dmPelsWidth;
            uint h = dm.dmPelsHeight;
            if (w == 0 || h == 0)
                continue;

            if (seen.Add((w, h)))
                result.Add(new DisplayMode(w, h));
        }

        result.Sort((a, b) => b.Width != a.Width ? b.Width.CompareTo(a.Width) : b.Height.CompareTo(a.Height));
        return result;
    }

    private static DISPLAYCONFIG_ROTATION ToRotation(DisplayOrientation orientation) => orientation switch
    {
        DisplayOrientation.Portrait => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE90,
        DisplayOrientation.LandscapeFlipped => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE180,
        DisplayOrientation.PortraitFlipped => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE270,
        _ => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_IDENTITY,
    };

    private static DisplayOrientation FromRotation(DISPLAYCONFIG_ROTATION rotation) => rotation switch
    {
        DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE90 => DisplayOrientation.Portrait,
        DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE180 => DisplayOrientation.LandscapeFlipped,
        DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE270 => DisplayOrientation.PortraitFlipped,
        _ => DisplayOrientation.Landscape,
    };

    private static bool IsPortrait(DisplayOrientation orientation) => ((int)orientation & 1) == 1;

    private static bool IsPortrait(DISPLAYCONFIG_ROTATION rotation) =>
        rotation is DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE90
                 or DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE270;
}
