# Memory Management and Patterns in ASP.NET Core

Memory management is complex, even in a managed framework like .NET. Analyzing and understanding memory issues can be challenging.

Some time ago an issue was created on the ASP.NET GitHub repository about The Garbage Collector (GC) "not collecting the garbage", which would make it quite useless. The symptoms, as described by the original creator, were that the memory would keep growing request after request, letting them think that the issue was in the GC. Here is the link to the GitHub issue: https://github.com/aspnet/Home/issues/1976

We tried to get more information about this issue, to understand if the problem was in the GC or in the application itself, but what we got instead was a wave of other contributors posting reports of such behavior: the memory keeps growing. The thread grew to the extent that we decided to split it into multiple issues and follow-up on them independantly. In the end most of the issues can be explained by some missunderstanding about how memory consumption works in .NET, but also issues in how it was measured.

To help .NET developers better understand their applications, we need understand how memory management works in ASP.NET Core, how to detect memory related issues, and how to prevent common mistakes.

## How Garbage Collection works in ASP.NET Core

The GC allocates a contiguous range of memory, the heap, and objects placed in it are categorized in with a generation number (0, 1, and 2). The lower the generation number, the more frequent GC will try to release the memory taken by the objects categorized with it.

Objects are moved from one generation to another based on their lifetime. As objects live longer they will be moved in a higher generation, and assessed for collection less often. Short term lived objects like the ones that are referenced during the life of a web request will always remain in generation 0. Application level singletons however will most probably move to generation 1 and eventually 2.

The first thing that affects how ASP.NET applications behave in terms of memory consumption is that the GC will allocate some memory even if there are no objects to put in, and this amount is greater as the available memory on the system is big. This is done for performance reasons as allocating memory is expensive. It also means that seeing an ASP.NET Core process take 400MB of memory at startup is expected and a normal behavior.

> Important: An ASP.NET Core process will preemptively allocate a significant amount of memory at startup.

### Calling the GC explicitly 

Objects in generation 2 are only collected when the amount of reserved memory is no more sufficient to allow new objects to be allocated on the heap, or when the GC is invoked programmatically by the application. To manually invoke the GC execute `GC.Collect()`. This will trigger a generation 2 collection, and indirectly all lower generations. This is usually only used when investigating memory leaks, to be sure the GC has removed all dangling objects from memory before we can measure it.

>Note: You should never call `GC.Collect()` directly unless for investigating purposes.

## Analyzing the memory usage of an application

Dedicated tools can help analyzing memory usage:
- counting object references
- measuring how much impact the GC has on CPU
- measuring space used for each generation

However for the sake of simplicity this article won't use any of these but instead render some in-app live charts.

For in-depth anlyzis please read these article which demonstrate how to use Visual Studio .NET:

[Analyze memory usage without the Visual Studio debugger](https://docs.microsoft.com/en-us/visualstudio/profiling/memory-usage-without-debugging2)

[Profile memory usage in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/profiling/memory-usage)


### Detecting memory issues

Most of the time the __Task Manager__ is used to get an idea of how much memory an ASP.NET application is using. This value actually represents the amount of memory that was allocated by the GC, not the amount that is actually used by the application's living objects.

Seeing this value increasing indefinitely is a clue that there is a memory leak somewhere in the code but it doesn't explain what it is. The next sections will introduce you to specific memory usage patterns and explain them.

### Running the application

The full source code is available on GitHub at https://github.com/sebastienros/memoryleak

Once it has started the application displays some memory and GC statictics and the page refreshes by itself every second. Specific API endpoints execute specific memory allocation patterns. 

To test this application, simply start it. You can see that the allocated memory keeps increasing, because displaying these statistics is allocating custom objects for instance. The GC eventually runs and collects them.

This pages shows a graphs including allocated memory and GC collections. The legend also displays the CPU usage and throughput in requests per second.

The chart displays two values for the memory usage:
- Allocated: the amount of memory allocated on the managed heap since last GC collection
- Working Set: the total physical memory (RAM) used by the process (as displayed in the Task Manager)

#### Transient objects

The following API creates a 10KB `String` instance and returns it to the client. On each request a new object is allocated in memory and written on the response. 

> Note: Strings are stored as UTF-16 characters in .NET so each char takes two bytes in memory.

```csharp
[HttpGet("bigstring")]
public ActionResult<string> GetBigString()
{
    return new String('x', 10 * 1024);
}
```

The following graph is generated with a relatively small load of 5K RPS in order to see how the memory allocations are impacted by the GC.

![](images/bigstring.png)

In this example, the GC collect the generation 0 instances about every two seconds once the allocations reach a threshold of a little above 300 MB. The working set is stable at around 500 MB, and the CPU usage is low.

What this graph shows is how on a relatively low requests throughput the memory consumption is very stable to an amount that has been chosen by the GC. 

The following chart is taken once the load is increased to the max througput that can be handled by the machine.

![](images/bigstring2.PNG)

There are some notable points:
- The collections happen much more frequently, as in many times per second
- There are now generation 1 collections, which is due to the fact that we allocated much more of them in the same time interval
- The working set is still stable

What we see is that as long as the CPU is not over-utilized, the garbage collection can deal with a high number of allocations.

#### Workstation GC vs. Server GC

The .NET Garbage Collector can work in two different modes, namely the __Workstation GC__ and the __Server GC__. As their names suggest, they are optimized for different workloads. ASP.NET applications default to the Server GC mode, while desktop applications use the Workstation GC mode.

To visualize the actual impact of these modes, we can force the Workstation GC on our web application by using the `ServerGarbageCollection` parameter in the project file (`.csproj`). This will require the application to be rebuilt.

```xml
    <ServerGarbageCollection>true</ServerGarbageCollection>
```

It can also be done by setting the `System.GC.Server` property in the `runtimeconfig.json` file of the published application.

Here is the memory profile under a 5K RPS.

![](images/workstation.png)

The differences are drastic:
- The working set came from 500MB to 70MB
- The GC does generation 0 collections multiple times per second instead of every two seconds
- The GC threshold went from 300MB to 10MB

On a typical web server environment the CPU resource is more critical than memory, hence using the Server GC is better suited. However, some server scenarios might be more adapted for a Workstation GC, for instance on a high density hosting several web application where memory becomes a scarce resource. 

> Note: On machines with a single core, the GC mode will always be Workstation.

#### Eternal references

Even though the garbage collector does a good job at preventing memory to grow, there are some scenarios where it can't release the memory, and then will induce memory leaks.

The following API creates a 10KB `String` instance and returns it to the client. The difference with the first example is that this instance is referenced by a static member, which means it will never available for collection.

```csharp
private static ConcurrentBag<string> _staticStrings = new ConcurrentBag<string>();

[HttpGet("staticstring")]
public ActionResult<string> GetStaticString()
{
    var bigString = new String('x', 10 * 1024);
    _staticStrings.Add(bigString);
    return bigString;
}
```

This is a typical user code memory leak as the memory will keep increasing without any way for the GC to free it until there is no more available memory and the application crashes.

![](images/eternal.png)

What we can see on this chart once we start issuing requests on this new endpoint is that the working set is no more stable and increases constantly. During that increase the GC tries to free memory as the memory pressure grows, by calling a generation 2 collection. This succeeds and frees some of it, but this can't stop the working set from increasing.

Some scenarios require to keep object references indefinitely, in which case a way to mitigate this issue would be to use the `WeakReference` class in order to keep a reference on an object that can still be collected under memory pressure. This is what the default implementation of `IMemoryCache` does in ASP.NET Core. 

#### Native memory

Memory leaks don't have to be caused by eternal references to managed objects. Some .NET objects rely on native memory to function. This memory cannot be collected by the GC and the .NET objects need free it using native code.

Fortunately .NET provides the `IDisposable` interface to let developers release this native memory proactively. And even if `Dispose()` is not called in time, classes usually do it automatically when the finalizer is called by the garbage collector... unless the class is not correctly implemented.

Let's take a look at this code for instance:

```csharp
[HttpGet("fileprovider")]
public void GetFileProvider()
{
    var fp = new PhysicalFileProvider(TempPath);
    fp.Watch("*.*");
}
```

`PhysicaFileProvider` is a managed class, so any instance will be collected at the end of the request.

Here is the resulting memory profile while invoking this API continuously.

![](images/fileprovider.png)

This chart shows an obvious issue with the implementation of this class, as it keeps increasing memory usage. This is a known issue that is being tracked here https://github.com/aspnet/Home/issues/3110

The same issue could be easily happening in user code, by not releasing the class correctly or forgetting to invoke the `Dispose()` method of the dependent objects which should be disposed. 

#### Large Objects Heap

As memory gets allocated and freed continously, fragmentation in the memory can happen. This is an issue as objects have to be allocated in a contiguous block of memory. To mitigate this issue, whenever the garbage collector frees some memory, it will try to defragment it. This process is called __compaction__.

The problem that compaction faces is that the bigger the object, the slower it is to move it. There is a size after which an object will take so much time to be moved that it is not as efficient anymore to move it. For this reason the GC creates a special memory zone for these _large_ objects, called the __Large Object Heap__ (LOH). Object that are greater than 85,000 bytes (not 85 KB) are placed there, not compacted, and eventually released during generation 2 collections. But another effect is that whenever the LOH is full, it will trigger an automatic generation 2 collection, which is inherently slower as it triggers a collection on all other generations too.

Here is an API that illustrates this behavior:

```csharp
[HttpGet("loh/{size=85000}")]
public int GetLOH1(int size)
{
    return new byte[size].Length;
}
```

The following chart shows the memory profile of calling this endpoint with a `84,975` bytes array, under maximum load:

![](images/loh1.png)

And then the chart when calling the same endpoint but using _just_ one more byte, i.e. `84,976` bytes (the `byte[]` structure has some little overhead on top of the actual bytes serialization).

![](images/loh2.png)

The working set is about the same on both scenarios, at a steady 450 MB. But what we notice is that instead of having mostly generation 0 collections, we instead get generation 2 collections, which require more CPU time and directly impacts the throughput which decreases from 35K to 18K RPS, __almost halving it__.

This shows that very large objects should be avoided. As an example the __Response Caching__ middleware in ASP.NET Core split the cache entries in block of a size lower than 85,000 bytes to handle this scenario.

Here are some links to the specific implementation handling this nehavior 
- https://github.com/aspnet/ResponseCaching/blob/c1cb7576a0b86e32aec990c22df29c780af29ca5/src/Microsoft.AspNetCore.ResponseCaching/Streams/StreamUtilities.cs#L16
- https://github.com/aspnet/ResponseCaching/blob/c1cb7576a0b86e32aec990c22df29c780af29ca5/src/Microsoft.AspNetCore.ResponseCaching/Internal/MemoryResponseCache.cs#L55

#### HttpClient

Not specifically a memory leak issue, more of a resource leak one, but this has been seen enough times in user code that it deserved to be mentioned here.

Seasoned .NET developer are used to disposing objects that implement `IDisposable`. After all not doing so might result is leaked memory (see previous examples), or other native resources like database connections and file handlers.

But `HttpClient`, even though it implements `IDisposable` should not be used then disposed on every invocation, but reused instead.

Here is an API endpoint that creates and disposes a new instance on every request.

```csharp
[HttpGet("httpclient1")]
public async Task<int> GetHttpClient1(string url)
{
    using (var httpClient = new HttpClient())
    {
        var result = await httpClient.GetAsync(url);
        return (int)result.StatusCode;
    }
}
```

While putting some load on this endpoint, some error messages are logged:

```
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HLG70PBE1CR1", Request id "0HLG70PBE1CR1:00000031": An unhandled exception was thrown by the application.
System.Net.Http.HttpRequestException: Only one usage of each socket address (protocol/network address/port) is normally permitted ---> System.Net.Sockets.SocketException: Only one usage of each socket address (protocol/network address/port) is normally permitted
   at System.Net.Http.ConnectHelper.ConnectAsync(String host, Int32 port, CancellationToken cancellationToken)
```

What happens is that even though the `HttpClient` instances are disposed, the actual network connection will take some time to be released by the operating system. By continuously creating new connections we finally hit _ports exhaustion_ as each client connection requires its own client port.

The solution is to actually reuse the same `HttpClient` instance like this:

```csharp
private static readonly HttpClient _httpClient = new HttpClient();

[HttpGet("httpclient2")]
public async Task<int> GetHttpClient2(string url)
{
    var result = await _httpClient.GetAsync(url);
    return (int)result.StatusCode;
}
```

This instance will eventually get released when the application closes.

This shows that it's not because a resource is disposable that it needs to be disposed right away.

> Note: there are better ways to handle the lifetime of an `HttpClient` instance since ASP.NET Core 2.1 https://blogs.msdn.microsoft.com/webdev/2018/02/28/asp-net-core-2-1-preview1-introducing-httpclient-factory/

#### Object pooling

In the previous example we saw how the `HttpClient` instance can be made a static and reused by all requests to prevent resource exhaustion.

A similar pattern is to use object pooling. The idea is that if an object is expensive to create, then we should reuse its instances to prevent resource allocations. A pool is a collection of pre-initialized objects that can be reserved and released across threads. Pools can define allocation rules like hard limits, predefined sizes, or growth rate.

The Nuget package `Microsoft.Extensions.ObjectPool` contains classes that help to manage such pools.

To show how beneficial it can be, let use an API endpoint that instanciates a `byte` buffer that is filled with random numbers on each request:

```csharp
        [HttpGet("array/{size}")]
        public byte[] GetArray(int size)
        {
            var random = new Random();
            var array = new byte[size];
            random.NextBytes(array);

            return array;
        }
```

With some load we can see generation 0 collections happening around every second.

![](images/array.png)

To optimize this code we can pool the `byte` buffer by using the `ArrayPool<>` class. A static instance is reused across requests. 

The special part of this scenario is that we are returning a pooled object from the API, which means we lose control of it as soon as we return from the method, and we can't release it. To solve that we need to encapsulate the pooled array in a disposable object and then register this special object with `HttpContext.Response.

```csharp
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
```

The same load as the non-pooled version result in the following profile.

![](images/pooledarray.png)

You can see that the main difference is in the rate of allocations, and as a consequence much fewer generation 0 collections.

## Conclusion

Understanding how Garbage Collection works together with ASP.NET Core can be helpful to investigate memory pressure issues, and ultimately the performance of an application. 

Applying the practices explained in this article should prevent applications from showing signs of memory leaks.

### Reference Articles

To go further in the understanding of how memory managment works in .NET, here are some recommended articles.

[Garbage Collection](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/)

[Understanding different GC modes with Concurrency Visualizer](https://blogs.msdn.microsoft.com/seteplia/2017/01/05/understanding-different-gc-modes-with-concurrency-visualizer/)