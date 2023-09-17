using System.ComponentModel.DataAnnotations.Schema;

namespace BackblazeB2.Requests;

/// <summary>
/// Put File Request Model
/// </summary>
public class PutFileRequest
{
    // Key
    [Column(name: "key")] public string? Key { get; set; } = null;
}