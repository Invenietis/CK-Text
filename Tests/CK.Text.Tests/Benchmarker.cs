using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Text.Tests
{
    public class BenchmarkResult
    {
        public readonly IReadOnlyList<double> Timings;

        public readonly double MinTiming;

        public readonly double MaxTiming;

        public readonly double Average;

        public readonly double StandardDeviation;

        public readonly double MeanAbsoluteDeviation;

        public readonly double NormalizedMean;

        /// <summary>
        /// Uses <see cref="NormalizedMean"/> to test whether this result is better than the other one.
        /// </summary>
        /// <param name="other">Other result. Must not be null.</param>
        /// <returns>True if this result is better than the other one.</returns>
        public bool IsBetterThan( BenchmarkResult other )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            return NormalizedMean < other.NormalizedMean;
        }

        /// <summary>
        /// Uses <see cref="NormalizedMean"/> and <see cref="StandardDeviation"/> to test whether this
        /// result is significantly better than the other one.
        /// </summary>
        /// <param name="other">Other result. Must not be null.</param>
        /// <returns>True if this result is better than the other one.</returns>
        public bool IsSignificantlyBetterThan( BenchmarkResult other )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            return (NormalizedMean + StandardDeviation / 2) < (other.NormalizedMean - other.StandardDeviation / 2);
        }

        /// <summary>
        /// This result is totally better than the other one if its worst timing (<see cref="MaxTiming"/>)
        /// is still better than the best other's timing (<see cref="MinTiming"/>).
        /// </summary>
        /// <param name="other">Other result. Must not be null.</param>
        /// <returns>True if this result is better than the other one.</returns>
        public bool IsTotallyBetterThan( BenchmarkResult other )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            return MaxTiming < other.MinTiming;
        }

        public BenchmarkResult( double[] timings )
        {
            if( timings == null || timings.Length < 2 ) throw new ArgumentException();
            double sum = 0;
            double sumSquare = 0;
            double minTiming = double.MaxValue;
            double maxTiming = 0;
            for( int i = 0; i < timings.Length; ++i )
            {
                var t = timings[i];
                sum += t;
                sumSquare += t * t;
                if( minTiming > t ) minTiming = t;
                if( maxTiming < t ) maxTiming = t;
            }
            double average = sum / timings.Length;
            double stdDev = Math.Sqrt( sumSquare/timings.Length - average * average );
            double[] deviations = new double[timings.Length];
            for( int i = 0; i < timings.Length; ++i )
            {
                deviations[i] = average - timings[i];
            }
            double meanDeviation = 0;
            for( int i = 0; i < timings.Length; ++i )
            {
                meanDeviation += Math.Abs( deviations[i] );
            }
            meanDeviation /= timings.Length;
            double normalizedMean = 0;
            int normalizedMeanCount = 0;
            for( int i = 0; i < timings.Length; ++i )
            {
                if( deviations[i] > 0 || -deviations[i] <= meanDeviation )
                {
                    normalizedMean += timings[i];
                    ++normalizedMeanCount;
                }
            }
            normalizedMean /= normalizedMeanCount;
            //
            Timings = timings;
            MinTiming = minTiming;
            MaxTiming = maxTiming;
            Average = average;
            StandardDeviation = stdDev;
            MeanAbsoluteDeviation = meanDeviation;
            NormalizedMean = normalizedMean;
        }
    }

    /// <summary>
    /// From https://stackoverflow.com/questions/969290/exact-time-measurement-for-performance-testing.
    /// </summary>
    class Benchmarker
    {
        interface IStopwatch
        {
            TimeSpan Elapsed { get; }
            void Start();
            void Stop();
            void Reset();
        }

        class TimeWatch : IStopwatch
        {
            Stopwatch _stopwatch = new Stopwatch();

            public TimeWatch()
            {
                if( !Stopwatch.IsHighResolution )
                    throw new NotSupportedException( "Your hardware doesn't support high resolution counter" );

                // Prevents the JIT Compiler from optimizing Fkt calls away
                long seed = Environment.TickCount;
                // Uses the second Core/Processor for the test
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr( 2 );
                // Prevents "Normal" Processes from interrupting Threads
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                // Prevents "Normal" Threads from interrupting this thread
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }

            public TimeSpan Elapsed => _stopwatch.Elapsed;

            public void Start() => _stopwatch.Start();

            public void Stop() =>  _stopwatch.Stop();

            public void Reset() => _stopwatch.Reset();
        }

        class CpuWatch : IStopwatch
        {
            TimeSpan _startTime;
            TimeSpan _endTime;

            public TimeSpan Elapsed => _endTime - _startTime;

            public void Start()
            {
                _startTime = Process.GetCurrentProcess().TotalProcessorTime;
            }

            public void Stop()
            {
                _endTime = Process.GetCurrentProcess().TotalProcessorTime;
            }

            public void Reset()
            {
                _startTime = TimeSpan.Zero;
                _endTime = TimeSpan.Zero;
            }
        }

        public static BenchmarkResult BenchmarkTime( Action action, int iterations = 10000, int timingCount = 5, bool warmup = true )
        {
            return Benchmark<TimeWatch>( action, iterations, timingCount, warmup );
        }

        public static BenchmarkResult BenchmarkCpu( Action action, int iterations = 10000, int timingCount = 5, bool warmup = true )
        {
            return Benchmark<CpuWatch>( action, iterations, timingCount, warmup );
        }

        static BenchmarkResult Benchmark<T>( Action action, int iterations, int timingCount, bool warmup ) where T : IStopwatch, new()
        {
            // Clean Garbage
            GC.Collect();
            // Wait for the finalizer queue to empty
            GC.WaitForPendingFinalizers();
            // Clean Garbage
            GC.Collect();
            // Warm up
            if( warmup ) action();
            var stopwatch = new T();
            var timings = new double[timingCount];
            for( int i = 0; i < timingCount; i++ )
            {
                stopwatch.Reset();
                stopwatch.Start();
                for( int j = 0; j < iterations; j++ ) action();
                stopwatch.Stop();
                timings[i] = stopwatch.Elapsed.TotalMilliseconds;
            }
            return new BenchmarkResult( timings );
        }

    }
}

