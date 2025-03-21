using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Optimiza_RepositoryBase.API.Applications.Interfaces;
using Optimiza_RepositoryBase.API.Domain;

namespace Optimiza_RepositoryBase.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentsGoodController : ControllerBase
{
    private readonly IStudentServiceGood _studentServiceGood;

    public StudentsGoodController(IStudentServiceGood studentServiceGood)
    {
        this._studentServiceGood = studentServiceGood;
    }

    [HttpGet("studentgood-details")]
    public IActionResult StudentDetails()
    {
        var result = _studentServiceGood.GetStudentsDetail();

        return Ok(result);
    }

    [HttpGet("studentgood-details-by-id")]
    public IActionResult StudentDetailsById()
    {
        var result = _studentServiceGood.GetStudentsDetailById();
        if (result is null)
            return Ok(new List<Student>());
        return Ok(result);
    }

    [HttpGet("studentgoods")]
    public IActionResult Students()
    {
        var result = _studentServiceGood.GetStudents();

        return Ok(result);
    }

    [HttpGet("studentsgood-by-id")]
    public async Task<IActionResult> StudentsById()
    {
        var result = await _studentServiceGood.FindByIdAsync(Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048"));
        if (result == null)
            return NotFound();
        return Ok(result);
    }


    [HttpGet("studentsgood-by-condition")]
    public async Task<IActionResult> StudentsByCondition()
    {
        var result = await _studentServiceGood.FindByConditionAsync(Guid.Parse("3cdd6589-2cb2-4794-a01f-f9c0c602d048"));
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}