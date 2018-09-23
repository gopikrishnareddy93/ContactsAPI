# ContactsAPI

We’ll focus on making our code asynchronous, and hopefully making our API work more efficiently than synchronous execution.

First we will write synchronous API's code – i.e. we haven’t used async / await yet. For synchronous API code, when a request is made to the API, a thread from the thread pool will handle the request. If the code makes an I/O call (like a database call) synchronously, the thread will block until the I/O call has finished. The blocked thread can’t be used for any other work, it simply does nothing and waits for the I/O task to finish. If other requests are made to our API whilst the other thread is blocked, different threads in the thread pool will be used for the other requests.

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ContactsAPI/master/diagram/Sync-Diagram.png)

There is some overhead in using a thread – a thread consumes memory and it takes time to spin a new thread up. So, really we want our API to use as few threads as possible.

If the API was to work in an asynchronous manner, when a request is made to our API, a thread from the thread pool handles the request (as in the synchronous case). If the code makes an asynchronous I/O call, the thread will be returned to the thread pool at the start of the I/O call and then be used for other requests.

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ContactsAPI/master/diagram/ASync-Diagram.png)

So, making operations asynchronous will allow our API to work more efficiently with the ASP.NET Core thread pool. So, in theory, this should allow our API to scale better – let’s see if we can prove that.

## Sync v Async Test
Let’s test the above theory with the following controller action methods. You can see that we prefix the method with “async” to make it asynchronous and prefix asynchronous I/O calls with “await”. In our example, we are using WAITFOR DELAY to simulate a database call that takes 2 seconds.


``` csharp

[Route("api/[controller]")]
[ApiController]
public class SyncVAsyncController : ControllerBase
{
    private readonly string _connectionString;
    public SyncVAsyncController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet("sync")]
    public IActionResult SyncGet()
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            using (var cmd = new SqlCommand("WAITFOR DELAY '00:00:02';", conn))
            {
                conn.Open();
                cmd.ExecuteScalar();
                conn.Close();
            }
        }

        return Ok();
    }

    [HttpGet("async")]
    public async Task<IActionResult> AsyncGet()
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            using (var cmd = new SqlCommand("WAITFOR DELAY '00:00:02';", conn))
            {
                await conn.OpenAsync();
                await cmd.ExecuteScalarAsync();
                conn.Close();
            }
        }

        return Ok();
    }

}

```

Let’s throttle the thread pool:

``` csharp 

public void ConfigureServices(IServiceCollection services)
{
    ...
    int processorCounter = Environment.ProcessorCount; // 8 on my PC
    ThreadPool.SetMaxThreads(processorCounter, processorCounter);
    ...
}

```

Let’s load test the sync end point first:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ContactsAPI/master/diagram/async-sync-loadtest.png)

Now let’s load test the async end point:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ContactsAPI/master/diagram/async-async-loadtest.png)

Load testing shows that async is more efficient than the sync end point because it is able to manage threads more efficiently.

## Conclusion
When the API is stressed, async action methods will give the API some much needed breathing room whereas sync action methods will deteriorate quicker.

Async code doesn’t come for free – there is additional overhead in context switching, data being shuffled on and off the heap, etc which is why async code can be a bit slower than the equivalent sync code if there is plenty of available threads in thread pool. This difference is usually very minor though.

It’s a good idea to write async action methods that are I/O bound even if the API is only currently dealing with a low amount of usage. It only takes typing an extra keyword per I/O call and the usage can grow.

This repository has one more controller(ContactsAPI) that shows how to implement CRUD operations using async/await pattern.