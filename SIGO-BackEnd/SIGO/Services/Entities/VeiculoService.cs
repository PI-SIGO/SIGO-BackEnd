using AutoMapper;
using Microsoft.AspNetCore.Http;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class VeiculoService : GenericService<Veiculo, VeiculoDTO>, IVeiculoService
    {
        private const int MaxImagesPerRequest = 5;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IMapper _mapper;
        private readonly IClienteRepository? _clienteRepository;
        private readonly IVeiculoImagemStorageService _imagemStorageService;

        public VeiculoService(
            IVeiculoRepository veiculoRepository,
            IMapper mapper,
            IClienteRepository? clienteRepository,
            IVeiculoImagemStorageService imagemStorageService)
            : base(veiculoRepository, mapper)
        {
            _veiculoRepository = veiculoRepository;
            _mapper = mapper;
            _clienteRepository = clienteRepository;
            _imagemStorageService = imagemStorageService;
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByPlaca(string placa)
        {
            var entity = await _veiculoRepository.GetByPlaca(placa);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entity);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByPlacaForCliente(string placa, int clienteId)
        {
            var entity = await _veiculoRepository.GetByPlacaForCliente(placa, clienteId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entity);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByPlacaForOficina(string placa, int oficinaId)
        {
            var entity = await _veiculoRepository.GetByPlacaForOficina(placa, oficinaId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entity);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByTipo(string tipo)
        {
            var entities = await _veiculoRepository.GetByTipo(tipo);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByTipoForCliente(string tipo, int clienteId)
        {
            var entities = await _veiculoRepository.GetByTipoForCliente(tipo, clienteId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByTipoForOficina(string tipo, int oficinaId)
        {
            var entities = await _veiculoRepository.GetByTipoForOficina(tipo, oficinaId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByCliente(int clienteId)
        {
            var entities = await _veiculoRepository.GetByCliente(clienteId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<IEnumerable<VeiculoDTO>> GetByOficina(int oficinaId)
        {
            var entities = await _veiculoRepository.GetByOficina(oficinaId);
            return _mapper.Map<IEnumerable<VeiculoDTO>>(entities);
        }

        public async Task<VeiculoDTO?> GetById(int id)
        {
            var entity = await _veiculoRepository.GetById(id);
            return _mapper.Map<VeiculoDTO?>(entity);
        }

        public async Task<VeiculoDTO?> GetByIdForCliente(int id, int clienteId)
        {
            var entity = await _veiculoRepository.GetByIdForCliente(id, clienteId);
            return _mapper.Map<VeiculoDTO?>(entity);
        }

        public async Task<VeiculoDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var entity = await _veiculoRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<VeiculoDTO?>(entity);
        }

        public async Task CreateForCliente(VeiculoDTO veiculoDto, int clienteId)
        {
            veiculoDto.ClienteId = clienteId;
            await base.Create(veiculoDto);
        }

        public async Task CreateForOficina(VeiculoDTO veiculoDto, int oficinaId)
        {
            await EnsureClienteVinculado(veiculoDto.ClienteId, oficinaId);
            await base.Create(veiculoDto);
        }

        public async Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagens(
            int veiculoId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default)
        {
            var veiculo = await _veiculoRepository.GetByIdWithImagens(veiculoId);
            return await AddImagensToVeiculo(veiculo, veiculoId, imagens, cancellationToken);
        }

        public async Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagensForCliente(
            int veiculoId,
            int clienteId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default)
        {
            var veiculo = await _veiculoRepository.GetByIdForCliente(veiculoId, clienteId);
            return await AddImagensToVeiculo(veiculo, veiculoId, imagens, cancellationToken);
        }

        public async Task RemoveImagem(int veiculoId, int imagemId)
        {
            var veiculo = await _veiculoRepository.GetByIdWithImagens(veiculoId);
            await RemoveImagemFromVeiculo(veiculo, veiculoId, imagemId);
        }

        public async Task<VeiculoImagemArquivoDTO> GetImagemArquivo(int veiculoId, string nomeArquivo)
        {
            var veiculo = await _veiculoRepository.GetByIdWithImagens(veiculoId);
            return OpenImagemFromVeiculo(veiculo, veiculoId, nomeArquivo);
        }

        public async Task<VeiculoImagemArquivoDTO> GetImagemArquivoForCliente(
            int veiculoId,
            int clienteId,
            string nomeArquivo)
        {
            var veiculo = await _veiculoRepository.GetByIdForCliente(veiculoId, clienteId);
            return OpenImagemFromVeiculo(veiculo, veiculoId, nomeArquivo);
        }

        public async Task<VeiculoImagemArquivoDTO> GetImagemArquivoForOficina(
            int veiculoId,
            int oficinaId,
            string nomeArquivo)
        {
            var veiculo = await _veiculoRepository.GetByIdForOficina(veiculoId, oficinaId);
            return OpenImagemFromVeiculo(veiculo, veiculoId, nomeArquivo);
        }

        public async Task RemoveImagemForCliente(int veiculoId, int clienteId, int imagemId)
        {
            var veiculo = await _veiculoRepository.GetByIdForCliente(veiculoId, clienteId);
            await RemoveImagemFromVeiculo(veiculo, veiculoId, imagemId);
        }

        public async Task UpdateVeiculo(VeiculoDTO veiculoDto, int id)
        {
            var existingEntity = await _veiculoRepository.GetById(id);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Veiculo com id {id} nao encontrado.");

            ApplyUpdate(existingEntity, veiculoDto, preserveClienteIdWhenMissing: true);
            await _veiculoRepository.SaveChanges();
        }

        public async Task UpdateVeiculoForCliente(VeiculoDTO veiculoDto, int id, int clienteId)
        {
            var existingEntity = await _veiculoRepository.GetByIdForCliente(id, clienteId);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Veiculo com id {id} nao encontrado.");

            veiculoDto.ClienteId = clienteId;
            ApplyUpdate(existingEntity, veiculoDto, preserveClienteIdWhenMissing: false);
            await _veiculoRepository.SaveChanges();
        }

        public async Task UpdateVeiculoForOficina(VeiculoDTO veiculoDto, int id, int oficinaId)
        {
            var existingEntity = await _veiculoRepository.GetByIdForOficina(id, oficinaId);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Veiculo com id {id} nao encontrado.");

            await EnsureClienteVinculado(veiculoDto.ClienteId, oficinaId);

            ApplyUpdate(existingEntity, veiculoDto, preserveClienteIdWhenMissing: false);
            await _veiculoRepository.SaveChanges();
        }

        private async Task EnsureClienteVinculado(int clienteId, int oficinaId)
        {
            if (_clienteRepository == null)
                return;

            var clienteVinculado = await _clienteRepository.ExistsInOficina(clienteId, oficinaId);
            if (!clienteVinculado)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(nameof(VeiculoDTO.ClienteId), "Cliente nao esta vinculado a oficina autenticada.")
                });
            }
        }

        private async Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagensToVeiculo(
            Veiculo? veiculo,
            int veiculoId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken)
        {
            if (veiculo == null)
                throw new KeyNotFoundException($"Veiculo com id {veiculoId} nao encontrado.");

            ValidateUploadRequest(imagens);

            var imagensSalvas = new List<VeiculoImagem>();

            try
            {
                foreach (var imagem in imagens)
                {
                    var imagemSalva = await _imagemStorageService.SaveAsync(veiculoId, imagem, cancellationToken);
                    imagensSalvas.Add(imagemSalva);
                    veiculo.Imagens.Add(imagemSalva);
                }

                await _veiculoRepository.SaveChanges();
            }
            catch
            {
                foreach (var imagemSalva in imagensSalvas)
                    _imagemStorageService.Delete(imagemSalva);

                throw;
            }

            return _mapper.Map<IReadOnlyCollection<VeiculoImagemDTO>>(imagensSalvas);
        }

        private VeiculoImagemArquivoDTO OpenImagemFromVeiculo(Veiculo? veiculo, int veiculoId, string nomeArquivo)
        {
            if (veiculo == null)
                throw new KeyNotFoundException($"Veiculo com id {veiculoId} nao encontrado.");

            var imagem = veiculo.Imagens.FirstOrDefault(i =>
                i.NomeArquivo.Equals(nomeArquivo, StringComparison.OrdinalIgnoreCase));

            if (imagem == null)
                throw new KeyNotFoundException($"Imagem {nomeArquivo} nao encontrada.");

            return new VeiculoImagemArquivoDTO
            {
                Conteudo = _imagemStorageService.OpenRead(imagem),
                ContentType = imagem.ContentType,
                NomeOriginal = imagem.NomeOriginal
            };
        }

        private async Task RemoveImagemFromVeiculo(Veiculo? veiculo, int veiculoId, int imagemId)
        {
            if (veiculo == null)
                throw new KeyNotFoundException($"Veiculo com id {veiculoId} nao encontrado.");

            var imagem = veiculo.Imagens.FirstOrDefault(i => i.Id == imagemId);
            if (imagem == null)
                throw new KeyNotFoundException($"Imagem com id {imagemId} nao encontrada.");

            veiculo.Imagens.Remove(imagem);
            await _veiculoRepository.SaveChanges();
            _imagemStorageService.Delete(imagem);
        }

        private static void ValidateUploadRequest(IReadOnlyCollection<IFormFile> imagens)
        {
            if (imagens == null || imagens.Count == 0)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError("imagens", "Envie pelo menos uma imagem.")
                });
            }

            if (imagens.Count > MaxImagesPerRequest)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError("imagens", $"Envie no maximo {MaxImagesPerRequest} imagens por requisicao.")
                });
            }
        }

        private static void ApplyUpdate(Veiculo existing, VeiculoDTO veiculoDto, bool preserveClienteIdWhenMissing)
        {
            existing.NomeVeiculo = veiculoDto.NomeVeiculo;
            existing.TipoVeiculo = veiculoDto.TipoVeiculo;
            existing.PlacaVeiculo = veiculoDto.PlacaVeiculo;
            existing.ChassiVeiculo = veiculoDto.ChassiVeiculo;
            existing.AnoFab = veiculoDto.AnoFab;
            existing.Quilometragem = veiculoDto.Quilometragem;
            existing.Combustivel = veiculoDto.Combustivel;
            existing.Seguro = veiculoDto.Seguro;
            existing.Cor = veiculoDto.Cor;

            if (!preserveClienteIdWhenMissing || veiculoDto.ClienteId > 0)
                existing.ClienteId = veiculoDto.ClienteId;
        }
    }
}
