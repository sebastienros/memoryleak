using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

            var newCPUTime = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? TimeSpan.Zero : _process.TotalProcessorTime;
            var elapsedTime = now.Subtract(_lastMonitorTime).TotalMilliseconds;
            var elapsedCPU = (newCPUTime - _oldCPUTime).TotalMilliseconds;
            var cpu = elapsedCPU * 100 / Environment.ProcessorCount / elapsedTime;

            _lastMonitorTime = now;
            _oldCPUTime = newCPUTime;

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

                CPU = cpu
            };

            return new ObjectResult(diagnostics);
        }

        [HttpGet("array")]
        public ActionResult<IEnumerable<string>> GetArray()
        {
            return new string[] { "value1", "value2" };
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

        private static string FormatMB(long bytes)
        {
            return $"{((bytes / 1024d) / 1024d).ToString("N2")} MB";
        }
    }
}
