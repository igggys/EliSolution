using Helpers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    builder.Services.AddLocalization();
    builder.Services.AddControllersWithViews()
        .AddViewLocalization();

    builder.Services.Configure<DatabaseConnection>(
        builder.Configuration.GetSection(DatabaseConnection.SectionName));

    builder.Services.Configure<Services.MailService.MailSettings>(
        builder.Configuration.GetSection(Services.MailService.MailSettings.SectionName));

    builder.Services.AddSingleton<DataManager.PublicSite.DataManager>(serviceProvider =>
    {
        var databaseConnection = serviceProvider.GetRequiredService<IOptions<DatabaseConnection>>().Value;
        return new DataManager.PublicSite.DataManager(databaseConnection.ConnectionString);
    });

    builder.Services.AddSingleton<Services.MailService.IMailService>(serviceProvider =>
    {
        var mailSettings = serviceProvider.GetRequiredService<IOptions<Services.MailService.MailSettings>>().Value;
        return new Services.MailService.MailService(mailSettings);
    });

    var app = builder.Build();

    app.UseStatusCodePages(async statusCodeContext =>
    {
        var httpContext = statusCodeContext.HttpContext;
        if (httpContext.Response.StatusCode != StatusCodes.Status404NotFound
            || httpContext.Request.Path.StartsWithSegments("/images/blog"))
        {
            return;
        }

        var originalPath = httpContext.Request.Path;
        var originalQueryString = httpContext.Request.QueryString;

        httpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature
        {
            OriginalPathBase = httpContext.Request.PathBase.Value ?? string.Empty,
            OriginalPath = originalPath.Value ?? string.Empty,
            OriginalQueryString = originalQueryString.Value ?? string.Empty
        });

        httpContext.SetEndpoint(endpoint: null);
        var routeValuesFeature = httpContext.Features.Get<IRouteValuesFeature>();
        if (routeValuesFeature is not null)
        {
            routeValuesFeature.RouteValues = null!;
        }

        httpContext.Request.Path = "/Error/404";
        httpContext.Request.QueryString = QueryString.Empty;

        try
        {
            await statusCodeContext.Next(httpContext);
        }
        finally
        {
            httpContext.Request.Path = originalPath;
            httpContext.Request.QueryString = originalQueryString;
            httpContext.Features.Set<IStatusCodeReExecuteFeature>(null!);
        }
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "blogImage",
        pattern: "images/blog/{fileName}",
        defaults: new { controller = "BlogImages", action = "Get" });

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "errorStatus",
        pattern: "Error/{statusCode:int}",
        defaults: new { controller = "Error", action = "Status" });

    app.MapControllerRoute(
        name: "homeContact",
        pattern: "Home/Contact/{lang?}",
        defaults: new { controller = "Home", action = "Contact" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "home",
        pattern: "Home/{lang?}",
        defaults: new { controller = "Home", action = "Index" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "practiceArea",
        pattern: "PracticeArea/{section}/{lang?}",
        defaults: new { controller = "PracticeArea", action = "Index" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "about",
        pattern: "About/{lang?}",
        defaults: new { controller = "About", action = "Index" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "privacyPolicy",
        pattern: "PrivacyPolicy/{lang?}",
        defaults: new { controller = "PrivacyPolicy", action = "Index" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "termsOfUse",
        pattern: "TermsOfUse/{lang?}",
        defaults: new { controller = "TermsOfUse", action = "Index" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "blogArticle",
        pattern: "Blog/{slug}/{lang?}",
        defaults: new { controller = "Blog", action = "Article" },
        constraints: new { slug = BlogRoutes.SlugConstraint })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "blog",
        pattern: "Blog/{lang?}",
        defaults: new { controller = "Blog", action = "Index" })
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PublicSite terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
