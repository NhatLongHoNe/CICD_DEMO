using Microsoft.AspNetCore.Mvc;
using Optimiza_RepositoryBase.API.Applications.Interfaces;
using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    public StudentsController(IStudentService studentService)
    {
        this._studentService = studentService;
    }

    [HttpGet("student-details")]
    public IActionResult StudentDetails()
    {
        var result = _studentService.GetStudentsDetail();

        return Ok(result);
    }

    [HttpGet("student-details-by-id")]
    public IActionResult StudentDetailsById()
    {
        var result = _studentService.GetStudentsDetailById();
        if (result is null)
            return Ok(new List<Student>());
        return Ok(result);
    }

    [HttpGet("students")]
    public IActionResult Students()
    {
        var result = _studentService.GetStudents();

        return Ok(result);
    }

    [HttpGet("students-by-id")]
    public IActionResult StudentsById()
    {
        var result = _studentService.FindById(Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048"));
        return Ok(result);
    }

    [HttpGet("students-by-condition")]
    public IActionResult StudentsByCondition()
    {
        var result = _studentService.FindByCondition(Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048"));
        return Ok(result);
    }
}