using Optimiza_RepositoryBase.API.Abstractions;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Optimiza_RepositoryBase.API.Domain;

[Table("Student")]
public class Student : DomainEntity<Guid>
{
    [Column("StudentId")]
    public override Guid Id { get; set; }

    [Required]
    [MaxLength(50, ErrorMessage = "Length must be less then 50 characters")]
    public string Name { get; set; }

    public int? Age { get; set; }
    public bool IsRegularStudent { get; set; }

    public StudentDetails StudentDetails { get; set; }

    public ICollection<Evaluation> Evaluations { get; set; }

    public ICollection<StudentSubject> StudentSubjects { get; set; }
}
