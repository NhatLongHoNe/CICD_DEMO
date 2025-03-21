using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Applications.Interfaces;

public interface IStudentService
{
    List<Student> GetStudentsDetail();

    List<Student> GetStudentsDetailById(); // => Not Found

    List<Student> GetStudents();

    Student FindById(Guid Id);
    Student FindByCondition(Guid Id);
}

