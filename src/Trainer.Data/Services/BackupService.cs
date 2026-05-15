using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Trainer.Core.Abstractions;
using Trainer.Core.Entities;

namespace Trainer.Data.Services;

public class BackupService(TrainerDbContext db) : IBackupService
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new TimeOnlyJsonConverter() },
    };

    public async Task<string> ExportJsonAsync(CancellationToken ct = default)
    {
        var types = await db.TrainingTypes.AsNoTracking().ToListAsync(ct);
        var clients = await db.Clients.AsNoTracking().ToListAsync(ct);
        var sessions = await db.Sessions.AsNoTracking()
            .Include(s => s.Schedule)
            .Include(s => s.Members)
            .ToListAsync(ct);

        var dto = new BackupDto
        {
            Version = CurrentVersion,
            ExportedAt = DateTimeOffset.UtcNow,
            TrainingTypes = types.Select(t => new TrainingTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                SortOrder = t.SortOrder,
            }).ToList(),
            Clients = clients.Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Notes = c.Notes,
                IsActive = c.IsActive,
            }).ToList(),
            Sessions = sessions.Select(s => new SessionDto
            {
                Id = s.Id,
                Title = s.Title,
                TrainingTypeId = s.TrainingTypeId,
                Notes = s.Notes,
                IsActive = s.IsActive,
                Schedule = s.Schedule.Select(sl => new ScheduleSlotDto
                {
                    Day = sl.Day,
                    Time = sl.Time,
                }).ToList(),
                MemberIds = s.Members.Select(m => m.Id).ToList(),
            }).ToList(),
        };

        return JsonSerializer.Serialize(dto, JsonOpts);
    }

    public async Task ImportJsonAsync(string json, CancellationToken ct = default)
    {
        BackupDto? data;
        try
        {
            data = JsonSerializer.Deserialize<BackupDto>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Не удалось прочитать файл бэкапа: {ex.Message}");
        }

        if (data is null)
            throw new InvalidOperationException("Файл бэкапа пустой");
        if (data.Version > CurrentVersion)
            throw new InvalidOperationException(
                $"Бэкап сделан более новой версией приложения (v{data.Version}). Обнови приложение и попробуй снова.");

        // Replace-all: чистим всё через DbContext (cascade delete на FK сам уберёт
        // SessionMembers и ScheduleSlots).
        db.Sessions.RemoveRange(db.Sessions);
        db.Clients.RemoveRange(db.Clients);
        db.TrainingTypes.RemoveRange(db.TrainingTypes);
        await db.SaveChangesAsync(ct);

        foreach (var t in data.TrainingTypes)
            db.TrainingTypes.Add(new TrainingType
            {
                Id = t.Id,
                Name = t.Name,
                SortOrder = t.SortOrder,
            });

        foreach (var c in data.Clients)
            db.Clients.Add(new Client
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Notes = c.Notes,
                IsActive = c.IsActive,
            });

        await db.SaveChangesAsync(ct);

        foreach (var s in data.Sessions)
        {
            db.Sessions.Add(new Session
            {
                Id = s.Id,
                Title = s.Title,
                TrainingTypeId = s.TrainingTypeId,
                Notes = s.Notes,
                IsActive = s.IsActive,
                Schedule = s.Schedule.Select(sl => new ScheduleSlot
                {
                    Day = sl.Day,
                    Time = sl.Time,
                }).ToList(),
            });
        }
        await db.SaveChangesAsync(ct);

        foreach (var s in data.Sessions)
            foreach (var memberId in s.MemberIds)
                db.SessionMembers.Add(new SessionMember
                {
                    SessionId = s.Id,
                    ClientId = memberId,
                });
        await db.SaveChangesAsync(ct);
    }

    // --- DTOs (внутренний формат файла) ---

    private class BackupDto
    {
        public int Version { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
        public List<TrainingTypeDto> TrainingTypes { get; set; } = new();
        public List<ClientDto> Clients { get; set; } = new();
        public List<SessionDto> Sessions { get; set; } = new();
    }

    private class TrainingTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    private class ClientDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }

    private class SessionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid TrainingTypeId { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public List<ScheduleSlotDto> Schedule { get; set; } = new();
        public List<Guid> MemberIds { get; set; } = new();
    }

    private class ScheduleSlotDto
    {
        public DayOfWeek Day { get; set; }
        public TimeOnly Time { get; set; }
    }

    // System.Text.Json не умеет TimeOnly до .NET 8+ — есть, но формат не совсем как в БД.
    // Сериализуем как "HH:mm:ss" чтобы был стабильный человекочитаемый формат.
    private class TimeOnlyJsonConverter : System.Text.Json.Serialization.JsonConverter<TimeOnly>
    {
        private const string Format = "HH:mm:ss";

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeOnly.ParseExact(reader.GetString()!, Format);

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format));
    }
}
