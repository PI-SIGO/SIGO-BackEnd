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

            CreateMap<Telefone, TelefoneDTO>().ReverseMap();
            CreateMap<MarcaDTO, Marca>().ReverseMap();
            CreateMap<VeiculoImagem, VeiculoImagemDTO>();
            CreateMap<PecaSubstituida, PecaSubstituidaDTO>().ReverseMap();
            CreateMap<RegistroServico, RegistroServicoDTO>().ReverseMap();

            CreateMap<Veiculo, VeiculoDTO>()
                .ForMember(dest => dest.Imagens, opt => opt.MapFrom(src => src.Imagens))
                .ForMember(dest => dest.RegistroServicos, opt => opt.MapFrom(src => src.RegistroServicos))
                .ForMember(dest => dest.Pedidos, opt => opt.MapFrom(src => src.Pedidos));

            CreateMap<Servico, ServicoDTO>().ReverseMap();
            CreateMap<Funcionario_Servico, Funcionario_ServicoDTO>().ReverseMap();
            CreateMap<Funcionario, FuncionarioDTO>().ReverseMap();
            CreateMap<FuncionarioRequestDTO, Funcionario>()
                .ForMember(dest => dest.Senha, opt => opt.MapFrom(src => src.Senha));
            CreateMap<Oficina, OficinaDTO>().ReverseMap();
            CreateMap<OficinaRequestDTO, Oficina>()
                .ForMember(dest => dest.Senha, opt => opt.MapFrom(src => src.Senha));
            CreateMap<Peca, PecaDTO>().ReverseMap();
            CreateMap<Pedido_Peca, Pedido_PecaDTO>()
                .ForMember(dest => dest.NomePeca, opt => opt.MapFrom(src => src.Peca != null ? src.Peca.Nome : null));
            CreateMap<Pedido_PecaDTO, Pedido_Peca>();
            CreateMap<Pedido_Servico, Pedido_ServicoDTO>()
                .ForMember(dest => dest.NomeServico, opt => opt.MapFrom(src => src.Servico != null ? src.Servico.Nome : null));
            CreateMap<Pedido_ServicoDTO, Pedido_Servico>();
            CreateMap<Pedido, PedidoDTO>()
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.Nome : null))
                .ForMember(dest => dest.NomeFuncionario, opt => opt.MapFrom(src => src.Funcionario != null ? src.Funcionario.Nome : null))
                .ForMember(dest => dest.NomeOficina, opt => opt.MapFrom(src => src.Oficina != null ? src.Oficina.Nome : null))
                .ForMember(dest => dest.NomeVeiculo, opt => opt.MapFrom(src => src.Veiculo != null ? src.Veiculo.NomeVeiculo : null));
            CreateMap<PedidoDTO, Pedido>();
        }
    }
}
