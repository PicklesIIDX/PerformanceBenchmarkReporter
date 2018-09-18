using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Unity.PerformanceTests.Reporter;
using UnityPerformanceBenchmarkReporter.Entities;

namespace UnityPerformanceBenchmarkReporter
{
    public class PerformanceTestRunV1
    {
        public string platform;
        public DateTime startTime;
        public DateTime endTime;
        public PerformanceTestSystemInfo systemInfo;
        public PerformanceTestEditorInfo editorInfo;
        public ProductVersion productVersion;
        public List<PlayerSetting> playerSettings;
        public List<PerformanceTestExecution> testExecutions;
    }

    public class PlayerSetting
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ProductVersion
    {
        public string productName;
        public string fullVersion;
        public int revisionValue;
        public int dateSeconds;
        public string majorVersion;
        public string minorVersion;
        public string revisionVersion;
        public string changeset;
        public string revision;
        public string branch;
        public DateTime date;
        public string ancestorChangeset;
        public string ancestorBranch;
        public DateTime ancestorDate;
    }

    public class PerformanceTestSystemInfo
    {
        public string operatingSystem;
        public string deviceModel;
        public string deviceName;
        public string processorType;
        public int processorCount;
        public string graphicsDeviceName;
        public int systemMemorySize;
        public string xrDeviceModel;
    }

    public class PerformanceTestResultV1
    {
        public string testName;
        public string testCategory;
        public string testVersion;
        public int state;
        public PerformanceTestSystemInfo systemInfo;
        public PerformanceTestEditorInfo editorInfo;
        public List<PlayerSetting> playerSettings;
        public List<PerformanceTestSampleGroup> sampleGroups;
        public List<SampleGroupResult> sampleGroupResults;
    }

    public class PerformanceTestEditorInfo
    {
        public string fullUnityVersion;
        public int unityRevision;
        public string unityBuildBranch;
        public int unityVersionDate;
    }

    public class PerformanceTestExecution
    {
        public string testName;
        public string testSuite;
        public string testCategory;
        public string testVersion;
        public DateTime startTime;
        public DateTime endTime;
        public List<PerformanceTestSampleGroup> sampleGroups;
    }

    public class PerformanceTestSampleGroup
    {
        public string name;
        public double threshold;
        public bool increaseIsBetter;
        public AggregationType aggregationType;
        public double percentile;
        public List<PerformanceTestSample> samples;

        public PerformanceTestSampleGroup()
        {
            samples = new List<PerformanceTestSample>();
            threshold = 0.1;
            increaseIsBetter = false;
            aggregationType = AggregationType.Median;
        }
    }
    public class PerformanceTestSample
    {
        public double value;
        public string contextName;
        public string contextValue;
        public bool isMedianOfRun;
    }

    public class TestResultXmlParser
    {
        public PerformanceTestRun GetPerformanceTestRunFromXml(string resultXmlFileName)
        {
            ValidateInput(resultXmlFileName);
            var xmlDocument = TryLoadResultXmlFile(resultXmlFileName);
            var performanceTestRun = TryParseXmlToPerformanceTestRun(xmlDocument);
            return performanceTestRun;
        }

        private void ValidateInput(string resultXmlFileName)
        {
            if (string.IsNullOrEmpty(resultXmlFileName))
            {
                throw new ArgumentNullException(resultXmlFileName, nameof(resultXmlFileName));
            }

            if (!File.Exists(resultXmlFileName))
            {
                throw new FileNotFoundException("Result file not found; {0}", resultXmlFileName);
            }
        }

        private XDocument TryLoadResultXmlFile(string resultXmlFileName)
        {
            try
            {
                return XDocument.Load(resultXmlFileName);
            }
            catch (Exception e)
            {
                var errMsg = string.Format("Failed to load xml result file: {0}", resultXmlFileName);
                WriteExceptionConsoleErrorMessage(errMsg, e);
                throw;
            }
        }

        private PerformanceTestRun TryParseXmlToPerformanceTestRun(XDocument xmlDocument)
        {
            var output = xmlDocument.Descendants("output").ToArray();
            if (output == null || !output.Any())
            {
                throw new Exception("The xmlDocument passed to the TryParseXmlToPerformanceTestRun method does not have any \'ouput\' xml tags needed for correct parsing.");
            }

            var testRun = xmlDocument.Descendants("test-results");
            var startTime = ParseStartTime(testRun);
            var testSuite = xmlDocument.Descendants("test-suite");
            var runDuration = int.Parse(testSuite.Attributes().First(a => a.Name == "time").Value);
            var endTime = startTime.AddMilliseconds(runDuration);
            return ParseTestExecutions(xmlDocument, startTime, endTime);
        }

        static DateTime ParseStartTime(IEnumerable<XElement> testRun)
        {
            var startDateTime = DateTime.Now;
            var startTimeString = "";
            try
            {
                var startTime = testRun.Attributes().First(a => a.Name == "time").Value;
                var startDate = testRun.Attributes().First(a => a.Name == "date").Value;
                startTimeString = startDate + " " + startTime;
                var month = int.Parse(startDate.Split('/')[0]);
                var day = int.Parse(startDate.Split('/')[1]);
                var year = int.Parse(startDate.Split('/')[2]);
                var hours = int.Parse(startTime.Split(':')[0]);
                var minutes = int.Parse(startTime.Split(':')[1].Split(' ')[0]);
                var period = "";
                var datetimeFormat = "ddMMyyyyHHmm";
                if (startTime.Split(':')[1].Split(' ').Length > 1)
                {
                    period = startTime.Split(':')[1].Split(' ')[1];
                    datetimeFormat = "ddMMyyyyhhmmtt";
                }
                var datetimeString = day.ToString("D2") +
                                     month.ToString("D2") +
                                     year.ToString("D4") +
                                     hours.ToString("D2") +
                                     minutes.ToString("D2") +
                                     period;
                startDateTime = DateTime.ParseExact(datetimeString, datetimeFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
                Console.WriteLine("Failed to parse start time. " + startTimeString);
            }
            return startDateTime;
        }

        static PerformanceTestRun ParseTestExecutions(
            XDocument xmlDocument,
            DateTime startTime,
            DateTime endTime)
        {
            var performanceTestRun = new PerformanceTestRunV1();
            performanceTestRun.testExecutions = new List<PerformanceTestExecution>();
            var runInfoDefined = false;
            var testcases = xmlDocument.Descendants("test-case");
            var currentTime = startTime;
            foreach (var testcase in testcases)
            {
                var attributes = testcase.Attributes();
                var testName = attributes.First(a => a.Name == "name").Value;
                var executionTime = double.Parse(attributes.First(a => a.Name == "time").Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture);
                var result = attributes.First(a => a.Name == "result").Value;
                if (result == "Success")
                {
                    var testExecution = new PerformanceTestExecution();
                    testExecution.testName = testName;
                    testExecution.startTime = currentTime;
                    currentTime = currentTime.AddSeconds(executionTime);
                    testExecution.endTime = currentTime;

                    var performanceData = testcase.Value;
                    PerformanceTestResultV1 performanceTestResult = null;
                    bool failedToParse;
                    try
                    {
                        performanceTestResult = JsonConvert.DeserializeObject<PerformanceTestResultV1>(performanceData);
                        failedToParse = false;
                    }
                    catch
                    {
                        failedToParse = true;
                    }

                    if (performanceTestResult == null || failedToParse)
                    {
                        Console.Error.WriteLine("Failed to parse performance data for test: " + testName);
                        break;
                    }

                    testExecution.sampleGroups = performanceTestResult.sampleGroups;
                    performanceTestRun.testExecutions.Add(testExecution);
                    testExecution.testCategory = performanceTestResult.testCategory;
                    testExecution.testVersion = performanceTestResult.testVersion;
                    if (!runInfoDefined)
                    {
                        performanceTestRun.startTime = startTime;
                        performanceTestRun.endTime = endTime;
                        performanceTestRun.editorInfo = performanceTestResult.editorInfo;
                        performanceTestRun.playerSettings = performanceTestResult.playerSettings;
                        performanceTestRun.systemInfo = performanceTestResult.systemInfo;
                        runInfoDefined = true;
                    }
                }
            }

            var newRun = new PerformanceTestRun();
            newRun.EditorVersion = new EditorVersion();
            newRun.EditorVersion.Branch = performanceTestRun.editorInfo.unityBuildBranch;
            newRun.EditorVersion.FullVersion = performanceTestRun.editorInfo.fullUnityVersion;
            newRun.EditorVersion.DateSeconds = performanceTestRun.editorInfo.unityVersionDate;
            newRun.EditorVersion.RevisionValue = performanceTestRun.editorInfo.unityRevision;
            newRun.PlayerSystemInfo = new PlayerSystemInfo();
            newRun.PlayerSystemInfo.DeviceModel = performanceTestRun.systemInfo.deviceModel;
            newRun.PlayerSystemInfo.DeviceName = performanceTestRun.systemInfo.deviceName;
            newRun.PlayerSystemInfo.GraphicsDeviceName = performanceTestRun.systemInfo.graphicsDeviceName;
            newRun.PlayerSystemInfo.OperatingSystem = performanceTestRun.systemInfo.operatingSystem;
            newRun.PlayerSystemInfo.ProcessorType = performanceTestRun.systemInfo.processorType;
            newRun.PlayerSystemInfo.ProcessorCount = performanceTestRun.systemInfo.processorCount;
            newRun.PlayerSystemInfo.XrModel = performanceTestRun.systemInfo.xrDeviceModel;
            newRun.StartTime = performanceTestRun.startTime.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ).TotalMilliseconds;
            newRun.EndTime = performanceTestRun.startTime.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ).TotalMilliseconds;
            var results = new List<PerformanceTestResult>();
            foreach (var testExecution in performanceTestRun.testExecutions)
            {
                var result = new PerformanceTestResult()
                {
                    StartTime = testExecution.startTime.ToUniversalTime().Subtract(
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    ).TotalMilliseconds,
                    EndTime = testExecution.endTime.ToUniversalTime().Subtract(
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    ).TotalMilliseconds,
                    TestName = testExecution.testName
                };
                var sampleGroups = new List<SampleGroup>();
                foreach (var sampleGroup in testExecution.sampleGroups)
                {
                    var newSampleGroup = new SampleGroup()
                    {
                        Definition = new SampleGroupDefinition()
                        {
                            Name = sampleGroup.name,
                            AggregationType = AggregationType.Median,
                            IncreaseIsBetter = sampleGroup.increaseIsBetter,
                            Percentile = 0,
                            SampleUnit = SampleUnit.Microsecond,
                            Threshold = sampleGroup.threshold
                        },
                        Samples = sampleGroup.samples.Select(s => s.value).ToList()
                    };
                    StatisticsCalculator.CalculateStatisticalValuesForSampleGroup(newSampleGroup);
                    sampleGroups.Add(newSampleGroup);
                }

                result.SampleGroups = sampleGroups;
                results.Add(result);
            }
            newRun.Results = results;

            return newRun;
        }

        private void WriteExceptionConsoleErrorMessage(string errMsg, Exception e)
        {
            Console.Error.WriteLine("{0}\r\nException: {1}\r\nInnerException: {2}", errMsg, e.Message,
                e.InnerException.Message);
        }
    }
}
