using GettingFeaturesFromDatabase.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GettingFeaturesFromDatabase.Database;

internal sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
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
}
