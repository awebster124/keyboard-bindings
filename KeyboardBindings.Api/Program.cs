var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "KeyboardBindings API");

app.Run();
