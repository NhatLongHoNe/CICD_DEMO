using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Infrastructure.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    private List<Student> InitialData()
    {
        List<Student> StudentSamples = new List<Student>()
        {
            new Student()
            {
                Id = Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048"),
                Name = "John Doe",
                Age = 30
            },

            new Student()
            {
                Id = Guid.Parse("24051c09-df63-4f5f-8a9e-7c7cd9bdca2e"),
                Name = "John Doe",
                Age = 30
            },
            new Student()
            {
                Id = Guid.Parse("4d0d6f82-f71c-49e3-9aaf-634ae7afc56b"),
                Name = "John Doe",
                Age = 30
            },
            new Student()
            {
                Id = Guid.Parse("5664f3e6-7098-4ea6-b4ee-27756654a6a3"),
                Name = "John Doe",
                Age = 30
            },
            new Student()
            {
                Id = Guid.Parse("24cc803d-7f0f-4d8d-974b-7fe7ef49138f"),
                Name = "John Doe",
                Age = 30
            }
        };

        for (int i = 0; i < 1000; i++)
        {
            StudentSamples.Add(new Student()
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Age = 30
            });
        }

        return StudentSamples;
    }

    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Student");
        builder.Property(s => s.Age)
            .IsRequired(false);
        builder.Property(s => s.IsRegularStudent)
            .HasDefaultValue(true);

        builder.HasData(InitialData());

        builder.HasMany(e => e.Evaluations)
            .WithOne(s => s.Student)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}