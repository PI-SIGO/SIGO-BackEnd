using System.Globalization;
using Microsoft.AspNetCore.Http;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class LocalVeiculoImagemStorageService : IVeiculoImagemStorageService
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string> AllowedExtensionsByContentType =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg",
                ["image/png"] = ".png",
                ["image/webp"] = ".webp"
            };

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalVeiculoImagemStorageService> _logger;

        public LocalVeiculoImagemStorageService(
            IWebHostEnvironment environment,
            ILogger<LocalVeiculoImagemStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<VeiculoImagem> SaveAsync(
            int veiculoId,
            IFormFile imagem,
            CancellationToken cancellationToken = default)
        {
            Validate(imagem);

            var contentType = imagem.ContentType.Trim().ToLowerInvariant();
            var extension = ResolveExtension(imagem, contentType);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var relativeDirectory = Path.Combine("veiculos", veiculoId.ToString(CultureInfo.InvariantCulture));
            var absoluteDirectory = Path.Combine(GetStorageRootPath(), relativeDirectory);

            Directory.CreateDirectory(absoluteDirectory);

            var absolutePath = Path.Combine(absoluteDirectory, fileName);
            try
            {
                await using var stream = File.Create(absolutePath);
                await imagem.CopyToAsync(stream, cancellationToken);
            }
            catch
            {
                DeleteFileIfExists(absolutePath);
                throw;
            }

            return new VeiculoImagem
            {
                VeiculoId = veiculoId,
                Url = $"/api/veiculos/{veiculoId.ToString(CultureInfo.InvariantCulture)}/imagens/{fileName}",
                NomeArquivo = fileName,
                NomeOriginal = Path.GetFileName(imagem.FileName),
                ContentType = contentType,
                TamanhoBytes = imagem.Length,
                CriadoEm = DateTime.UtcNow
            };
        }

        public Stream OpenRead(VeiculoImagem imagem)
        {
            var absolutePath = ResolveAbsolutePath(imagem);

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Imagem nao encontrada no armazenamento.", absolutePath);

            return File.OpenRead(absolutePath);
        }

        public void Delete(VeiculoImagem imagem)
        {
            if (imagem == null)
                return;

            var absolutePath = ResolveAbsolutePath(imagem);

            if (!File.Exists(absolutePath))
                return;

            DeleteFileIfExists(absolutePath, imagem);
        }

        private static void Validate(IFormFile imagem)
        {
            var errors = new List<ValidationError>();

            if (imagem == null)
            {
                errors.Add(new ValidationError("imagens", "Envie pelo menos uma imagem."));
                throw new BusinessValidationException(errors);
            }

            if (imagem.Length <= 0)
                errors.Add(new ValidationError("imagens", "A imagem enviada esta vazia."));

            if (imagem.Length > MaxFileSizeBytes)
                errors.Add(new ValidationError("imagens", "Cada imagem deve ter no maximo 5 MB."));

            if (string.IsNullOrWhiteSpace(imagem.ContentType) ||
                !AllowedExtensionsByContentType.ContainsKey(imagem.ContentType))
            {
                errors.Add(new ValidationError("imagens", "Somente imagens JPEG, PNG ou WebP sao permitidas."));
            }
            else if (!HasValidSignature(imagem, imagem.ContentType))
            {
                errors.Add(new ValidationError("imagens", "O conteudo do arquivo nao corresponde a uma imagem valida."));
            }

            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private static string ResolveExtension(IFormFile imagem, string contentType)
        {
            var sourceExtension = Path.GetExtension(imagem.FileName);

            if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) &&
                sourceExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return ".jpeg";
            }

            return AllowedExtensionsByContentType[contentType];
        }

        private static bool HasValidSignature(IFormFile imagem, string contentType)
        {
            Span<byte> header = stackalloc byte[12];
            using var stream = imagem.OpenReadStream();
            var read = stream.Read(header);

            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => IsJpeg(header, read),
                "image/png" => IsPng(header, read),
                "image/webp" => IsWebp(header, read),
                _ => false
            };
        }

        private static bool IsJpeg(ReadOnlySpan<byte> header, int read)
        {
            return read >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;
        }

        private static bool IsPng(ReadOnlySpan<byte> header, int read)
        {
            return read >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A;
        }

        private static bool IsWebp(ReadOnlySpan<byte> header, int read)
        {
            return read >= 12 &&
                header[0] == 0x52 &&
                header[1] == 0x49 &&
                header[2] == 0x46 &&
                header[3] == 0x46 &&
                header[8] == 0x57 &&
                header[9] == 0x45 &&
                header[10] == 0x42 &&
                header[11] == 0x50;
        }

        private string GetStorageRootPath()
        {
            return Path.Combine(_environment.ContentRootPath, "uploads");
        }

        private string ResolveAbsolutePath(VeiculoImagem imagem)
        {
            var rootFullPath = Path.GetFullPath(GetStorageRootPath());
            if (!rootFullPath.EndsWith(Path.DirectorySeparatorChar))
                rootFullPath += Path.DirectorySeparatorChar;

            var absolutePath = Path.GetFullPath(Path.Combine(
                rootFullPath,
                "veiculos",
                imagem.VeiculoId.ToString(CultureInfo.InvariantCulture),
                imagem.NomeArquivo));

            if (!absolutePath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Caminho de imagem invalido.");

            return absolutePath;
        }

        private void DeleteFileIfExists(string absolutePath, VeiculoImagem? imagem = null)
        {
            try
            {
                if (File.Exists(absolutePath))
                    File.Delete(absolutePath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Falha ao remover a imagem {ImagemId} do veiculo {VeiculoId}.", imagem?.Id, imagem?.VeiculoId);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Sem permissao para remover a imagem {ImagemId} do veiculo {VeiculoId}.", imagem?.Id, imagem?.VeiculoId);
            }
        }
    }
}
