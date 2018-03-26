# Example

This document aims to help people without much experience in Service Fabric and Traefik in setting up a working solution.

It will be divided in following challenges:

1. Adding Traefik to a Service Fabric Cluster
2. Setting up routing for 3 web applications (2 apis, 1 web) based on pathing.  /api/product => goes to product service, /api/products goes to products
3. [Optional] Using Traefik to handle https requests
4. [Optional] Canary releases with Traefik

## Prerequisites

In order to use this guide ensure the following:

- Visual Studio 2017 with Service Fabric extensions is installed

- You have a Service Fabric local cluster running

## Getting Started

In this step we will deploy a ficticious retail Store application with Service Fabric. The Store application consists of the following "microservices":

- Web site: where people can see our products and store locations
- Location REST API: it has locations of physical stores
- Products REST API: is has information about products on sale

To get started clone this repository and open the solution under Examples/src/StoreApp. Executing (F5 / Start) the solution will deploy it to your local Service Fabric Cluster. The following 3 services should by deployed:

![Store Web on local SF](images/running-storeweb-locally-v100.png)

- Web app on http://localhost:8729/
    - List of products is displayed in start page. The list is built in server side
    ![Product list](images/web-store-running.png)
    - The Store location page (http://localhost:8729/StoreLocation) display a list of location retrieved in client side from the location API
- Products API on http://localhost:8525/api/product
- Location API on http://localhost:8560/api/location

The first challenge is to deploy Traefik Service Fabric application in our cluster.

## Challenge 1: Deploying Traefik on a Service Fabric cluster

Traefik extensions for Service Fabric was released in Traefik version 1.5. Traefik is a standalone application written in Go. Service Fabric can deploy guest applications, which are executable files. This is how Traefik is deployed to a Service Fabric cluster.

Getting Traefik running in your local cluster

1. Clone the repository https://github.com/jjcollinge/traefik-on-service-fabric.

2. Open and Build the solution

3. Right click on the project Traefik and select "Publish...". Select as target the local cluster

![Deploy to local cluster](images/public-traefik-locally.png)

4. Once deployment is done you should be able to open http://locahost:8080 and see an empty dashboard.
If you have Traefik deployment errors make sure you don't  have a different application listening on port 80 (default IIS installation does that) or 8080.

![Empty Traefik Dashboard](images/empty-traefik-dashboard.png)

## Challenge 2: Using Traeffik to route URLs in our cluster

The Store App services deployed in our local cluster have different port bindings (i.e. location api in port 8560). We want to make them more accessible outside the cluster, following these principles:

- All services should be available outside the cluster in port 80 (default http port).
- APIs should be exposed with URL pattern http://{domain-name}/api/{resource}
- Web Site is exposed to all other URLs on http://{domain-name}

Note: In the local cluster the domain name will be localhost. When deploying to Azure (or another cloud provider) you can apply your own CNAME rule to customize it.

Traefik routing rules in Service Fabric are resolved from Service manifests extensions configuration (with prefix "traefik."). In order to customize the routing we need to modify the ServiceManifest.xml files of our previously deployed Store app.

1. Set Product API route to be /api/product by editing the ServiceManifest.xml file. To do so add an extension to the service definition to tell Traefik that all requests to /api/product should be binded to this service
    ```xml
    <StatelessServiceType ServiceTypeName="StoreProductAPIType">
      <Extensions>
        <Extension Name="Traefik">
          <Labels xmlns="http://schemas.microsoft.com/2015/03/fabact-no-schema">
            <Label Key="traefik.frontend.rule.store-productapi">PathPrefix: /api/product</Label>
            <Label Key="traefik.expose">true</Label>
            <Label Key="traefik.frontend.passHostHeader">true</Label>
          </Labels>
        </Extension>
      </Extensions>
      </StatelessServiceType>
    ```

2. Since the API will be available on {domain}/api/product we don't need CORS enabled anymore (XMLHttpRequest will be in same domain). Remove CORS support by editing the Startup.cs file.
```c#
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        //services.AddCors(options =>
        //{
        //    options.AddPolicy("AllowAnyOrigin", builder => builder.AllowAnyOrigin());
        //});
        services.AddMvc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        //app.UseCors("AllowAnyOrigin");
        app.UseMvc();
    }
}
```

3. Set Location API route to be /api/location by editing the ServiceManifest.xml file. To do so add an extension to the service definition to tell Traefik that all requests to /api/location should be binded to this service
    ```xml
    <StatelessServiceType ServiceTypeName="StoreLocationAPIType">
      <Extensions>
        <Extension Name="Traefik">
          <Labels xmlns="http://schemas.microsoft.com/2015/03/fabact-no-schema">
            <Label Key="traefik.frontend.rule.store-locationapi">PathPrefix: /api/location</Label>
            <Label Key="traefik.expose">true</Label>
            <Label Key="traefik.frontend.passHostHeader">true</Label>
          </Labels>
        </Extension>
      </Extensions>
    </StatelessServiceType>
    ```
4. Since the API will be available on {domain}/api/location we don't need CORS enabled (XMLHttpRequest will be in same domain). Do same as product api, removing CORS support in the Startup.cs file.

5. Set Web App route to match all remaining requests by editing the ServiceManifest.xml file. To do so add an extension to the service definition to tell Traefik that all requests prefixed with / should be binded to this service
    ```xml
    <StatelessServiceType ServiceTypeName="StoreWebType">
      <Extensions>
        <Extension Name="Traefik">
          <Labels xmlns="http://schemas.microsoft.com/2015/03/fabact-no-schema">
            <Label Key="traefik.frontend.rule.store-storeweb">PathPrefix: /</Label>
            <Label Key="traefik.expose">true</Label>
            <Label Key="traefik.frontend.passHostHeader">true</Label>
          </Labels>
        </Extension>
      </Extensions>
    </StatelessServiceType>
    ```
6. Since the location API will be available under {domain}/api/location change the way the StoreLocation page retrieves the location list (previously using the internal Service Fabric port).
```c#
namespace StoreWeb.Pages
{
    public class StoreLocationModel : PageModel
    {
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public StoreLocationModel(FabricClient fabricClient, StatelessServiceContext serviceContext)
        {
            this.fabricClient = fabricClient;
            this.serviceContext = serviceContext;
        }

        public string Message { get; set; }

        public string APIUrl { get; set; }

        public void OnGet()
        {
            Message = "Where our stores are located";
            // Replace the service fabric URL resolution by "/api/location"
            this.APIUrl = $"/api/location";
        }
    }
}
```

6. Run locally and test it

### Verifying the deployment

1. Notice that the new routes will be displayed in the Traefik Dashboard (http://localhost:8080).

![Updated Traefik dashboard](images/traefik-dashboard-after-storeapp-deployment.png)

2. Going to http://localhost will display the main page. Internal server-server communication does not rely on Traefik, therefore Index.cshtml.cs has no changes (server communication still relies in Service Fabric)

3. Going to http://localhost/StoreLocation will display the Store locations page, pulling data from /api/location instead of Service Fabric internal URL. Browser communications now rely on Traefik routing (even though  http://localhost:8560/api/location still works locally)

If openning http://localhost displays a different page or you have Traefik deployment errors make sure you don't have an application already listening on port 80 (default IIS installation does that) and 8080.

### Routing used in this example

As metioned before, Service Fabric Traeffik extension will use the routes defined in the ServiceManifest extension to identify which routes should be mapped to the specific service. In our case we defined that:

- if the url starts with "api/location" (after the dns) the request should be binded to the location api
- if the url starts with "api/product" the request should be binded to the product api
- all remaining requests should be binded to the web app

Routing rules are resolved [in descending order by rule length](https://docs.traefik.io/basics/#priorities). In our case:

1. "PathPrefix: /api/location", Length 25
1. "PathPrefix: /api/product", Length 24
1. "PathPrefix: /", Length 13

**Important**: Respect the ' ' (space) between the routing rule type and the value.

Routes can be combined with a semi-colon. Here are a few examples:

| Description | URL | Resolved Service | Route Rule |
|--|--|--|--|
| Match all | //{domain}/x/y/z <br> www.example.com/x/y/z | {service url:port}/x/y/z <br> {service url:port}/x/y/z | PathPrefix: / |
| Using a virtual path | //{domain}/virtualpath/x/y/z<br> www.example.com/virtualpath/details | {service url:port}/x/y/z <br>{service url:port}/details | PathPrefix: /virtualpath/;ReplacePathRegex: /virtualpath/(.*) /$1  |
| Using domain names | //api.{domain}/location <br> api.example.com/location | {service url:port}/api/location <br> {service url:port}/api/location | Host: xxx.domain.com;AddPrefix: /api |

For more possibilities check the [Traefik documentation](https://docs.traefik.io/basics/).

A working example of the solution can be found on this repository under Examples\src\challenges\2\StoreApp in case you have problems making it work.

## Challenge 3: Adding HTTPS support on Traefik

In an production environment it is a common scenario to use Traefik to handle HTTPS requests.

To do so we need to change the Traefik deployment to listen on port 443. Additionally we need to add the respective certificates.
