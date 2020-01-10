using System;
using System.Collections.Generic;
using System.Linq;
using UnityPerformanceBenchmarkReporter.Entities;

namespace UnityPerformanceBenchmarkReporter
{
    internal enum MeasurementResult
    {
        Neutral = 0,
        Regression = 1,
        Progression = 2
    }

    public class PerformanceTestRunProcessor
    {
        public List<TestResult> GetTestResults(
            PerformanceTestRun performanceTestRun)
        {
            var mergedTestExecutions = MergeTestExecutions(performanceTestRun);
            var performanceTestResults = new List<TestResult>();
            foreach (var testName in mergedTestExecutions.Keys)
            {
                var performanceTestResult = new TestResult
                {
                    TestName = testName,
                    TestCategories = performanceTestRun.Results.First(r => r.Name == testName).Categories,
                    TestVersion = performanceTestRun.Results.First(r => r.Name == testName).Version,
                    State = (int) TestState.Success,
                    SampleGroupResults = new List<SampleGroupResult>()
                };
                foreach (var sampleGroup in mergedTestExecutions[testName])
                {
                    var sampleGroupResult = new SampleGroupResult
                    {
                        SampleGroupName = sampleGroup.Name,
                        // SampleUnit = sampleGroup.Definition.SampleUnit.ToString(),
                        IncreaseIsBetter = sampleGroup.IncreaseIsBetter,
                        // Threshold = sampleGroup.Definition.Threshold,
                        // AggregationType = sampleGroup.Definition.AggregationType.ToString(),
                        // Percentile = sampleGroup.Definition.Percentile,
                        Min = sampleGroup.Min,
                        Max = sampleGroup.Max,
                        Median = sampleGroup.Median,
                        Average = sampleGroup.Average,
                        StandardDeviation = sampleGroup.StandardDeviation,
                        PercentileValue = sampleGroup.PercentileValue,
                        Sum = sampleGroup.Sum,
                        Zeroes = sampleGroup.Zeroes,
                        SampleCount = sampleGroup.SampleCount,
                        BaselineValue = -1,
                        AggregatedValue = GetAggregatedSampleValue(sampleGroup)
                    };

                    performanceTestResult.SampleGroupResults.Add(sampleGroupResult);
                }
                performanceTestResults.Add(performanceTestResult);
            }
            return performanceTestResults;
        }

        public void UpdateTestResultsBasedOnBaselineResults(List<TestResult> baselineTestResults,
            List<TestResult> testResults, uint sigfig)
        {
            foreach (var testResult in testResults)
            {
                if (baselineTestResults.All(r => r.TestName != testResult.TestName)) continue;
                var baselineSampleGroupResults = baselineTestResults.First(r => r.TestName == testResult.TestName).SampleGroupResults;
                foreach (var sampleGroupResult in testResult.SampleGroupResults)
                {
                    if (baselineSampleGroupResults.Any(sg => sg.SampleGroupName == sampleGroupResult.SampleGroupName))
                    {
                        var baselineSampleGroupResult = baselineSampleGroupResults.First(sg =>
                            sg.SampleGroupName == sampleGroupResult.SampleGroupName);
                        sampleGroupResult.BaselineValue = baselineSampleGroupResult.AggregatedValue;
                        sampleGroupResult.Regressed = DeterminePerformanceResult(sampleGroupResult, sigfig) == MeasurementResult.Regression;
                    }
                }

                if (testResult.SampleGroupResults.Any(r => r.Regressed))
                {
                    testResult.State = (int) TestState.Failure;
                }
            }
        }

        public Dictionary<string, List<SampleGroup>> MergeTestExecutions(PerformanceTestRun performanceTestRun)
        {
            var mergedTestExecutions = new Dictionary<string, List<SampleGroup>>();
            var testNames = performanceTestRun.Results.Select(te => te.Name).Distinct().ToList();
            foreach (var testName in testNames)
            {
                var executions = performanceTestRun.Results.Where(te => te.Name == testName);
                var sampleGroups = new List<SampleGroup>();
                foreach (var execution in executions)
                {
                    foreach (var sampleGroup in execution.SampleGroups)
                    {
                        if (sampleGroups.Any(sg => sg.Name == sampleGroup.Name))
                        {
                            sampleGroups.First(sg => sg.Name == sampleGroup.Name).Samples
                                .AddRange(sampleGroup.Samples);
                        }
                        else
                        {
                            sampleGroups.Add(sampleGroup);
                        }
                    }
                }

                mergedTestExecutions.Add(testName, sampleGroups);
            }
            return mergedTestExecutions;
        }

        private double GetAggregatedSampleValue(SampleGroup sampleGroup)
        {
            return sampleGroup.Average;
            
            // double aggregatedSampleValue;
            // switch (sampleGroup.Definition.AggregationType)
            // {
            //     case AggregationType.Average:
            //         aggregatedSampleValue = sampleGroup.Average;
            //         break;
            //     case AggregationType.Min:
            //         aggregatedSampleValue = sampleGroup.Min;
            //         break;
            //     case AggregationType.Max:
            //         aggregatedSampleValue = sampleGroup.Max;
            //         break;
            //     case AggregationType.Median:
            //         aggregatedSampleValue = sampleGroup.Median;
            //         break;
            //     case AggregationType.Percentile:
            //         aggregatedSampleValue = sampleGroup.PercentileValue;
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException(string.Format("Unhandled aggregation type {0}", sampleGroup.Definition.AggregationType));
            // }
            // return aggregatedSampleValue;
        }

        private MeasurementResult DeterminePerformanceResult(SampleGroupResult sampleGroup, uint sigFig)
        {
            var measurementResult = MeasurementResult.Neutral;
            var positiveThresholdValue = sampleGroup.BaselineValue + sampleGroup.BaselineValue * sampleGroup.Threshold;
            var negativeThresholdValue = sampleGroup.BaselineValue - sampleGroup.BaselineValue * sampleGroup.Threshold;
            if (sampleGroup.IncreaseIsBetter)
            {
                if (sampleGroup.AggregatedValue.TruncToSigFig(sigFig) < negativeThresholdValue.TruncToSigFig(sigFig))
                {
                    measurementResult = MeasurementResult.Regression;
                }
                if (sampleGroup.AggregatedValue.TruncToSigFig(sigFig) > positiveThresholdValue.TruncToSigFig(sigFig))
                {
                    measurementResult = MeasurementResult.Progression;
                }
            }
            else
            {
                if (sampleGroup.AggregatedValue.TruncToSigFig(sigFig) > positiveThresholdValue.TruncToSigFig(sigFig))
                {
                    measurementResult = MeasurementResult.Regression;
                }
                if (sampleGroup.AggregatedValue.TruncToSigFig(sigFig) < negativeThresholdValue.TruncToSigFig(sigFig))
                {
                    measurementResult = MeasurementResult.Progression;
                }
            }
            return measurementResult;
        }

        public PerformanceTestRunResult CreateTestRunResult(PerformanceTestRun runResults,
            List<TestResult> testResults, string resultName, bool isBaseline = false)
        {
            var performanceTestRunResult = new PerformanceTestRunResult
            {
                ResultName = resultName,
                IsBaseline = isBaseline,
                TestSuite = runResults.TestSuite,
                StartTime = DateTime.Parse(runResults.Date),
                TestResults = testResults,
                PlayerSystemInfo = runResults.Hardware,
                EditorVersion = runResults.Editor,
                BuildSettings = runResults.BuildSettings,
                ScreenSettings = runResults.ScreenSettings,
                QualitySettings = runResults.QualitySettings,
                PlayerSettings = runResults.Player
            };
            return performanceTestRunResult;
        }
    }
}
