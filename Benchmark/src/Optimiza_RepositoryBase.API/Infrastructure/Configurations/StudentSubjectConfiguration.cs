using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Infrastructure.Configurations;

public class StudentSubjectConfiguration : IEntityTypeConfiguration<StudentSubject>
{
    public void Configure(EntityTypeBuilder<StudentSubject> builder)
    {
        builder.HasKey(s => new { s.StudentId, s.SubjectId });

        builder.HasOne(ss => ss.Student)
            .WithMany(s => s.StudentSubjects)
            .HasForeignKey(ss => ss.StudentId);

        builder.HasOne(ss => ss.Subject)
            .WithMany(s => s.StudentSubjects)
            .HasForeignKey(ss => ss.SubjectId);
    }
}
