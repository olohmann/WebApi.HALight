# WebAPI.HALight

A simple [HAL](http://stateless.co/hal_specification.html) formatter for ASP.NET Web API.

A more in-depth usage sample should follow soon.

**Note**: If you are looking for the **ASP.NET Core** version, move over to: [Lohmann.HALight](https://github.com/olohmann/Lohmann.HALight)

## Installation
Install via NuGet:
```
install-package WebAPI.HALight
```

## Usage

In your Startup.cs:
```
using using WebApi.HALight.Formatters;

// ...
var httpConfiguration = new HttpConfiguration();

// ... route config ...

// Add formatter for 'application/hal+json'. 
httpConfiguration.Formatters.Add(new SimpleHalFormatter());

```

In your resource model:
```
using WebApi.HALight;

public class UserResource : Resource
{
    public int Id { get; set; }

    public string Name { get; set; }
}
```

In your controller:
```
public class UsersController : ApiController
{
    private readonly UserRepository _repository;

    // ...
    
    // Route: "/Users/{id}"
    public IHttpActionResult Get(int id)
    {
        var user = _repository.Get(id);

        if (user == null)
        {
            return NotFound();
        }

        var userResource = new UserResource
        {
            Id = user.Id,
            Name = user.Name,            
        };

        userResource.Relations.Add(
            Link.CreateSelfLink(
                Url.Link(
                    "DefaultApi", 
                    new { controller = "Users", id = userDetailResource.Id })
            )
        );

        return Ok(userDetailResource);
    }
}
```

## Acknowledgements
There a couple of other HAL libraries for ASP .NET Web API around that inspired my take on this subject. A big thank you to:
* [WebApi.Hal](https://github.com/JakeGinnivan/WebApi.Hal)
* [PointW.WebApi.ResourceModel](https://github.com/biscuit314/PointW.WebApi.ResourceModel)
