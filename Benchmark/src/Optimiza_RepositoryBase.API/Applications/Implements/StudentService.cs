using Optimiza_RepositoryBase.API.Abstractions;
using Optimiza_RepositoryBase.API.Applications.Interfaces;
using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Applications.Implements;

public class StudentService : IStudentService
{
    private readonly IRepositoryBase<Student, Guid> _studentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StudentService(IRepositoryBase<Student, Guid> studentRepository,
           IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
    }

    public Student FindByCondition(Guid Id)
    {
        return _studentRepository.FindSingle(x => x.Id.Equals(Id));
    }

    public Student FindById(Guid Id)
    {
        return _studentRepository.FindById(Id);
    }

    // Check Case: AsNoTracking(); With No Include()
    public List<Student> GetStudents()
    {
        return _studentRepository.FindAll().OrderBy(x => x.Name).ToList();
    }

    // Check Case: AsNoTracking(); With Include() =>> SplitQuery() =>> Get All Student and StudentDetails And Evaluation
    public List<Student> GetStudentsDetail()
    {
        return _studentRepository.FindAll(x => x.StudentDetails, x => x.Evaluations).OrderBy(x => x.Name).ToList();
    }

    // Check Case: AsNoTracking(); With Include() =>> SplitQuery() => Get data but not found
    public List<Student> GetStudentsDetailById()
    {
        return _studentRepository.FindAll(x => x.StudentDetails, x => x.Evaluations)
            .Where(x => x.Id.Equals(Guid.Parse("02225ea0-4029-41f7-b117-9a41cde997fd")))
            .OrderBy(x => x.Name).ToList();
    }

}
