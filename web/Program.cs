using Microsoft.AspNetCore.ResponseCompression;
using N8.Shared.Messaging;
using N8.Web.Components;
using N8.Web.Hubs.Hubs;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

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
});

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    return new ConnectionFactory()
    {
        HostName = builder.Configuration["RABBITMQ_HOST"],
        UserName = "guest",
        Password = "guest"
    };
});

builder.Services.AddSingleton<MessageQueueService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseDeveloperExceptionPage();
}


app.UseResponseCompression();
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

app.MapHub<ChatHub>("/chathub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
