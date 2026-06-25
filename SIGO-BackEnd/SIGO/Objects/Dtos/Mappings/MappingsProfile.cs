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
                .ForMember(dest => dest.Veiculos, opt => opt.MapFrom(src => src.Veiculos));
            CreateMap<ClienteDTO, Cliente>();
            CreateMap<ClienteRequestDTO, Cliente>()
                .ForMember(dest => dest.Senha, opt => opt.MapFrom(src => src.senha));

            CreateMap<Telefone, TelefoneDTO>().ReverseMap();
            CreateMap<MarcaDTO, Marca>().ReverseMap();
            CreateMap<VeiculoImagem, VeiculoImagemDTO>();
            CreateMap<PecaSubstituida, PecaSubstituidaDTO>().ReverseMap();
            CreateMap<RegistroServico, RegistroServicoDTO>().ReverseMap();

            CreateMap<Veiculo, VeiculoDTO>()
                .ForMember(dest => dest.Imagens, opt => opt.MapFrom(src => src.Imagens))
                .ForMember(dest => dest.Marcas, opt => opt.MapFrom(src => src.Marcas))
                .ForMember(dest => dest.RegistroServicos, opt => opt.MapFrom(src => src.RegistroServicos))
                .ForMember(dest => dest.Pedidos, opt => opt.MapFrom(src => src.Pedidos));
            CreateMap<VeiculoDTO, Veiculo>()
                .ForMember(dest => dest.Cliente, opt => opt.Ignore())
                .ForMember(dest => dest.Marcas, opt => opt.Ignore())
                .ForMember(dest => dest.Imagens, opt => opt.Ignore())
                .ForMember(dest => dest.RegistroServicos, opt => opt.Ignore())
                .ForMember(dest => dest.Pedidos, opt => opt.Ignore());

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
