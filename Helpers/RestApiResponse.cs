using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

namespace BackblazeB2.Helpers;

public class RestApiResponse : ControllerBase
{
    public readonly IConfiguration Configuration;

    protected readonly IAmazonS3 _s3Client;

    public RestApiResponse(IConfiguration configuration, IAmazonS3 s3Client)
    {
        this.Configuration = configuration;
        _s3Client = s3Client;
    }

    [NonAction]
    public dynamic Success(
        int httpStatusCode,
        string? message = null,
        dynamic? data = null,
        params dynamic[]? otherFields
    )
    {
        Dictionary<string, dynamic> response = new Dictionary<string, dynamic> { { "success", true } };

        if (message != null)
        {
            response.Add("message", message);
        }

        if (data != null)
        {
            response.Add("data", data);
        }

        if (otherFields is null) return StatusCode(httpStatusCode, response);
        foreach (var field in otherFields)
        {
            switch (field)
            {
                case IDictionary<string, dynamic> dict:
                {
                    foreach (var value in dict)
                    {
                        response.Add(value.Key, value.Value);
                    }

                    break;
                }
            }
        }

        return StatusCode(httpStatusCode, response);
    }

    [NonAction]
    public dynamic Error(
        int httpStatusCode, 
        string? message = null, 
        dynamic? errordata = null
    )
    {
        Dictionary<string, dynamic> response = new Dictionary<string, dynamic> { { "success", false } };

        if (message != null)
        {
            response.Add("message", message);
        }

        if (errordata != null)
        {
            response.Add("errors", errordata);
        }

        return StatusCode(httpStatusCode, response);
    }
}