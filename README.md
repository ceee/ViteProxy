![ViteProxy](https://raw.githubusercontent.com/ceee/ViteProxy/main/viteproxy.png "ViteProxy")

## Introduction

**ViteProxy** is a proxy for vite projects within ASP.NET Core.

This project is meant to be used within server-side applications and not within SPAs (single-page applications). 
<br>
Without ViteProxy you would have to start your server application (e.g. dotnet run) and additionally the vite dev server (e.g. npm run dev). The proxy is registered as a hosted service and started in tandem with the server application.

## Usage

### 1. Register service

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddViteProxy();
```

### 2. Deliver additional static files

```csharp
var app = builder.Build();

app.UseStaticFiles();
app.UseViteStaticFiles();
```

### 3. Configure vite.config

```js
export default {
  ...,
  server: {
    port: process.env.PORT || 2341,
    cors: true
  },
  ...
};
```

## License

[MIT License](https://github.com/ceee/PocketSharp/blob/master/LICENSE-MIT)
