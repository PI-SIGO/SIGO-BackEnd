using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using System.Linq;

namespace SIGO.Services.Entities
{
    public class OficinaService : GenericService<Oficina, OficinaDTO>, IOficinaService
    {
        private readonly IOficinaRepository _oficinaRepository;
        private readonly IMapper _mapper;

        public OficinaService(IOficinaRepository oficinaRepository, IMapper mapper)
            : base(oficinaRepository, mapper)
        {
            _oficinaRepository = oficinaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OficinaDTO>> GetByName(string nomeOficina)
        {
            var oficinas = await _oficinaRepository.GetByName(nomeOficina);
            return _mapper.Map<IEnumerable<OficinaDTO>>(oficinas);
        }

        public new async Task Create(OficinaDTO oficinaDTO)
        {
            await ValidarCnpj(oficinaDTO.CNPJ);
            oficinaDTO.CNPJ = SomenteDigitos(oficinaDTO.CNPJ!);
            await base.Create(oficinaDTO);
        }

        public override async Task Update(OficinaDTO oficinaDTO, int id)
        {
            await ValidarCnpj(oficinaDTO.CNPJ, id);
            oficinaDTO.CNPJ = SomenteDigitos(oficinaDTO.CNPJ!);
            await base.Update(oficinaDTO, id);
        }

        public async Task ValidarCnpj(string? cnpj, int? ignoreId = null)
        {
            if (!IsCnpjValido(cnpj))
                throw new ArgumentException("CNPJ inválido.");

            var cnpjNormalizado = SomenteDigitos(cnpj!);
            var existe = await _oficinaRepository.ExistsByCnpj(cnpjNormalizado, ignoreId);
            if (existe)
                throw new ArgumentException("CNPJ já cadastrado.");
        }

        private static bool IsCnpjValido(string? cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            cnpj = SomenteDigitos(cnpj);
            if (cnpj.Length != 14 || TodosCaracteresIguais(cnpj))
                return false;

            var peso1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var peso2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            var soma = 0;
            for (var i = 0; i < 12; i++)
                soma += (cnpj[i] - '0') * peso1[i];

            var resto = soma % 11;
            var dig13 = resto < 2 ? 0 : 11 - resto;
            if (dig13 != (cnpj[12] - '0'))
                return false;

            soma = 0;
            for (var i = 0; i < 13; i++)
                soma += (cnpj[i] - '0') * peso2[i];

            resto = soma % 11;
            var dig14 = resto < 2 ? 0 : 11 - resto;

            return dig14 == (cnpj[13] - '0');
        }

        private static bool TodosCaracteresIguais(string valor) =>
            valor.All(c => c == valor[0]);

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());
    }
}
