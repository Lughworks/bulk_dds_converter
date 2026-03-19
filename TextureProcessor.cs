using DirectXTexNet;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BulkDDSConverter;

public enum DdsFormat {
    BC1,   // Opaque / 1-bit alpha
    BC3,   // Interpolated alpha
    BC4,   // Single channel (Grayscale)
    BC5,   // Dual channel (Normals)
    BC6H,  // HDR (unsigned float)
    BC7,   // High quality RGBA
    RGBA   // Uncompressed
}

public class ConversionResult {
    public string SourcePath { get; init; } = "";
    public string BackupPath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public bool Success { get; init; }
    public string? Error { get; init; }
}

public static class TextureProcessor {
    private static readonly string[] SupportedExtensions = {
        ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".tga", ".hdr", ".dds"
    };

    public static IEnumerable<string> DiscoverFiles(string rootPath) {
        return Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()));
    }

    public static ConversionResult Convert(string file, DdsFormat format, bool useGpu = false) {
        if (useGpu && GpuHelper.IsGpuAvailable()) {
            return ConvertWithGpuPipeline(file, format);
        } else {
            var ext = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
            var dir = Path.GetDirectoryName(file)!;
            var fileName = Path.GetFileNameWithoutExtension(file);

            var backupDir = Path.Combine(dir, $"{ext}_backup");
            var ddsDir    = Path.Combine(dir, "dds");

            Directory.CreateDirectory(backupDir);
            Directory.CreateDirectory(ddsDir);

            var backupPath = Path.Combine(backupDir, Path.GetFileName(file));
            var outputPath = Path.Combine(ddsDir, $"{fileName}.dds");

            try {
                if (!File.Exists(backupPath)) File.Move(file, backupPath);

                using var image = LoadImage(backupPath, ext);
                var metadata = image.GetMetadata();

                using var converted = ConvertAndCompress(image, metadata, format);
                converted.SaveToDDSFile(DDS_FLAGS.NONE, outputPath);

                return new ConversionResult {
                    SourcePath = file,
                    BackupPath = backupPath,
                    OutputPath = outputPath,
                    Success    = true
                };
            } catch (Exception ex) {
                try {
                    if (!File.Exists(file) && File.Exists(backupPath)) File.Move(backupPath, file);
                } catch {  }

                return new ConversionResult {
                    SourcePath = file,
                    BackupPath = backupPath,
                    OutputPath = outputPath,
                    Success    = false,
                    Error      = ex.Message
                };
            }
        }
    }

    private static ConversionResult ConvertWithGpuPipeline(string file, DdsFormat format) {
        try {
            // For now: fallback to CPU (safe)
            // This is where real GPU logic will go later.

            return Convert(file, format, false);

        } catch (Exception ex) {
            return new ConversionResult {
                SourcePath = file,
                Success = false,
                Error = "[GPU] " + ex.Message
            };
        }
    }

    private static ScratchImage LoadImage(string path, string ext) {
        var helper = TexHelper.Instance;

        if (ext == "dds")
            return helper.LoadFromDDSFile(path, DDS_FLAGS.NONE);

        if (ext == "hdr")
            return helper.LoadFromHDRFile(path);

        if (ext == "tga")
            return helper.LoadFromTGAFile(path);

        return helper.LoadFromWICFile(path, WIC_FLAGS.NONE);
    }

    private static ScratchImage ConvertAndCompress(ScratchImage source, TexMetadata metadata, DdsFormat format) {
        DXGI_FORMAT targetFormat;
        TEX_COMPRESS_FLAGS compFlags = TEX_COMPRESS_FLAGS.DEFAULT;

        switch (format) {
            case DdsFormat.BC1:  targetFormat = DXGI_FORMAT.BC1_UNORM; break;
            case DdsFormat.BC3:  targetFormat = DXGI_FORMAT.BC3_UNORM; break;
            case DdsFormat.BC4:  targetFormat = DXGI_FORMAT.BC4_UNORM; break;
            case DdsFormat.BC5:  targetFormat = DXGI_FORMAT.BC5_UNORM; break;
            case DdsFormat.BC6H: targetFormat = DXGI_FORMAT.BC6H_UF16; break;
            case DdsFormat.BC7:
                targetFormat = DXGI_FORMAT.BC7_UNORM;
                compFlags |= TEX_COMPRESS_FLAGS.BC7_USE_3SUBSETS;
                break;
            case DdsFormat.RGBA:
                targetFormat = DXGI_FORMAT.R8G8B8A8_UNORM;
                return source.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
            default: throw new ArgumentOutOfRangeException(nameof(format));
        }

        ScratchImage working = source;
        
        if (metadata.Format != DXGI_FORMAT.R8G8B8A8_UNORM && metadata.Format != DXGI_FORMAT.R16G16B16A16_FLOAT && metadata.Format != DXGI_FORMAT.R32G32B32A32_FLOAT) {
            working = source.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
        }

        return working.Compress(targetFormat, compFlags, 0.5f);
    }
}