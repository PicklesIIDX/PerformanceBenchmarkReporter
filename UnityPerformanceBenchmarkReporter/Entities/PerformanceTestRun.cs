using System;
using System.Collections.Generic;

namespace UnityPerformanceBenchmarkReporter.Entities
{
    [Serializable]
    public class PerformanceTestRun
    {
        public PlayerSystemInfo Hardware;
        public EditorVersion Editor;
        public BuildSettings BuildSettings;
        public ScreenSettings ScreenSettings;
        public QualitySettings QualitySettings;
        public PlayerSettings Player;
        public string TestSuite;
        public double Date;
        public double EndTime;
        public List<PerformanceTestResult> Results  = new List<PerformanceTestResult>();
    }

    [Serializable]
    public class PlayerSystemInfo
    {
        public string OperatingSystem;
        public string DeviceModel;
        public string DeviceName;
        public string ProcessorType;
        public int ProcessorCount;
        public string GraphicsDeviceName;
        public int SystemMemorySizeMB;
        public string XrModel;
        public string XrDevice;
    }

    [Serializable]
    public class EditorVersion
    {
        public string Version;
        public int Date;
        public string Branch;
        public string Changeset;
    }

    [Serializable]
    public class BuildSettings
    {
        public string Platform;
        public string BuildTarget;
        public bool DevelopmentPlayer;
        public string AndroidBuildSystem;
    }

    [Serializable]
    public class ScreenSettings
    {
        public int ScreenWidth;
        public int ScreenHeight;
        public int ScreenRefreshRate;
        public bool Fullscreen;
    }

    [Serializable]
    public class QualitySettings
    {
        public int Vsync;
        public int AntiAliasing;
        public string ColorSpace;
        public string AnisotropicFiltering;
        public string BlendWeights;
    }

    [Serializable]
    public class PlayerSettings
    {
        public string Platform;
        public bool Development;
        public int ScreenWidth;
        public int ScreenHeight;
        public int ScreenRefreshRate;
        public bool Fullscreen;
        public int Vsync;
        public int AntiAliasing;
        public string ColorSpace;
        public string AnisotropicFiltering;
        public string BlendWeights;
        public string GraphicsApi;
        public bool Batchmode;
        public string RenderThreadingMode;
        public bool GpuSkinning;
        public string ScriptingBackend;
        public string AndroidTargetSdkVersion;
        public string AndroidApiLevelAuto;
        public string AndroidBuildSystem;
        public string BuildTarget;
        public string StereoRenderingPath;
        public string ScriptingRuntimeVersion;
        public bool VrSupported;
        public bool MtRendering;
        public bool GraphicsJobs;
        public string AndroidMinimumSdkVersion;
        public List<string> EnabledXrTargets;
    }
}