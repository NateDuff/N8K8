using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.ResponseCompression;
using N8.Web.Hubs.Hubs;
using N8.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoint"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Configuration.AddAzureKeyVaultSecrets("secrets");

//builder.AddRabbitMQClient("messaging");
builder.AddAzureServiceBusClient("messaging");
builder.AddAzureTableClient("tables");

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}
//builder.Services.AddSingleton<MessageQueueService>();
builder.Services.AddSingleton<ServiceBusListenerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    
    app.UseResponseCompression();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
             "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseAntiforgery();
app.UseForwardedHeaders();

app.MapHub<ChatHub>("/chathub");

app.MapDefaultEndpoints();

app.MapRazorComponents<N8.Web.Components.App>()
    .AddInteractiveServerRenderMode();

var serviceBusListenerService = app.Services.GetRequiredService<ServiceBusListenerService>();

serviceBusListenerService.StartAsync("ha").GetAwaiter().GetResult();

app.Run();
