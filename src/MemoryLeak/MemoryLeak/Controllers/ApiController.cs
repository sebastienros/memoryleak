using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryLeak.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private static ConcurrentBag<string> _staticStrings = new ConcurrentBag<string>();
        private static Process _process = Process.GetCurrentProcess();
        private static TimeSpan _oldCPUTime = TimeSpan.Zero;
        private static DateTime _lastMonitorTime = DateTime.UtcNow;
        private static DateTime _lastRpsTime = DateTime.UtcNow;
        private static double _cpu = 0, _rps = 0;
        private static readonly double RefreshRate = TimeSpan.FromSeconds(1).TotalMilliseconds;
        private static long _requests = 0;
        private static readonly string TempPath = Path.GetTempPath();
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly ObjectPool<StringBuilder> _stringBuilders = new DefaultObjectPool<StringBuilder>(new DefaultPooledObjectPolicy<StringBuilder>());

        public ApiController()
        {
            Interlocked.Increment(ref _requests);
        }

        [HttpGet("collect")]
        public ActionResult GetCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return Ok();
        }

        [HttpGet("diagnostics")]
        public ActionResult GetDiagnostics()
        {
            var now = DateTime.UtcNow;
            _process.Refresh();

            var cpuElapsedTime = now.Subtract(_lastMonitorTime).TotalMilliseconds;

            if (cpuElapsedTime > RefreshRate)
            {
                var newCPUTime = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? TimeSpan.Zero : _process.TotalProcessorTime;
                var elapsedCPU = (newCPUTime - _oldCPUTime).TotalMilliseconds;
                _cpu = elapsedCPU * 100 / Environment.ProcessorCount / cpuElapsedTime;

                _lastMonitorTime = now;
                _oldCPUTime = newCPUTime;
            }

            var rpsElapsedTime = now.Subtract(_lastRpsTime).TotalMilliseconds;
            if (rpsElapsedTime > RefreshRate)
            {
                _rps = _requests * 1000 / rpsElapsedTime;
                Interlocked.Exchange(ref _requests, 0);
                _lastRpsTime = now;
            }

            var diagnostics = new
            {
                PID = _process.Id,

                // The allocated managed objects in the GC segments.
                Allocated = GC.GetTotalMemory(false),

                // The working set includes both shared and private data. The shared data includes the pages that contain all the 
                // instructions that the process executes, including instructions in the process modules and the system libraries.
                WorkingSet = _process.WorkingSet64,

                // The value returned by this property represents the current size of memory used by the process, in bytes, that 
                // cannot be shared with other processes.
                PrivateBytes = _process.PrivateMemorySize64,

                // The number of generation 0 collections
                Gen0 = GC.CollectionCount(0),

                // The number of generation 1 collections
                Gen1 = GC.CollectionCount(1),

                // The number of generation 2 collections
                Gen2 = GC.CollectionCount(2),

                CPU = _cpu,

                RPS = _rps
            };

            return new ObjectResult(diagnostics);
        }

        [HttpGet("staticstring")]
        public ActionResult<string> GetStaticString()
        {
            var bigString = new String('x', 10 * 1024);
            _staticStrings.Add(bigString);
            return bigString;
        }

        [HttpGet("bigstring")]
        public ActionResult<string> GetBigString()
        {
            return new String('x', 10 * 1024);
        }

        [HttpGet("loh/{size=85000}")]
        public int GetLOH(int size)
        {
            return new byte[size].Length;
        }

        [HttpGet("fileprovider")]
        public void GetFileProvider()
        {
            var fp = new PhysicalFileProvider(TempPath);
            fp.Watch("*.*");
        }

        [HttpGet("httpclient1")]
        public async Task<int> GetHttpClient1(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetAsync(url);
                return (int)result.StatusCode;
            }
        }

        [HttpGet("httpclient2")]
        public async Task<int> GetHttpClient2(string url)
        {
            var result = await _httpClient.GetAsync(url);
            return (int)result.StatusCode;
        }

        [HttpGet("array/{size}")]
        public byte[] GetArray(int size)
        {
            var array = new byte[size];

            var random = new Random();
            random.NextBytes(array);

            return array;
        }

        private static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

        private class PooledArray : IDisposable
        {
            public byte[] Array { get; private set; }

            public PooledArray(int size)
            {
                Array = _arrayPool.Rent(size);
            }

            public void Dispose()
            {
                _arrayPool.Return(Array);
            }
        }

        [HttpGet("pooledarray/{size}")]
        public byte[] GetPooledArray(int size)
        {
            var pooledArray = new PooledArray(size);

            var random = new Random();
            random.NextBytes(pooledArray.Array);

            HttpContext.Response.RegisterForDispose(pooledArray);

            return pooledArray.Array;
        }

    }
}
