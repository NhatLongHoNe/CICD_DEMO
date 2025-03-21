namespace Benchmark.Models;

public class StudentSubject
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; }
}