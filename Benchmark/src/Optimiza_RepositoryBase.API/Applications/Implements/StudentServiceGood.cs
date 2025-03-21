using Optimiza_RepositoryBase.API.Abstractions;
using Optimiza_RepositoryBase.API.Applications.Interfaces;
using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Applications.Implements;

public class StudentServiceGood : IStudentServiceGood
{
    private readonly IRepositoryBaseGood<Student, Guid> _studentRepositoryGood;
    private readonly IUnitOfWork _unitOfWork;

    public StudentServiceGood(IRepositoryBaseGood<Student, Guid> studentRepositoryGood,
           IUnitOfWork unitOfWork)
    {
        _studentRepositoryGood = studentRepositoryGood;
        _unitOfWork = unitOfWork;
    }

    public async Task<Student> FindByConditionAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        return await _studentRepositoryGood.FindSingleAsync(x => x.Id.Equals(Id), cancellationToken);
    }

    public async Task<Student> FindByIdAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        return await _studentRepositoryGood.FindByIdAsync(Id, cancellationToken);
    }

    // Check Case: AsNoTracking(); With No Include()
    public List<Student> GetStudents()
    {
        return _studentRepositoryGood.FindAll().OrderBy(x => x.Name).ToList();
    }

    // Check Case: AsNoTracking(); With Include() =>> SplitQuery() =>> Get All Student and StudentDetails And Evaluation
    public List<Student> GetStudentsDetail()
    {
        return _studentRepositoryGood.FindAll(x => x.StudentDetails, x => x.Evaluations).OrderBy(x => x.Name).ToList();
    }

    // Check Case: AsNoTracking(); With Include()  =>> SplitQuery() => Get data but not found
    public List<Student> GetStudentsDetailById()
    {
        return _studentRepositoryGood.FindAll(x => x.StudentDetails, x => x.Evaluations)
            .Where(x => x.Id.Equals(Guid.Parse("02225ea0-4029-41f7-b117-9a41cde997fd")))
            .OrderBy(x => x.Name).ToList();
    }
}