# Proof-of-Concept for OData for ASP.NET Core

1. Create a new ASP.NET Core Web API
	- Using Visual Studio, create a new ASP.NET Core web project
	- Select the Web API template
	- Add global.json file to lock down .NET Core SDK version

	```
	dotnet new globaljson
	```

	```json
    {
      "sdk": {
        "version": "2.1.2"
      }
    }
	```	

2. Scaffold Product table from NorthwindSlim database
	- Create NorthwindSlim database on LocalDb SQL Server instance
	- Download and run script from http://bit.ly/northwindslim
	- Add package for **Microsoft.EntityFrameworkCore.SqlServer**.
	- Edit the csproj file and add the following element to the last `ItemGroup`:

	```xml
	<DotNetCliToolReference Include="Microsoft.EntityFrameworkCore.Tools.DotNet" Version="2.0.0" />
	```

	- Execute `dotnet restore` from the command line.
	- Reverse engineer the Product table from NorthwindSlim.

	```
	dotnet ef dbcontext scaffold "Data Source=(localdb)\MSSQLLocalDB; Initial Catalog=NorthwindSlim; Integrated Security=True" Microsoft.EntityFrameworkCore.SqlServer -o Models -c NorthwindSlimContext -f -t Product
	```

	- Two files will be added to a Models folder: NorthwindSlimContext.cs, Product.cs

3. 	Update the NorthwindSlimContext class
	- Remove the `OnConfiguring` method.
	- Add a **NorthwindSlimContextEx.cs** file with a ctor.

	```csharp
	using Microsoft.EntityFrameworkCore;

	namespace ODataAspNetCorePoc.Models
	{
		public partial class NorthwindSlimContext
		{
			public NorthwindSlimContext(DbContextOptions<NorthwindSlimContext> options) : base(options) { }
		}
	}
	```

4. Configure the web api to use the EF context and connection string.
	- Add a connection string to **appsettings.json**.

	```json
	"ConnectionStrings": {
	"NorthwindContext": "Data Source=(localdb)\\MSSQLLocalDB;initial catalog=NorthwindSlim;Integrated Security=True; MultipleActiveResultSets=True"
	}
	```

	- Update the ConfigureServices method in **Startup.cs**.

	```csharp
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddMvc();
		var connectionString = Configuration.GetConnectionString("NorthwindContext");
		services.AddDbContext<NorthwindSlimContext>(options => options.UseSqlServer(connectionString));
	}
	```

5. Add support for OData to the Web API project.
	- Add a NuGet package source for the OData ASP.NET Core nightly builds.

	```
	https://www.myget.org/F/webapinetcore/
	```

	- You can use the Package Manager Console to install the OData package:

	```
	Install-Package Microsoft.AspNetCore.OData -pre
	```

	- Add the following line of code in `ConfigureServices` _before_ `services.AddMvc()`:

	```csharp
	services.AddOData();
	```

	- Add a private `GetEdmModel` method to **Startup.cs**.

	```csharp
	private IEdmModel GetEdmModel(IServiceProvider serviceProvider)
	{
		var builder = new ODataConventionModelBuilder(serviceProvider);
		builder.EntitySet<Product>("Product");
		return builder.GetEdmModel();
	}
	```

	> Note: Resolve `IEdmModel` by adding: `using Microsoft.OData.Edm`.

	- Update the `Configure` method as follows:

	```csharp
	public void Configure(IApplicationBuilder app, IHostingEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		IEdmModel model = GetEdmModel(app.ApplicationServices);
		app.UseMvc(routes =>
			{
				routes.MapODataServiceRoute("odata_route", "odata", model);
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
	}
	```

	- Change `launchUrl` in **launchSettings.json** to `"odata"`.
	- Press F5 or Ctrl+F5 to launch the app.
	- You should see the following in the browser.

	```json
	{
	  @odata.context: "http://localhost:54151/odata/$metadata",
	  value: [
	  	{
		  name: "Product",
    	  kind: "EntitySet",
    	  url: "Product"
        }
  	  ]
	}
	```

6. Add a **ProductController.cs** file to the Controllers folder.
	- Add the following attribute: `[Produces("application/json")]`
	- Inherit from `ODataController`.
	- Inject a `NorthwindSlimContext` into a constructor.
	- Add a `Get` method returning `IQueryable` with a `[EnableQuery]` attribute.

	```csharp
	[Produces("application/json")]
	public class ProductController : ODataController
	{
		private readonly NorthwindSlimContext _context;

		public ProductController(NorthwindSlimContext context)
		{
			_context = context;
		}

		[HttpGet]
		[EnableQuery]
		public IQueryable<Product> Get()
		{
			return _context.Product;
		}
	}
	```

	- Submit a GET request to http://localhost:54151/odata/Product
	- You should get back a `Product` array.

7. Add a POST action to the Product controller.

	```csharp
	[HttpPost]
	public async Task<IActionResult> Post([FromBody]Product item)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		_context.Entry(item).State = EntityState.Added;

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			if (ItemExists(item.ProductId))
			{
				return StatusCode((int)HttpStatusCode.Conflict);
			}
			throw;
		}

		return Created(item);
	}

	private bool ItemExists(int id)
	{
		return _context.Product.Any(e => e.ProductId == id);
	}
	```

	- Submit a POST request via Postman or Fiddler with a Content-Type header of application/json.

	```json
	{
		"ProductName": "Chocolato",
		"CategoryId": 1,
		"UnitPrice": 24,
		"Discontinued": false,
	}
	```

	- You should receive a 201 response with the inserted product.

> Note: Put, Patch and Delete actions presently return a 404 Not Found response.
