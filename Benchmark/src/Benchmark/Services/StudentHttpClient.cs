using Benchmark.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Benchmark.Services;

public class StudentHttpClient
{
    private static readonly HttpClient client = new HttpClient();
    private const string BaseAddress = "http://localhost:5153/api/Students";

    public StudentHttpClient()
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<Student>> GetStudentDetailsAsync()
    {
        return await client.GetFromJsonAsync<List<Student>>($"{BaseAddress}/student-details");
    }
    public async Task<List<Student>> GetStudentDetailsByIdAsync()
    {
        return await client.GetFromJsonAsync<List<Student>>($"{BaseAddress}/student-details-by-id");
    }

    public async Task<List<Student>> GetStudentAsync()
    {
        return await client.GetFromJsonAsync<List<Student>>($"{BaseAddress}/students");
    }

    public async Task<Student> GetStudentByIdAsync()
    {
        return await client.GetFromJsonAsync<Student>($"{BaseAddress}/students-by-id");
    }


    public async Task<Student> GetStudentByConditionAsync()
    {
        return await client.GetFromJsonAsync<Student>($"{BaseAddress}/students-by-condition");
    }
}