using System.IO.Compression;

namespace DatabaseBackupUtility.Services;

public static class CompressionService
{
    public static async Task CompressFileAsync(string sourceFilePath, string destinationFilePath)
    {
        await using var sourceStream = File.OpenRead(sourceFilePath);
        await using var destinationStream = File.Create(destinationFilePath);
        await using var gzipStream = new GZipStream(destinationStream, CompressionMode.Compress);
        await sourceStream.CopyToAsync(gzipStream);
    }

    public static async Task DecompressFileAsync(string sourceFilePath, string destinationFilePath)
    {
        await using var sourceStream = File.OpenRead(sourceFilePath);
        await using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
        await using var destinationStream = File.Create(destinationFilePath);
        await gzipStream.CopyToAsync(destinationStream);
    }
}
