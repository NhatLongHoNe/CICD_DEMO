using Benchmark.Services;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[HtmlExporter]
public class BenchmarkHarness
{
    [Params(1, 5)]
    public int IterationCount;
    private readonly StudentHttpClient _studentHttpClient = new StudentHttpClient();
    private readonly StudentGoodHttpClient _studentGoodHttpClient = new StudentGoodHttpClient();

    /// <summary>
    /// Has Data within Include statement
    /// </summary>
    /// <returns></returns>
    [Benchmark]
    public async Task Student_GetStudentDetails()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentHttpClient.GetStudentDetailsAsync();
        }
    }
    [Benchmark]
    public async Task StudentGood_GetStudentDetails()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentGoodHttpClient.GetStudentDetailsAsync();
        }
    }

    /// <summary>
    /// Notfound data to see if SplitQuery has faster or not
    /// </summary>
    /// <returns></returns>
    [Benchmark]
    public async Task Student_GetStudentDetailsByIdNotFound()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentHttpClient.GetStudentDetailsByIdAsync();
        }
    }
    [Benchmark]
    public async Task StudentGood_GetStudentDetailsByIdNotFound()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentGoodHttpClient.GetStudentDetailsByIdAsync();
        }
    }

    /// <summary>
    /// Has No Data within Include statement
    /// </summary>
    /// <returns></returns>

    [Benchmark]
    public async Task Student_GetStudents()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentHttpClient.GetStudentAsync();
        }
    }
    [Benchmark]
    public async Task StudentGood_GetStudents()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentGoodHttpClient.GetStudentAsync();
        }
    }

    /// <summary>
    /// FindById-FindByCondition Async - Sync
    /// </summary>
    /// <returns></returns>
    [Benchmark]
    public async Task Student_GetStudentsByIdAsync()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentHttpClient.GetStudentByIdAsync();
        }
    }
    [Benchmark]
    public async Task StudentGood_GetStudentsByIdAsync()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentGoodHttpClient.GetStudentByIdAsync();
        }
    }


    [Benchmark]
    public async Task Student_GetStudentsByConditionAsunc()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentHttpClient.GetStudentByConditionAsync();
        }
    }
    [Benchmark]
    public async Task StudentGood_GetStudentsByConditionAsync()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            await _studentGoodHttpClient.GetStudentByConditionAsync();
        }
    }
}