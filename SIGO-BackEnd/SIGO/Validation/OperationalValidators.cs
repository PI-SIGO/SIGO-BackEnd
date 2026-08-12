using FluentValidation;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Validation
{
    public sealed class PedidoValidator : AbstractValidator<PedidoDTO>
    {
        public PedidoValidator()
        {
            RuleFor(request => request.idCliente).GreaterThan(0);
            RuleFor(request => request.idFuncionario).GreaterThan(0);
            RuleFor(request => request.idVeiculo).GreaterThan(0);
            RuleFor(request => request.DescontoReais).GreaterThanOrEqualTo(0);
            RuleFor(request => request.DescontoServicoReais).GreaterThanOrEqualTo(0);
            RuleFor(request => request.descontoPecaReais).GreaterThanOrEqualTo(0);
            RuleFor(request => request.DescontoPorcentagem).InclusiveBetween(0, 100);
            RuleFor(request => request.DescontoServicoPorcentagem).InclusiveBetween(0, 100);
            RuleFor(request => request.DescontoPecaPorcentagem).InclusiveBetween(0, 100);
            RuleFor(request => request.Observacao).MaximumLength(500);
            RuleFor(request => request.DataFim)
                .GreaterThanOrEqualTo(request => request.DataInicio)
                .WithMessage("Data final deve ser igual ou posterior a data inicial.");

            RuleForEach(request => request.Pedido_Pecas).ChildRules(piece =>
            {
                piece.RuleFor(item => item.IdPeca).GreaterThan(0);
                piece.RuleFor(item => item.Quantidade).GreaterThan(0);
                piece.RuleFor(item => item.Estado).NotEmpty().MaximumLength(100);
                piece.RuleFor(item => item.Observacao).MaximumLength(500);
            });

            RuleForEach(request => request.Pedido_Servicos).ChildRules(service =>
            {
                service.RuleFor(item => item.IdServico).GreaterThan(0);
                service.RuleFor(item => item.QuantVezes).GreaterThan(0);
            });

            RuleFor(request => request.Pedido_Pecas)
                .NotNull()
                .Must(items => items is null || items.Select(item => item.IdPeca).Distinct().Count() == items.Count)
                .WithMessage("Uma peca nao pode aparecer mais de uma vez no pedido.");
            RuleFor(request => request.Pedido_Servicos)
                .NotNull()
                .Must(items => items is null || items.Select(item => item.IdServico).Distinct().Count() == items.Count)
                .WithMessage("Um servico nao pode aparecer mais de uma vez no pedido.");

            RuleFor(request => request)
                .Must(request => request.DescontoReais <= 0 || request.DescontoPorcentagem <= 0)
                .WithMessage("Informe o desconto geral em reais ou porcentagem, nunca os dois.");
            RuleFor(request => request)
                .Must(request => request.DescontoServicoReais <= 0 || request.DescontoServicoPorcentagem <= 0)
                .WithMessage("Informe o desconto de servicos em reais ou porcentagem, nunca os dois.");
            RuleFor(request => request)
                .Must(request => request.descontoPecaReais <= 0 || request.DescontoPecaPorcentagem <= 0)
                .WithMessage("Informe o desconto de pecas em reais ou porcentagem, nunca os dois.");
        }
    }

    public sealed class AtualizarStatusRequestValidator : AbstractValidator<AtualizarStatusRequestDTO>
    {
        public AtualizarStatusRequestValidator()
        {
            RuleFor(request => request.Status)
                .NotNull()
                .IsInEnum();
        }
    }

    public sealed class PecaValidator : AbstractValidator<PecaDTO>
    {
        public PecaValidator()
        {
            RuleFor(request => request.Nome).NotEmpty().MaximumLength(100);
            RuleFor(request => request.Tipo).NotEmpty().MaximumLength(100);
            RuleFor(request => request.Descricao).MaximumLength(500);
            RuleFor(request => request.Valor).GreaterThanOrEqualTo(0);
            RuleFor(request => request.Quantidade).GreaterThanOrEqualTo(0);
            RuleFor(request => request.Unidade).GreaterThan(0);
            RuleFor(request => request.IdMarca).GreaterThan(0);
            RuleFor(request => request.Fornecedor).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class ServicoValidator : AbstractValidator<ServicoDTO>
    {
        public ServicoValidator()
        {
            RuleFor(request => request.Nome).NotEmpty().MaximumLength(100);
            RuleFor(request => request.Descricao).NotEmpty().MaximumLength(500);
            RuleFor(request => request.Valor).GreaterThanOrEqualTo(0);
            RuleForEach(request => request.Funcionario_Servicos).ChildRules(employee =>
            {
                employee.RuleFor(item => item.IdFuncionario).GreaterThan(0);
                employee.RuleFor(item => item.TempoDec).NotEmpty().MaximumLength(50);
            });
            RuleFor(request => request.Funcionario_Servicos)
                .NotNull()
                .Must(items => items is null || items.Select(item => item.IdFuncionario).Distinct().Count() == items.Count)
                .WithMessage("Um funcionario nao pode aparecer mais de uma vez no servico.");
        }
    }

}
