using Microsoft.EntityFrameworkCore;
using Optimiza_RepositoryBase.API.Domain;
using Optimiza_RepositoryBase.API.Infrastructure.Configurations;

namespace Optimiza_RepositoryBase.API.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new StudentSubjectConfiguration());

        modelBuilder.Entity<StudentDetails>().HasData(
            new StudentDetails()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048"),
                Address = "Ha Noi ",
                AdditionalInformation = "Senior Solution Architecture"
            },
            new StudentDetails()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.Parse("24051c09-df63-4f5f-8a9e-7c7cd9bdca2e"),
                Address = "Ha Noi ",
                AdditionalInformation = "Senior Solution Architecture"
            },
            new StudentDetails()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.Parse("4d0d6f82-f71c-49e3-9aaf-634ae7afc56b"),
                Address = "Ha Noi ",
                AdditionalInformation = "Senior Solution Architecture"
            },
            new StudentDetails()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.Parse("5664f3e6-7098-4ea6-b4ee-27756654a6a3"),
                Address = "Ha Noi ",
                AdditionalInformation = "Senior Solution Architecture"
            },
            new StudentDetails()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.Parse("24cc803d-7f0f-4d8d-974b-7fe7ef49138f"),
                Address = "Ha Noi ",
                AdditionalInformation = "Senior Solution Architecture"
            }
        );

        modelBuilder.Entity<Evaluation>().HasData(
            new Evaluation()
            {
                Id = Guid.NewGuid(),
                Grade = 10,
                AdditionalExplanation = "Good",
                StudentId = Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048")
            },
            new Evaluation()
            {
                Id = Guid.NewGuid(),
                Grade = 10,
                AdditionalExplanation = "Good",
                StudentId = Guid.Parse("24051c09-df63-4f5f-8a9e-7c7cd9bdca2e")
            },
            new Evaluation()
            {
                Id = Guid.NewGuid(),
                Grade = 10,
                AdditionalExplanation = "Good",
                StudentId = Guid.Parse("24051c09-df63-4f5f-8a9e-7c7cd9bdca2e")
            }
        ); ;
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }
    public DbSet<StudentDetails> StudentDetails { get; set; }
}

