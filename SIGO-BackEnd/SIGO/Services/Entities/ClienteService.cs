using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using System.Linq;

namespace SIGO.Services.Entities
{
    public class ClienteService : GenericService<Cliente, ClienteDTO>, IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IMapper _mapper;

        private readonly ITelefoneRepository _telefoneRepository;

        public ClienteService(IClienteRepository clienteRepository, ITelefoneRepository telefoneRepository, IMapper mapper)
            : base(clienteRepository, mapper)
        {
            _clienteRepository = clienteRepository;
            _telefoneRepository = telefoneRepository;
            _mapper = mapper;
        }
        public override async Task<IEnumerable<ClienteDTO>> GetAll()
        {
            var entities = await _clienteRepository.Get();
            return _mapper.Map<IEnumerable<ClienteDTO>>(entities);
        }

        public async Task<ClienteDTO?> GetByIdWithDetails(int id)
        {
            var entity = await _clienteRepository.GetByIdWithDetails(id);
            return _mapper.Map<ClienteDTO?>(entity);
        }

        public async Task<IEnumerable<ClienteDTO>> GetByNameWithDetails(string nome)
        {
            var entities = await _clienteRepository.GetByNameWithDetails(nome);
            return _mapper.Map<IEnumerable<ClienteDTO>>(entities);
        }

        public async Task<ClienteDTO?> GetById(int id)
        {
            var entity = await _clienteRepository.GetById(id);
            return _mapper.Map<ClienteDTO?>(entity);
        }

        public new async Task Create(ClienteDTO clienteDTO)
        {
            await ValidarNomeEmail(clienteDTO.Nome, clienteDTO.Email);
            await ValidarCpfCnpj(clienteDTO.Cpf_Cnpj);
            clienteDTO.Cpf_Cnpj = SomenteDigitos(clienteDTO.Cpf_Cnpj!);

            var cliente = _mapper.Map<Cliente>(clienteDTO);
            await _clienteRepository.Add(cliente);
        }

        public override async Task Update(ClienteDTO clienteDTO, int id)
        {
            var existingCliente = await _clienteRepository.GetById(id);
            if (existingCliente == null)
            {
                throw new KeyNotFoundException($"Cliente com id {id} não encontrado.");
            }

            await ValidarNomeEmail(clienteDTO.Nome, clienteDTO.Email, id);
            await ValidarCpfCnpj(clienteDTO.Cpf_Cnpj, id);
            clienteDTO.Cpf_Cnpj = SomenteDigitos(clienteDTO.Cpf_Cnpj!);

            clienteDTO.Id = id;

            // Atualiza dados do cliente
            var clienteEntity = _mapper.Map<Cliente>(clienteDTO);
            await _clienteRepository.Update(clienteEntity);

            // Sincroniza telefones (atualiza existentes e adiciona novos)
            if (clienteDTO.Telefones != null)
            {
                foreach (var telefoneDto in clienteDTO.Telefones)
                {
                    telefoneDto.ClienteId = id;
                    var telefoneEntity = _mapper.Map<Telefone>(telefoneDto);

                    if (telefoneEntity.Id > 0)
                    {
                        await _telefoneRepository.Update(telefoneEntity);
                    }
                    else
                    {
                        await _telefoneRepository.Add(telefoneEntity);
                    }
                }
            }
        }

        public async Task ValidarCpfCnpj(string? documento, int? ignoreId = null)
        {
            if (!IsCpfOuCnpjValido(documento))
                throw new ArgumentException("CPF/CNPJ inválido.");

            var documentoNormalizado = SomenteDigitos(documento!);
            var existe = await _clienteRepository.ExistsByCpfCnpj(documentoNormalizado, ignoreId);

            if (existe)
                throw new ArgumentException("CPF/CNPJ já cadastrado.");
        }

        public async Task ValidarNomeEmail(string? nome, string? email, int? ignoreId = null)
        {
            if (!string.IsNullOrWhiteSpace(nome))
            {
                var nomeJaExiste = await _clienteRepository.ExistsByNome(nome, ignoreId);
                if (nomeJaExiste)
                    throw new ArgumentException("Já existe cliente cadastrado com este nome.");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailJaExiste = await _clienteRepository.ExistsByEmail(email, ignoreId);
                if (emailJaExiste)
                    throw new ArgumentException("Já existe cliente cadastrado com este e-mail.");
            }
        }

        private static bool IsCpfOuCnpjValido(string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return false;

            var digits = SomenteDigitos(documento);

            return digits.Length switch
            {
                11 => IsCpfValido(digits),
                14 => IsCnpjValido(digits),
                _ => false
            };
        }

        private static bool IsCpfValido(string cpf)
        {
            if (cpf.Length != 11 || TodosCaracteresIguais(cpf))
                return false;

            var soma = 0;
            for (var i = 0; i < 9; i++)
                soma += (cpf[i] - '0') * (10 - i);

            var resto = (soma * 10) % 11;
            if (resto == 10 || resto == 11)
                resto = 0;

            if (resto != (cpf[9] - '0'))
                return false;

            soma = 0;
            for (var i = 0; i < 10; i++)
                soma += (cpf[i] - '0') * (11 - i);

            resto = (soma * 10) % 11;
            if (resto == 10 || resto == 11)
                resto = 0;

            return resto == (cpf[10] - '0');
        }

        private static bool IsCnpjValido(string cnpj)
        {
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

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

        private static bool TodosCaracteresIguais(string valor) =>
            valor.All(c => c == valor[0]);
    }
}
