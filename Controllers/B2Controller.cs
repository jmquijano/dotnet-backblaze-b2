using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using BackblazeB2.Helpers;
using BackblazeB2.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BackblazeB2.Controllers
{
	[ApiController, Route("/"), Tags("Backblaze B2 (S3 Compatible)")]
	public class B2Controller : RestApiResponse
	{
		public B2Controller(IConfiguration configuration, IAmazonS3 s3Client) : base(configuration, s3Client)
		{
		}
		
		/// GET /buckets
		/// <summary>
		/// List all list of Buckets
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("buckets")]
		[Produces("application/json")]
		[ProducesResponseType((int) HttpStatusCode.OK)]
		[ProducesResponseType((int) HttpStatusCode.InternalServerError)]
		public async Task<IActionResult> GetBuckets()
		{
			var listBuckets = await _s3Client.ListBucketsAsync();

			var buckets = listBuckets.Buckets.Select(bucket => new
			{
				name = bucket.BucketName,
				created_at = bucket.CreationDate
			}).ToList();
			
			return Success(200, "Buckets has been successfully retrieved.", buckets);
		}

		/// GET /buckets/{bucketName}
		/// <summary>
		/// Retrieve files and folder (objects) within bucket
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("buckets/{bucketName}")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[ProducesResponseType((int)HttpStatusCode.BadRequest)]
		[ProducesResponseType((int)HttpStatusCode.InternalServerError)]
		public async Task<IActionResult> GetObjectsWithinBucket([FromRoute] string bucketName)
		{
			try
			{
				var isBucketNameValid = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

				if (!isBucketNameValid)
				{
					return Error(400, "Bucket name is invalid.");
				}
				
				// Get folder and files within bucket
				var listObjects = await _s3Client.ListObjectsAsync(bucketName);
				
				return Success(200, "Bucket contents has been successfully retrieved.", listObjects.S3Objects);
			}
			catch (Exception)
			{
				return Error(500, "An error occurred while retrieving bucket contents.");
			}
		}
		
		/// GET /buckets/{bucketName}/file
		/// <summary>
		/// Get file object
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[Route("buckets/{bucketName}/file")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[ProducesResponseType((int)HttpStatusCode.BadRequest)]
		[ProducesResponseType((int)HttpStatusCode.InternalServerError)]
		public async Task<IActionResult> GetFile([FromRoute] string bucketName, [FromQuery] string objectName)
		{
			try
			{
				var isBucketNameValid = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

				if (!isBucketNameValid)
				{
					return Error(400, "Bucket name is invalid.");
				}
				
				// Get folder and files within bucket
				var listObjects = await _s3Client.ListObjectsAsync(bucketName);
				
				var file = listObjects.S3Objects.FirstOrDefault(x => x.Key == objectName);
				
				if (file == null)
				{
					return Error(400, "File does not exist.");
				}
				
				// Get download link with 1 hour expiry
				var request = new GetPreSignedUrlRequest
				{
					BucketName = bucketName,
					Key = objectName,
					Expires = DateTime.Now.AddHours(1)
				};
				var url = _s3Client.GetPreSignedURL(request);
				
				
				return Success(200, "File has been successfully retrieved.", new
				{
					url,
				});
			}
			catch (Exception)
			{
				return Error(500, "An error occurred while retrieving file.");
			}
		}
		
		/// PUT /buckets/{bucketName}/file
		/// <summary>
		/// Upload file object
		/// </summary>
		/// <returns></returns>
		[HttpPut]
		[Route("buckets/{bucketName}/file")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[ProducesResponseType((int)HttpStatusCode.BadRequest)]
		[ProducesResponseType((int)HttpStatusCode.InternalServerError)]
		public async Task<IActionResult> PutFile(
			[FromRoute] string bucketName, 
			[FromForm] IFormFile file, 
			[FromForm] PutFileRequest request)
		{
			try
			{
				var isBucketNameValid = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

				if (!isBucketNameValid)
				{
					return Error(400, "Bucket name is invalid.");
				}
				
				string key = request.Key ?? file.FileName;
				
				if (String.IsNullOrEmpty(request.Key) || String.IsNullOrWhiteSpace(request.Key))
				{
					// Generate a default key /{{md5filehash}}/{{filename}}
					var fileHash = Md5Helper.FromFile(file);
					key = fileHash + "/" + file.FileName;
				}
				
				// Get folder and files within bucket
				var listObjects = await _s3Client.ListObjectsAsync(bucketName);
				
				// Check if request.Key file already exists
				bool checkIfFileAlreadyExist = false;

				try
				{
					checkIfFileAlreadyExist = listObjects.S3Objects.Any(x => x.Key == key);
				}
				catch (Exception)
				{
					// ignored
				}

				if (checkIfFileAlreadyExist)
				{
					return Error(400, "File already exists.", new
					{
						key
					});
				}
				
				// Get download link with 1 hour expiry
				var putRequest = new PutObjectRequest
				{
					BucketName = bucketName,
					Key = key,
					ContentType = "application/octet-stream",
					InputStream = file.OpenReadStream()
				};
				
				await _s3Client.PutObjectAsync(putRequest);
				
				return Success(200, "File has been successfully uploaded.", new
				{
					url = await this.GetFileUrl(bucketName, key)
				});
			}
			catch (Exception)
			{
				return Error(500, "An error occurred while uploading file.");
			}
		}
		
		/// <summary>
		/// A non-action method to get file
		/// </summary>
		[NonAction]
		private async Task<string?> GetFileUrl(string bucketName, string objectName)
		{
			try
			{
				var isBucketNameValid = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

				if (!isBucketNameValid)
				{
					return null;
				}
				
				// Get folder and files within bucket
				var listObjects = await _s3Client.ListObjectsAsync(bucketName);
				
				var file = listObjects.S3Objects.FirstOrDefault(x => x.Key == objectName);
				
				if (file == null)
				{
					return null;
				}
				
				// Get download link with 1 hour expiry
				var request = new GetPreSignedUrlRequest
				{
					BucketName = bucketName,
					Key = objectName,
					Expires = DateTime.Now.AddHours(1)
				};
				var url = _s3Client.GetPreSignedURL(request);
				
				return url;
			}
			catch (Exception)
			{
				return null;
			}
		}
		
	}
}

