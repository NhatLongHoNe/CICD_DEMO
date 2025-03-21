using Benchmark.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Benchmark.Services;

public class StudentGoodHttpClient
{
    private static readonly HttpClient client = new HttpClient();
    private const string BaseAddress = "http://localhost:5153/api/StudentsGood";

    public StudentGoodHttpClient()
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<Student>> GetStudentDetailsAsync()
    {
        return await client.GetFromJsonAsync<List<Student>>($"{BaseAddress}/studentgood-details");
    }

    public async Task<List<Student>> GetStudentDetailsByIdAsync()
    {
        return await client.GetFromJsonAsync<List<Student>>($"{BaseAddress}/studentgood-details-by-id");
    }

    public async Task<List<Student>> GetStudentAsync()
    {
        return await client.GetFromJsonAsync<List<Student>>($"{BaseAddress}/studentgoods");
    }

    public async Task<Student> GetStudentByIdAsync()
    {
        return await client.GetFromJsonAsync<Student>($"{BaseAddress}/studentsgood-by-id");
    }

    public async Task<Student> GetStudentByConditionAsync()
    {
        return await client.GetFromJsonAsync<Student>($"{BaseAddress}/studentsgood-by-condition");
    }
}