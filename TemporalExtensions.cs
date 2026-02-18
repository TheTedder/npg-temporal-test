using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public static class TemporalExtensions
{
    public static KeyBuilder HasTemporalKey<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, object?>> keyExpression) where TEntity : class
    {
        // Get the range property.
        var members = keyExpression.GetMemberAccessList();
        //return entityTypeBuilder.HasTemporalKey(members.Select((member) => member.Name).ToArray());
        var prop = entityTypeBuilder.Metadata.FindProperty(members[^1].Name);
        string pgtype = prop.GetColumnType();

        // Give it a default range of now to infinity for new records.
        entityTypeBuilder.Property(prop.Name).HasDefaultValueSql($"{pgtype}(now(), 'infinity', '[)')");
        return entityTypeBuilder.HasKey(keyExpression).WithoutOverlaps();
    }

    public static KeyBuilder<TEntity> HasTemporalKey<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        params string[] propertyNames) where TEntity : class
    {
        var prop = entityTypeBuilder.Metadata.FindProperty(propertyNames[^1]);
        string pgtype = prop.GetColumnType();

        entityTypeBuilder.Property(prop.Name).HasDefaultValueSql($"{pgtype}(now(), 'infinity', '[)')");
        return entityTypeBuilder.HasKey(propertyNames).WithoutOverlaps();
    }

    public static KeyBuilder HasTemporalKey(
        this EntityTypeBuilder entityTypeBuilder,
        params string[] propertyNames)
    {
        var prop = entityTypeBuilder.Metadata.FindProperty(propertyNames[^1]);
        string pgtype = prop.GetColumnType();
        Console.WriteLine($"got type {pgtype}");

        entityTypeBuilder.Property(prop.Name).HasDefaultValueSql($"{pgtype}(now(), 'infinity', '[)')");
        return entityTypeBuilder.HasKey(propertyNames).WithoutOverlaps();
    }

    public static void WithCurrentOnlyFilter(this KeyBuilder keyBuilder)
    {
        var prop = keyBuilder.Metadata.Properties[^1];
        var propinfo = prop.PropertyInfo;        
        Type rangeDbFuncs = typeof(NpgsqlRangeDbFunctionsExtensions);
        var contains = rangeDbFuncs.GetMethod("Contains", 1, [typeof(DateTime)]);
        var param = Expression.Parameter(keyBuilder.Metadata.DeclaringEntityType.ClrType);
        Type dateTime = typeof(DateTime);

        var now = dateTime.GetProperty(prop.GetColumnType() switch
        {
            "daterange" => "Today",
            "tsrange" => "UtcNow",
            "tstzrange" => "Now",
            string unk => throw new Exception($"unknown range type {unk}"),
            null => throw new Exception("no range type available")
        });

        keyBuilder.Metadata.DeclaringEntityType.SetQueryFilter(
            "TemporalFilterCurrentOnly",
            Expression.Lambda(
                Expression.Call(
                    Expression.Property(param, propinfo),
                    contains,
                    Expression.Property(null, now)),
                param));
    }

    public static void WithCurrentOnlyFilter<TEntity>(this KeyBuilder<TEntity> keyBuilder) => keyBuilder.WithCurrentOnlyFilter();
}