using System.Text.Json;
using System.Text.Json.Nodes;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Application.Utilities;

/// <summary>
/// JSON Diff utility (: JSON Patch)
/// - op: add | remove | replace | move
/// - path: JSON Pointer
/// - from: move
/// - valueJson: add/replace (JSON)
/// </summary>
public static class JsonDiffUtility
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static JsonDiffResultDto Diff(object? oldValue, object? newValue)
    {
        var oldNode = SerializeToNode(oldValue);
        var newNode = SerializeToNode(newValue);

        var operations = new List<JsonDiffOperationDto>();
        DiffInternal(oldNode, newNode, string.Empty, operations);

        return new JsonDiffResultDto(operations);
    }

    private static JsonNode? SerializeToNode(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(value, JsonOptions);
    }

    private static void DiffInternal(
        JsonNode? oldNode,
        JsonNode? newNode,
        string path,
        List<JsonDiffOperationDto> operations)
    {
        if (oldNode is null && newNode is null)
        {
            return;
        }

        if (oldNode is null)
        {
            AddOperation(operations, "add", path, newNode);
            return;
        }

        if (newNode is null)
        {
            AddOperation(operations, "remove", path);
            return;
        }

        if (JsonNode.DeepEquals(oldNode, newNode))
        {
            return;
        }

        if (oldNode is JsonObject oldObject && newNode is JsonObject newObject)
        {
            DiffObject(oldObject, newObject, path, operations);
            return;
        }

        if (oldNode is JsonArray oldArray && newNode is JsonArray newArray)
        {
            DiffArray(oldArray, newArray, path, operations);
            return;
        }

        if (oldNode is JsonValue && newNode is JsonValue)
        {
            AddOperation(operations, "replace", path, newNode);
            return;
        }

        AddOperation(operations, "replace", path, newNode);
    }

    private static void DiffObject(
        JsonObject oldObject,
        JsonObject newObject,
        string path,
        List<JsonDiffOperationDto> operations)
    {
        var oldKeys = oldObject.Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
        var newKeys = newObject.Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);

        foreach (var removedKey in oldKeys.Except(newKeys))
        {
            AddOperation(operations, "remove", CombinePath(path, removedKey));
        }

        foreach (var addedKey in newKeys.Except(oldKeys))
        {
            AddOperation(operations, "add", CombinePath(path, addedKey), newObject[addedKey]);
        }

        foreach (var commonKey in oldKeys.Intersect(newKeys))
        {
            DiffInternal(
                oldObject[commonKey],
                newObject[commonKey],
                CombinePath(path, commonKey),
                operations);
        }
    }

    private static void DiffArray(
        JsonArray oldArray,
        JsonArray newArray,
        string path,
        List<JsonDiffOperationDto> operations)
    {
        var oldCount = oldArray.Count;
        var newCount = newArray.Count;

        var oldHashes = oldArray.Select(GetNodeFingerprint).ToList();
        var newHashes = newArray.Select(GetNodeFingerprint).ToList();

        var oldIndexMap = BuildIndexMap(oldHashes);
        var newIndexMap = BuildIndexMap(newHashes);

        var handledOld = new bool[oldCount];
        var handledNew = new bool[newCount];

        // Move detection
        foreach (var (hash, oldQueue) in oldIndexMap)
        {
            if (!newIndexMap.TryGetValue(hash, out var newQueue))
            {
                continue;
            }

            while (oldQueue.Count > 0 && newQueue.Count > 0)
            {
                var oldIndex = oldQueue.Dequeue();
                var newIndex = newQueue.Dequeue();
                if (oldIndex != newIndex)
                {
                    AddOperation(
                        operations,
                        "move",
                        CombinePath(path, newIndex.ToString()),
                        from: CombinePath(path, oldIndex.ToString()));
                }

                handledOld[oldIndex] = true;
                handledNew[newIndex] = true;
            }
        }

        // Diff remaining indices by position
        var minCount = Math.Min(oldCount, newCount);
        for (var i = 0; i < minCount; i++)
        {
            if (handledOld[i] || handledNew[i])
            {
                continue;
            }

            DiffInternal(oldArray[i], newArray[i], CombinePath(path, i.ToString()), operations);
            handledOld[i] = true;
            handledNew[i] = true;
        }

        // Removals (reverse order to preserve indices)
        for (var i = oldCount - 1; i >= 0; i--)
        {
            if (!handledOld[i])
            {
                AddOperation(operations, "remove", CombinePath(path, i.ToString()));
            }
        }

        // Additions
        for (var i = 0; i < newCount; i++)
        {
            if (!handledNew[i])
            {
                AddOperation(operations, "add", CombinePath(path, i.ToString()), newArray[i]);
            }
        }
    }

    private static Dictionary<string, Queue<int>> BuildIndexMap(IReadOnlyList<string> hashes)
    {
        var map = new Dictionary<string, Queue<int>>(StringComparer.Ordinal);
        for (var i = 0; i < hashes.Count; i++)
        {
            var hash = hashes[i];
            if (!map.TryGetValue(hash, out var queue))
            {
                queue = new Queue<int>();
                map[hash] = queue;
            }
            queue.Enqueue(i);
        }

        return map;
    }

    private static string GetNodeFingerprint(JsonNode? node)
    {
        if (node is null)
        {
            return "null";
        }

        return node.ToJsonString(JsonOptions);
    }

    private static void AddOperation(
        List<JsonDiffOperationDto> operations,
        string op,
        string path,
        JsonNode? value = null,
        string? from = null)
    {
        operations.Add(new JsonDiffOperationDto(
            Op: op,
            Path: path,
            ValueJson: value is null ? null : value.ToJsonString(JsonOptions),
            From: from));
    }

    private static string CombinePath(string path, string segment)
    {
        var escapedSegment = EscapeJsonPointerSegment(segment);
        return string.IsNullOrEmpty(path)
            ? $"/{escapedSegment}"
            : $"{path}/{escapedSegment}";
    }

    private static string EscapeJsonPointerSegment(string segment)
    {
        return segment.Replace("~", "~0").Replace("/", "~1");
    }
}
