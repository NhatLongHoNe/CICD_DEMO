using Optimiza_RepositoryBase.API.Abstractions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Optimiza_RepositoryBase.API.Domain;

public class Subject : DomainEntity<Guid>
{
    [Column("SubjectId")]
    public override Guid Id { get; set; }
    public string SubjectName { get; set; }

    public ICollection<StudentSubject> StudentSubjects { get; set; }
}
