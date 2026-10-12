using System.Text.Json;
using Kronos.RosterWizard.Domain;

namespace Kronos.RosterWizard.Infrastructure;

public sealed class JsonRosterRepository : IRosterRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "roster-data.json");
    private RosterData? _data;

    public async Task<RosterData> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return Clone(_data!);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAsync(Action<RosterData> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            var candidate = Clone(_data!);
            update(candidate);
            RosterDataValidator.Validate(candidate);
            await PersistAsync(candidate, cancellationToken);
            _data = candidate;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceAsync(RosterData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var candidate = Clone(data);
            RosterDataValidator.Validate(candidate);
            await PersistAsync(candidate, cancellationToken);
            _data = candidate;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_data is not null)
        {
            return;
        }

        if (File.Exists(_filePath))
        {
            await using var stream = File.OpenRead(_filePath);
            _data = await JsonSerializer.DeserializeAsync<RosterData>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException($"The roster data file '{_filePath}' is empty or invalid.");
            RosterDataValidator.Validate(_data);
        }
        else
        {
            _data = RosterDataSeeder.Create();
            RosterDataValidator.Validate(_data);
            await PersistAsync(_data, cancellationToken);
        }
    }

    private async Task PersistAsync(RosterData data, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("The application data directory is unavailable.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_filePath}.tmp";

        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private static RosterData Clone(RosterData data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        return JsonSerializer.Deserialize<RosterData>(json, JsonOptions)
            ?? throw new InvalidDataException("Roster data could not be copied.");
    }
}
