using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.UnitTests;

public sealed class TaskDtoJsonSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void CreateTaskRequest_DeserializesExactUserPayload_WithEnumStrings()
    {
        const string payload = """
        {
          "title": "Implementation User Authentication",
          "description": "Impliement user atuthentication",
          "status": "ToDo",
          "priority": "Medium",
          "deadline": "2026-09-24",
          "teamId": "ada95511-dae7-42ec-8322-8c169b018f91",
          "assignedToId": "cabb58c4-e437-4737-a8e2-0aa6c4d7ed4d"
        }
        """;

        var request = JsonSerializer.Deserialize<CreateTaskRequest>(payload, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Implementation User Authentication", request.Title);
        Assert.Equal("Impliement user atuthentication", request.Description);
        Assert.Equal(TaskStatus.ToDo, request.Status);
        Assert.Equal(TaskPriority.Medium, request.Priority);
        Assert.Equal(new DateOnly(2026, 9, 24), request.Deadline);
        Assert.Equal(Guid.Parse("ada95511-dae7-42ec-8322-8c169b018f91"), request.TeamId);
        Assert.Equal(Guid.Parse("cabb58c4-e437-4737-a8e2-0aa6c4d7ed4d"), request.AssignedToId);
    }

    [Theory]
    [InlineData("ToDo", TaskStatus.ToDo, "Low", TaskPriority.Low)]
    [InlineData("InProgress", TaskStatus.InProgress, "Medium", TaskPriority.Medium)]
    [InlineData("Done", TaskStatus.Done, "High", TaskPriority.High)]
    public void CreateTaskRequest_DeserializesAllStatusAndPriorityEnumStrings(
        string statusStr, TaskStatus expectedStatus,
        string priorityStr, TaskPriority expectedPriority)
    {
        var json = $$"""
        {
          "title": "Test Task",
          "status": "{{statusStr}}",
          "priority": "{{priorityStr}}",
          "deadline": "2026-10-01",
          "teamId": "11111111-1111-1111-1111-111111111111",
          "assignedToId": "22222222-2222-2222-2222-222222222222"
        }
        """;

        var request = JsonSerializer.Deserialize<CreateTaskRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(expectedStatus, request.Status);
        Assert.Equal(expectedPriority, request.Priority);
    }

    [Theory]
    [InlineData("ToDo", TaskStatus.ToDo)]
    [InlineData("InProgress", TaskStatus.InProgress)]
    [InlineData("Done", TaskStatus.Done)]
    public void UpdateTaskStatusRequest_DeserializesAllStatusEnumStrings(string statusStr, TaskStatus expectedStatus)
    {
        var json = $$"""{ "status": "{{statusStr}}" }""";

        var request = JsonSerializer.Deserialize<UpdateTaskStatusRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(expectedStatus, request.Status);
    }

    [Theory]
    [InlineData("Low", TaskPriority.Low)]
    [InlineData("Medium", TaskPriority.Medium)]
    [InlineData("High", TaskPriority.High)]
    public void UpdateTaskRequest_DeserializesAllPriorityEnumStrings(string priorityStr, TaskPriority expectedPriority)
    {
        var json = $$"""{ "priority": "{{priorityStr}}" }""";

        var request = JsonSerializer.Deserialize<UpdateTaskRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(expectedPriority, request.Priority);
    }
}

