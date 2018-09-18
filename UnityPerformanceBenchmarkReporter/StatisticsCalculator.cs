using System;
using System.Collections.Generic;
using UnityPerformanceBenchmarkReporter.Entities;

namespace Unity.PerformanceTests.Reporter
{
    static class StatisticsCalculator
    {
        public static void CalculateStatisticalValuesForSampleGroup(SampleGroup sampleGroup)
        {
            var samples = sampleGroup.Samples;
            if (samples.Count < 2)
            {
                sampleGroup.Min = samples[0];
                sampleGroup.Max = samples[0];
                sampleGroup.Median = samples[0];
                sampleGroup.Average = samples[0];
                sampleGroup.PercentileValue = 0.0;
                sampleGroup.Zeroes = GetZeroValueCount(samples);
                sampleGroup.SampleCount = sampleGroup.Samples.Count;
                sampleGroup.Sum = samples[0];
                sampleGroup.StandardDeviation = 0.0;
            }
            else
            {
                sampleGroup.Min = Min(samples);
                sampleGroup.Max = Max(samples);
                sampleGroup.Median = GetMedianValue(samples);
                sampleGroup.Average = Average(samples);
                sampleGroup.PercentileValue = GetPercentile(samples, sampleGroup.Definition.Percentile);
                sampleGroup.Zeroes = GetZeroValueCount(samples);
                sampleGroup.SampleCount = sampleGroup.Samples.Count;
                sampleGroup.Sum = Sum(samples);
                sampleGroup.StandardDeviation = GetStandardDeviation(samples, sampleGroup.Average);
            }
        }

        public static int GetZeroValueCount(List<double> samples)
        {
            var zeroValues = 0;
            foreach (var sample in samples)
            {
                if (Math.Abs(sample) < .0001f)
                {
                    zeroValues++;
                }
            }

            return zeroValues;
        }

        public static double GetMedianValue(List<double> samples)
        {
            var samplesClone = new List<double>(samples);
            samplesClone.Sort();

            var middleIdx = samplesClone.Count / 2;
            return samplesClone[middleIdx];
        }

        public static double GetPercentile(List<double> samples, double percentile)
        {
            if (percentile < 0.00001D)
                return percentile;

            var samplesClone = new List<double>(samples);
            samplesClone.Sort();

            if (samplesClone.Count == 1)
            {
                return samplesClone[0];
            }

            var rank = percentile * (samplesClone.Count + 1);
            var integral = (int)rank;
            var fractional = rank % 1;
            return samplesClone[integral - 1] + fractional * (samplesClone[integral] - samplesClone[integral - 1]);
        }

        public static double GetStandardDeviation(List<double> samples, double average)
        {
            double sumOfSquaresOfDifferences = 0.0D;
            foreach (var sample in samples)
            {
                sumOfSquaresOfDifferences += (sample - average) * (sample - average);
            }

            return Math.Sqrt(sumOfSquaresOfDifferences / samples.Count);
        }

        public static double Min(List<double> samples)
        {
            double min = Double.MaxValue;
            foreach (var sample in samples)
            {
                if (sample < min) min = sample;
            }

            return min;
        }

        public static double Max(List<double> samples)
        {
            double max = Double.MinValue;
            foreach (var sample in samples)
            {
                if (sample > max) max = sample;
            }

            return max;
        }

        public static double Average(List<double> samples)
        {
            return Sum(samples) / samples.Count;
        }

        public static double Sum(List<double> samples)
        {
            double sum = 0.0D;
            foreach (var sample in samples)
            {
                sum += sample;
            }

            return sum;
        }
    }
}
