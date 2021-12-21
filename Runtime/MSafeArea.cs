using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MSafeArea : MonoBehaviour
{
    #region Simulations
    /// <summary>
    /// Simulation device that uses safe area due to a physical notch or software home bar. For use in Editor only.
    /// </summary>
    public enum SimDevice
    {
        /// <summary>
        /// Don't use a simulated safe area - GUI will be full screen as normal.
        /// </summary>
        None,
        /// <summary>
        /// Simulate the iPhone X and Xs (identical safe areas).
        /// </summary>
        iPhoneX,
        /// <summary>
        /// Simulate the iPhone Xs Max and XR (identical safe areas).
        /// </summary>
        iPhoneXsMax,
        /// <summary>
        /// Simulate the Google Pixel 3 XL using landscape left.
        /// </summary>
        Pixel3XL_LSL,
        /// <summary>
        /// Simulate the Google Pixel 3 XL using landscape right.
        /// </summary>
        Pixel3XL_LSR
    }

    /// <summary>
    /// Simulation mode for use in editor only. This can be edited at runtime to toggle between different safe areas.
    /// </summary>
    public static SimDevice Sim = SimDevice.None;

    /// <summary>
    /// Normalised safe areas for iPhone X with Home indicator (ratios are identical to iPhone Xs). Absolute values:
    ///  PortraitU x=0, y=102, w=1125, h=2202 on full extents w=1125, h=2436;
    ///  PortraitD x=0, y=102, w=1125, h=2202 on full extents w=1125, h=2436 (not supported, remains in Portrait Up);
    ///  LandscapeL x=132, y=63, w=2172, h=1062 on full extents w=2436, h=1125;
    ///  LandscapeR x=132, y=63, w=2172, h=1062 on full extents w=2436, h=1125.
    ///  Aspect Ratio: ~19.5:9.
    /// </summary>
    Rect[] NSA_iPhoneX = new Rect[]
    {
            new Rect (0f, 102f / 2436f, 1f, 2202f / 2436f),  // Portrait
            new Rect (132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f),  // Landscape
    };
	Vector2Int iPhoneXSize = new Vector2Int(1125, 2436);

    /// <summary>
    /// Normalised safe areas for iPhone Xs Max with Home indicator (ratios are identical to iPhone XR). Absolute values:
    ///  PortraitU x=0, y=102, w=1242, h=2454 on full extents w=1242, h=2688;
    ///  PortraitD x=0, y=102, w=1242, h=2454 on full extents w=1242, h=2688 (not supported, remains in Portrait Up);
    ///  LandscapeL x=132, y=63, w=2424, h=1179 on full extents w=2688, h=1242;
    ///  LandscapeR x=132, y=63, w=2424, h=1179 on full extents w=2688, h=1242.
    ///  Aspect Ratio: ~19.5:9.
    /// </summary>
    Rect[] NSA_iPhoneXsMax = new Rect[]
    {
            new Rect (0f, 102f / 2688f, 1f, 2454f / 2688f),  // Portrait
            new Rect (132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f),  // Landscape
    };
    Vector2Int iPhoneXsMaxSize = new Vector2Int(1242, 2688);

    /// <summary>
    /// Normalised safe areas for Pixel 3 XL using landscape left. Absolute values:
    ///  PortraitU x=0, y=0, w=1440, h=2789 on full extents w=1440, h=2960;
    ///  PortraitD x=0, y=0, w=1440, h=2789 on full extents w=1440, h=2960;
    ///  LandscapeL x=171, y=0, w=2789, h=1440 on full extents w=2960, h=1440;
    ///  LandscapeR x=0, y=0, w=2789, h=1440 on full extents w=2960, h=1440.
    ///  Aspect Ratio: 18.5:9.
    /// </summary>
    Rect[] NSA_Pixel3XL_LSL = new Rect[]
    {
            new Rect (0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new Rect (0f, 0f, 2789f / 2960f, 1f),  // Landscape
    };
    Vector2Int Pixel3XLSize = new Vector2Int(1440, 2960);

    /// <summary>
    /// Normalised safe areas for Pixel 3 XL using landscape right. Absolute values and aspect ratio same as above.
    /// </summary>
    Rect[] NSA_Pixel3XL_LSR = new Rect[]
    {
            new Rect (0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new Rect (171f / 2960f, 0f, 2789f / 2960f, 1f),  // Landscape
    };

    #endregion

    Vector2 currentResolution;
    RectTransform Panel;
    Rect LastSafeArea = new Rect(0, 0, 0, 0);
    Vector2Int LastScreenSize = new Vector2Int(0, 0);
    ScreenOrientation LastOrientation = ScreenOrientation.AutoRotation;
    [SerializeField] bool ConformX = true;  // Conform to screen safe area on X-axis (default true, disable to ignore)
    [SerializeField] bool ConformY = true;  // Conform to screen safe area on Y-axis (default true, disable to ignore)
    [SerializeField] bool Logging = false;  // Conform to screen safe area on Y-axis (default true, disable to ignore)

    void Awake()
    {
        Panel = GetComponent<RectTransform>();

        if (Panel == null) {
            Debug.LogError("Cannot apply safe area - no RectTransform found on " + name);
            Destroy(gameObject);
        }

        currentResolution = new Vector2(Screen.width, Screen.height);

        Refresh();
    }

    void Update()
    {
        if (currentResolution.x != Screen.width || currentResolution.y != Screen.height) {
			Refresh();
            currentResolution.x = Screen.width;
            currentResolution.y = Screen.height;
		}
	}

    void Refresh()
    {
        Rect safeArea = GetSafeArea();

        if (safeArea != LastSafeArea
            || Screen.width != LastScreenSize.x
            || Screen.height != LastScreenSize.y
            || Screen.orientation != LastOrientation) {
            // Fix for having auto-rotate off and manually forcing a screen orientation.
            // See https://forum.unity.com/threads/569236/#post-4473253 and https://forum.unity.com/threads/569236/page-2#post-5166467
            LastScreenSize.x = Screen.width;
            LastScreenSize.y = Screen.height;
            LastOrientation = Screen.orientation;

            ApplySafeArea(safeArea);
        }
    }

    SimDevice GetSim()
    {
        if (Screen.width == iPhoneXSize.x && Screen.height == iPhoneXSize.y)
            return SimDevice.iPhoneX;

        if (Screen.width == iPhoneXsMaxSize.x && Screen.height == iPhoneXsMaxSize.y)
            return SimDevice.iPhoneXsMax;

        if (Screen.width == Pixel3XLSize.x && Screen.height == Pixel3XLSize.y)
            return SimDevice.Pixel3XL_LSL;

        return SimDevice.None;
	}

	Rect GetSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Sim = GetSim();
        if (Application.isEditor && Sim != SimDevice.None) {

            Rect nsa = new Rect(0, 0, Screen.width, Screen.height);

            switch (Sim) {
                case SimDevice.iPhoneX:
                    if (Screen.height > Screen.width)  // Portrait
                        nsa = NSA_iPhoneX[0];
                    else  // Landscape
                        nsa = NSA_iPhoneX[1];
                    break;
                case SimDevice.iPhoneXsMax:
                    if (Screen.height > Screen.width)  // Portrait
                        nsa = NSA_iPhoneXsMax[0];
                    else  // Landscape
                        nsa = NSA_iPhoneXsMax[1];
                    break;
                case SimDevice.Pixel3XL_LSL:
                    if (Screen.height > Screen.width)  // Portrait
                        nsa = NSA_Pixel3XL_LSL[0];
                    else  // Landscape
                        nsa = NSA_Pixel3XL_LSL[1];
                    break;
                case SimDevice.Pixel3XL_LSR:
                    if (Screen.height > Screen.width)  // Portrait
                        nsa = NSA_Pixel3XL_LSR[0];
                    else  // Landscape
                        nsa = NSA_Pixel3XL_LSR[1];
                    break;
                default:
                    break;
            }

            safeArea = new Rect(Screen.width * nsa.x, Screen.height * nsa.y, Screen.width * nsa.width, Screen.height * nsa.height);
        }

        return safeArea;
    }

    void ApplySafeArea(Rect r)
    {
        LastSafeArea = r;

        // Ignore x-axis?
        if (!ConformX) {
            r.x = 0;
            r.width = Screen.width;
        }

        // Ignore y-axis?
        if (!ConformY) {
            r.y = 0;
            r.height = Screen.height;
        }

		// Convert safe area rectangle from absolute pixels to normalised anchor coordinates
		Vector2 anchorMin = r.position;
		Vector2 anchorMax = r.position + r.size;
		anchorMin.x /= Screen.width;
		anchorMin.y /= Screen.height;
		anchorMax.x /= Screen.width;
		anchorMax.y /= Screen.height;
		Panel.anchorMin = anchorMin;
		Panel.anchorMax = anchorMax;

		if (Logging) {
            Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}",
            name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
        }
    }
}
