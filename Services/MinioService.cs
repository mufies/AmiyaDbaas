// using Minio;
// using Minio.DataModel;
//
// namespace AmiyaDbaasManager.Services
// {
//     public class MinioService
//     {
//         private readonly IMinioClient _minioClient;
//         private readonly string _bucketName;
//
//         public MinioService(IMinioClient minioClient, IConfiguration configuration)
//         {
//             _minioClient = minioClient;
//             _bucketName = configuration["Minio:Bucket"];
//         }
//         // ─── Upload ─────────────────────────────────────────────────────────────
//         public async Task UploadAsync(string objectName, string filePath)
//         {
//             await _minioClient.PutObjectAsync(_bucketName, objectName, filePath);
//         }
//
//         // ─── Download ────────────────────────────────────────────────────────────
//         public async Task DownloadAsync(string objectName, string filePath)
//         {
//             await _minioClient.GetObjectAsync(_bucketName, objectName, stream =>
//             {
//                 using var fileStream = File.Create(filePath);
//                 stream.CopyTo(fileStream);
//             });
//         }
//
//         // ─── Check Exists ────────────────────────────────────────────────────────
//         public async Task<bool> CheckExistsAsync(string objectName)
//         {
//             try
//             {
//                 await _minioClient.StatObjectAsync(_bucketName, objectName);
//                 return true;
//             }
//             catch (Exception)
//             {
//                 return false;
//             }
//         }
//
//         // ─── List ────────────────────────────────────────────────────────────────
//         public async Task<List<string>> ListAsync()
//         {
//             var objects = new List<string>();
//             await foreach (var obj in _minioClient.ListObjectsAsync(_bucketName))
//             {
//                 objects.Add(obj.Key);
//             }
//             return objects;
//         }
//     }
// }
