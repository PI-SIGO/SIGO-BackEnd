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
                .ForMember(dest => dest.Telefones, opt => opt.MapFrom(src => src.Telefones))
                .ForMember(dest => dest.Veiculos, opt => opt.MapFrom(src => src.Veiculos))
                .ReverseMap();

            CreateMap<Telefone, TelefoneDTO>().ReverseMap();
            CreateMap<MarcaDTO, Marca>().ReverseMap();

            CreateMap<Veiculo, VeiculoDTO>()
                .ForMember(dest => dest.Cores, opt => opt.MapFrom(src => src.Cor))
                .ReverseMap();
            CreateMap<Cor, CorDTO>().ReverseMap();

            CreateMap<Servico, ServicoDTO>().ReverseMap();
            CreateMap<Funcionario_Servico, Funcionario_ServicoDTO>().ReverseMap();
            CreateMap<Funcionario, FuncionarioDTO>().ReverseMap();
            CreateMap<Oficina, OficinaDTO>().ReverseMap();
            CreateMap<Peca, PecaDTO>().ReverseMap();
            CreateMap<Pedido_Peca, Pedido_PecaDTO>().ReverseMap();
            CreateMap<Pedido_Servico, Pedido_ServicoDTO>().ReverseMap();
            CreateMap<Pedido, PedidoDTO>().ReverseMap();
        }
    }
}
