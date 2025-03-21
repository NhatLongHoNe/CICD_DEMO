namespace Benchmark.Models;

public class Subject
{
    public Guid Id { get; set; }
    public string SubjectName { get; set; }
    public ICollection<StudentSubject> StudentSubjects { get; set; }
}