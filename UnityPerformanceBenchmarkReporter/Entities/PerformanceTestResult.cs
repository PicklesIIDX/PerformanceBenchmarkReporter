using System;
using System.Collections.Generic;

namespace UnityPerformanceBenchmarkReporter.Entities
{
    [Serializable]
    public class PerformanceTestResult
    {
        public string Name;
        public List<string> Categories;
        public string Version;
        public double StartTime;
        public double EndTime;
        public List<SampleGroup> SampleGroups;
    }
}