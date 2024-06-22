# Getting features from database (Web API Example)

This example demonstrates how to get features from a database and use them with Microsoft Feature Management.

## Quickstart

To get started,

1. Simply run the project `GettingFeaturesFromDatabase` and you should see **Swagger UI** open in a new browser window.
2. Try out the `GET /WeatherForecast` endpoint and see the response. It should return a successful response (200 OK).
3. Now, try out `PUT /feature` endpoint to set the state of 'Weather' feature flag to 'false'.
4. Try out the `GET /WeatherForecast` endpoint again and see the response. It should return not found response (404 NotFound).

_Note: You can get the list of features using `GET /feature` endpoint._

## About the example project

This example is a simple Web API project with Entity Framework Core and SQLite database.

The endpoints are,

1. `GET /WeatherForecast` - This is the endpoint where `FeatureGate` attribute is used and access to this endpoint is controlled by the state of 'Weather' feature flag.
2. `GET /feature` - This is the endpoint to get all the feature flags from the database.
3. `PUT /feature` - This is the endpoint to update the state of a feature flag in the database. The query parameters are 'featureName' and 'isEnabled'.

## Seeding the database

During the initial run of the project, an SQLite database file named `example.db` will be created in the root of `GettingFeaturesFromDatabase` directory.
Initial run would also create a table named 'Features' and populate it with a feature flag named 'Weather'.

You can find the database seeding logic inside `/Database/FeatureConfiguration.cs` file.

```csharp
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();

        /* Populate with feature data. */
        builder.HasData(new List<Feature>()
        {
            new Feature { Name = FeatureConstants.Weather, IsEnabled = true },
        });

        builder.ToTable("Features").HasKey(x => x.Name);
    }
```
