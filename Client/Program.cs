using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Cấu hình retry với thư viện mới
// AddStandardResilienceHandler cấu hình các chiến lược sau:
// 1: Rate limiter 
// The rate limiter pipeline limits the maximum number of concurrent requests being sent to the dependency.
// 2: Total request timeout
// The total request timeout pipeline applies an overall timeout to the execution, ensuring that the request, including retry attempts, doesn’t exceed the configured limit.
// 3: Retry
// The retry pipeline retries the request in case the dependency is slow or returns a transient error.
// 4: Circuit breaker
// The circuit breaker blocks the execution if too many direct failures or timeouts are detected.
// 5: Attempt timeout:
// The attempt timeout pipeline limits each request attempt duration and throws if it’s exceeded.

// Ví dụ 1:
// => xử lý các lỗi: 5XX, 429, 408,  HttpRequestException and TimeoutRejectedException.
builder.Services.AddHttpClient("my-client1")
                .AddStandardResilienceHandler();

// Ví dụ 2:
builder.Services
    .AddHttpClient("my-client2")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                                    .Handle<TimeoutRejectedException>()
                                    .Handle<HttpRequestException>()
                                    .HandleResult(response => response.StatusCode == HttpStatusCode.InternalServerError);
       // options.Retry.OnRetry = 

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    });

// Ví dụ 3:
builder.Services
    .AddHttpClient("my-client3")
    .AddStandardResilienceHandler()
    .Configure(builder.Configuration.GetSection("my-section"))
    .Configure((options, serviceProvider) =>
    {
        // configure options here
    });

// Ví dụ 4
builder.Services
    .AddHttpClient("my-client4", client => client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com"))
    .AddResilienceHandler("custom-pipeline", (builder, context) =>
    {
        builder
            .AddRetry(new HttpRetryStrategyOptions())
            .AddTimeout(new HttpTimeoutStrategyOptions());
    });

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
