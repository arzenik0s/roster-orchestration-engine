using System.Text;
using System.Text.Json;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Infrastructure;

namespace Kronos.RosterWizard.Application.Services;

public sealed class RosterDataTransferService(IRosterRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<string> ExportJsonAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.GetSnapshotAsync(cancellationToken);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public async Task ImportJsonAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var imported = await JsonSerializer.DeserializeAsync<RosterData>(source, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The selected file does not contain roster data.");
        RosterDataValidator.Validate(imported);
        await repository.ReplaceAsync(imported, cancellationToken);
    }

    public async Task<string> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var data = await repository.GetSnapshotAsync(cancellationToken);
        var staffById = data.Staff.ToDictionary(staff => staff.Id);
        var rolesById = data.Roles.ToDictionary(role => role.Id);
        var shiftsById = data.Shifts.ToDictionary(shift => shift.Id);
        var builder = new StringBuilder();
        builder.AppendLine("date,shift,staff_email,staff_name,roles");

        foreach (var slot in data.Slots.OrderBy(slot => slot.Date).ThenBy(slot => shiftsById.GetValueOrDefault(slot.ShiftId)?.ShiftType))
        {
            if (!shiftsById.TryGetValue(slot.ShiftId, out var shift))
            {
                continue;
            }

            if (slot.StaffIds.Count == 0)
            {
                builder.Append(Csv(slot.Date.ToString("yyyy-MM-dd"))).Append(',')
                    .Append(Csv(shift.ShiftType)).AppendLine(",,,");
                continue;
            }

            foreach (var staffId in slot.StaffIds)
            {
                if (!staffById.TryGetValue(staffId, out var staff))
                {
                    continue;
                }

                var roleNames = staff.RoleIds
                    .Where(rolesById.ContainsKey)
                    .Select(roleId => rolesById[roleId].RoleName);
                builder.Append(Csv(slot.Date.ToString("yyyy-MM-dd"))).Append(',')
                    .Append(Csv(shift.ShiftType)).Append(',')
                    .Append(Csv(staff.Email)).Append(',')
                    .Append(Csv($"{staff.FirstName} {staff.LastName}".Trim())).Append(',')
                    .AppendLine(Csv(string.Join("; ", roleNames)));
            }
        }

        return builder.ToString();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
