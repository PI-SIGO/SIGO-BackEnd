using AutoMapper;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Objects.Dtos.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Cliente, ClienteDTO>()
                .ForMember(dest => dest.Telefones, opt => opt.MapFrom(src => src.Telefones));
            CreateMap<ClienteDTO, Cliente>();
            CreateMap<ClienteRequestDTO, Cliente>()
                .ForMember(dest => dest.Senha, opt => opt.MapFrom(src => src.senha));

            CreateMap<Telefone, TelefoneDTO>().ReverseMap();
            CreateMap<MarcaDTO, Marca>().ReverseMap();

            CreateMap<VeiculoDTO, Veiculo>()
                .ForMember(dest => dest.Cliente, opt => opt.Ignore())
                .ForMember(dest => dest.Marcas, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<Servico, ServicoDTO>().ReverseMap();
            CreateMap<Funcionario_Servico, Funcionario_ServicoDTO>().ReverseMap();
            CreateMap<Funcionario, FuncionarioDTO>().ReverseMap();
            CreateMap<FuncionarioRequestDTO, Funcionario>()
                .ForMember(dest => dest.Senha, opt => opt.MapFrom(src => src.Senha));
            CreateMap<Oficina, OficinaDTO>().ReverseMap();
            CreateMap<OficinaRequestDTO, Oficina>()
                .ForMember(dest => dest.Senha, opt => opt.MapFrom(src => src.Senha));
            CreateMap<Peca, PecaDTO>().ReverseMap();
            CreateMap<Pedido_Peca, Pedido_PecaDTO>().ReverseMap();
            CreateMap<Pedido_Servico, Pedido_ServicoDTO>().ReverseMap();
            CreateMap<Pedido, PedidoDTO>().ReverseMap();
        }
    }
}
