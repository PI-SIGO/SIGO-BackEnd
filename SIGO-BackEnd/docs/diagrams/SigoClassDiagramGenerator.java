import java.awt.geom.Point2D;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import com.change_vision.jude.api.inf.AstahAPI;
import com.change_vision.jude.api.inf.editor.BasicModelEditor;
import com.change_vision.jude.api.inf.editor.ClassDiagramEditor;
import com.change_vision.jude.api.inf.editor.ModelEditorFactory;
import com.change_vision.jude.api.inf.editor.TransactionManager;
import com.change_vision.jude.api.inf.model.IAssociation;
import com.change_vision.jude.api.inf.model.IAttribute;
import com.change_vision.jude.api.inf.model.IClass;
import com.change_vision.jude.api.inf.model.IModel;
import com.change_vision.jude.api.inf.model.IPackage;
import com.change_vision.jude.api.inf.presentation.INodePresentation;
import com.change_vision.jude.api.inf.project.ProjectAccessor;

/** Generates the SIGO domain class diagrams as a native Astah UML project. */
public final class SigoClassDiagramGenerator {
    private static final String ENTITY_COLOR = "#E8F1FF";
    private static final String LINK_COLOR = "#FFF1D6";
    private static final String AUDIT_COLOR = "#F2EAFE";
    private static final String VALUE_COLOR = "#E8F7EE";

    private record AttributeDef(String name, String type, boolean primaryKey) {}
    private record ClassDef(String name, String color, List<AttributeDef> attributes) {}
    private record AssociationDef(
        String left,
        String right,
        String leftRole,
        String rightRole,
        String leftMultiplicity,
        String rightMultiplicity,
        String name) {}
    private record Position(double x, double y) {}

    private static AttributeDef field(String name, String type) {
        return new AttributeDef(name, type, false);
    }

    private static AttributeDef key(String name, String type) {
        return new AttributeDef(name, type, true);
    }

    private static ClassDef entity(String name, AttributeDef... attributes) {
        return new ClassDef(name, ENTITY_COLOR, List.of(attributes));
    }

    private static ClassDef link(String name, AttributeDef... attributes) {
        return new ClassDef(name, LINK_COLOR, List.of(attributes));
    }

    private static ClassDef audit(String name, AttributeDef... attributes) {
        return new ClassDef(name, AUDIT_COLOR, List.of(attributes));
    }

    private static ClassDef value(String name, AttributeDef... attributes) {
        return new ClassDef(name, VALUE_COLOR, List.of(attributes));
    }

    private static final List<ClassDef> CLASSES = List.of(
        entity("Cliente",
            key("Id", "int"), field("Nome", "string"), field("Email", "string"),
            field("Senha", "string"), field("Cpf_Cnpj", "string"), field("Obs", "string"),
            field("Razao", "string"), field("DataNasc", "DateOnly?"), field("Sexo", "Sexo"),
            field("Numero", "int"), field("Rua", "string"), field("Cidade", "string"),
            field("Cep", "string"), field("Bairro", "string"), field("Estado", "string"),
            field("Pais", "string"), field("Complemento", "string"),
            field("TipoCliente", "TipoCliente"), field("Situacao", "Situacao")),
        value("ClienteConta",
            key("Id", "int"), field("ClienteId", "int"), field("EmailNormalizado", "string"),
            field("PasswordHash", "string"), field("Status", "EstadoClienteConta"),
            field("TokenVersion", "int"), field("CreatedAt", "DateTime"), field("UpdatedAt", "DateTime")),
        value("ClienteContato",
            key("Id", "int"), field("ClienteId", "int"), field("Tipo", "TipoContatoCliente"),
            field("ValorNormalizado", "string"), field("Origem", "OrigemContatoCliente"),
            field("VerificadoEm", "DateTime?"), field("CreatedAt", "DateTime")),
        link("ClienteOficina",
            key("OficinaId", "int"), key("ClienteId", "int"), field("Ativo", "bool"),
            field("CreatedAt", "DateTime"), field("UpdatedAt", "DateTime"), field("RevogadoEm", "DateTime?")),
        audit("AuditoriaSeguranca",
            key("Id", "long"), field("ClienteId", "int?"), field("TipoAtor", "TipoAtorAuditoria"),
            field("AtorId", "int?"), field("Evento", "TipoEventoAuditoria"),
            field("Resultado", "ResultadoAuditoria"), field("DocumentoHash", "string?"),
            field("ContatoHash", "string?"), field("DocumentoMascarado", "string?"),
            field("ContatoMascarado", "string?"), field("IpAddress", "string?"),
            field("CorrelationId", "string?"), field("CreatedAt", "DateTime")),
        entity("Oficina",
            key("Id", "int"), field("Nome", "string"), field("CNPJ", "string"),
            field("Email", "string"), field("Numero", "int"), field("Rua", "string"),
            field("Cidade", "string"), field("Cep", "int"), field("Bairro", "string"),
            field("Estado", "string"), field("Pais", "string"), field("Complemento", "string"),
            field("Senha", "string"), field("Situacao", "Situacao")),
        entity("Funcionario",
            key("Id", "int"), field("Nome", "string"), field("Cpf", "string"),
            field("Cargo", "string"), field("Email", "string"), field("Senha", "string"),
            field("Role", "string"), field("Situacao", "Situacao"), field("IdOficina", "int?")),
        value("Telefone",
            key("Id", "int"), field("Numero", "string"), field("DDD", "int"), field("ClienteId", "int")),
        entity("Veiculo",
            key("Id", "int"), field("NomeVeiculo", "string"), field("TipoVeiculo", "string"),
            field("PlacaVeiculo", "string"), field("ChassiVeiculo", "string"), field("AnoFab", "int"),
            field("Quilometragem", "int"), field("Combustivel", "string"), field("Seguro", "string"),
            field("Cor", "string"), field("Status", "Status"), field("ClienteId", "int")),
        value("VeiculoImagem",
            key("Id", "int"), field("VeiculoId", "int"), field("Url", "string"),
            field("NomeArquivo", "string"), field("NomeOriginal", "string"),
            field("ContentType", "string"), field("TamanhoBytes", "long"), field("CriadoEm", "DateTime")),
        entity("Marca",
            key("Id", "int"), field("Nome", "string"), field("Desc", "string"), field("TipoMarca", "string")),
        entity("Peca",
            key("Id", "int"), field("Nome", "string"), field("Tipo", "string"),
            field("Descricao", "string"), field("Valor", "decimal"), field("Quantidade", "int"),
            field("Garantia", "DateOnly"), field("Unidade", "int"), field("IdMarca", "int"),
            field("DataAquisicao", "DateOnly"), field("Fornecedor", "string"), field("IdOficina", "int?")),
        entity("Servico",
            key("Id", "int"), field("Nome", "string"), field("Descricao", "string"),
            field("Valor", "decimal"), field("Garantia", "DateOnly"), field("IdOficina", "int?")),
        link("Funcionario_Servico",
            key("IdFuncionario", "int"), key("IdServico", "int"), field("TempoDec", "string")),
        entity("Pedido",
            key("Id", "int"), field("idCliente", "int"), field("idFuncionario", "int"),
            field("idOficina", "int"), field("idVeiculo", "int"), field("ValorTotal", "decimal"),
            field("DescontoReais", "decimal"), field("DescontoPorcentagem", "decimal"),
            field("DescontoTotalReais", "decimal"), field("DescontoServicoPorcentagem", "decimal"),
            field("DescontoServicoReais", "decimal"), field("DescontoPecaPorcentagem", "decimal"),
            field("descontoPecaReais", "decimal"), field("Observacao", "string"),
            field("DataInicio", "DateOnly"), field("DataFim", "DateOnly")),
        link("Pedido_Peca",
            key("IdPedido", "int"), key("IdPeca", "int"), field("Quantidade", "int"),
            field("ValorUnitario", "decimal"), field("DataInstalacao", "DateOnly"),
            field("Estado", "string"), field("Observacao", "string")),
        link("Pedido_Servico",
            key("IdPedido", "int"), key("IdServico", "int"), field("QuantVezes", "int"),
            field("ValorUnitario", "decimal")),
        entity("RegistroServico",
            key("Id", "int"), field("VeiculoId", "int"), field("OficinaId", "int"),
            field("ServicoId", "int?"), field("DataServico", "DateTime"),
            field("Descricao", "string"), field("Quilometragem", "int"), field("Responsavel", "string")),
        value("PecaSubstituida",
            key("Id", "int"), field("RegistroServicoId", "int"), field("Nome", "string"),
            field("Quantidade", "int"), field("Observacao", "string"))
    );

    private static final List<AssociationDef> ASSOCIATIONS = List.of(
        new AssociationDef("Cliente", "ClienteConta", "cliente", "conta", "1", "0..1", "possui"),
        new AssociationDef("Cliente", "ClienteContato", "cliente", "contatos", "1", "0..*", "possui"),
        new AssociationDef("Cliente", "AuditoriaSeguranca", "cliente", "auditorias", "0..1", "0..*", "registra"),
        new AssociationDef("Cliente", "Telefone", "cliente", "telefones", "1", "0..*", "possui"),
        new AssociationDef("Cliente", "Veiculo", "cliente", "veiculos", "1", "0..*", "possui"),
        new AssociationDef("Cliente", "ClienteOficina", "cliente", "vinculos", "1", "0..*", "vincula"),
        new AssociationDef("Oficina", "ClienteOficina", "oficina", "clientes", "1", "0..*", "vincula"),
        new AssociationDef("Oficina", "Funcionario", "oficina", "funcionarios", "0..1", "0..*", "emprega"),
        new AssociationDef("Oficina", "Telefone", "oficina", "telefones", "0..1", "0..*", "possui"),
        new AssociationDef("Funcionario", "Telefone", "funcionario", "telefones", "0..1", "0..*", "possui"),
        new AssociationDef("Oficina", "Peca", "oficina", "pecas", "0..1", "0..*", "mantem"),
        new AssociationDef("Marca", "Peca", "marca", "pecas", "1", "0..*", "classifica"),
        new AssociationDef("Oficina", "Servico", "oficina", "servicos", "0..1", "0..*", "oferece"),
        new AssociationDef("Funcionario", "Funcionario_Servico", "funcionario", "habilitacoes", "1", "0..*", "executa"),
        new AssociationDef("Servico", "Funcionario_Servico", "servico", "funcionarios", "1", "0..*", "habilita"),
        new AssociationDef("Cliente", "Pedido", "cliente", "pedidos", "1", "0..*", "solicita"),
        new AssociationDef("Funcionario", "Pedido", "funcionario", "pedidos", "1", "0..*", "atende"),
        new AssociationDef("Oficina", "Pedido", "oficina", "pedidos", "1", "0..*", "recebe"),
        new AssociationDef("ClienteOficina", "Pedido", "vinculo", "pedidos", "1", "0..*", "autoriza"),
        new AssociationDef("Veiculo", "Pedido", "veiculo", "pedidos", "1", "0..*", "origina"),
        new AssociationDef("Pedido", "Pedido_Peca", "pedido", "itensPeca", "1", "0..*", "contem"),
        new AssociationDef("Peca", "Pedido_Peca", "peca", "pedidos", "1", "0..*", "referencia"),
        new AssociationDef("Pedido", "Pedido_Servico", "pedido", "itensServico", "1", "0..*", "contem"),
        new AssociationDef("Servico", "Pedido_Servico", "servico", "pedidos", "1", "0..*", "referencia"),
        new AssociationDef("Veiculo", "VeiculoImagem", "veiculo", "imagens", "1", "0..*", "possui"),
        new AssociationDef("Veiculo", "RegistroServico", "veiculo", "historico", "1", "0..*", "possui"),
        new AssociationDef("Oficina", "RegistroServico", "oficina", "registros", "1", "0..*", "registra"),
        new AssociationDef("Servico", "RegistroServico", "servico", "registros", "0..1", "0..*", "documenta"),
        new AssociationDef("RegistroServico", "PecaSubstituida", "registro", "pecasSubstituidas", "1", "0..*", "detalha")
    );

    private SigoClassDiagramGenerator() {}

    public static void main(String[] args) throws Exception {
        if (args.length != 1) {
            throw new IllegalArgumentException("Usage: SigoClassDiagramGenerator <output.asta>");
        }

        Path output = Path.of(args[0]).toAbsolutePath().normalize();
        Files.createDirectories(output.getParent());
        Files.deleteIfExists(output);

        ProjectAccessor accessor = AstahAPI.getAstahAPI().getProjectAccessor();
        try {
            accessor.create(output.toString());
            IModel project = accessor.getProject();

            TransactionManager.beginTransaction();
            BasicModelEditor modelEditor = ModelEditorFactory.getBasicModelEditor();
            modelEditor.setLanguageCSharp(project, true);
            IPackage domain = modelEditor.createPackage(project, "SIGO.Objects.Models");

            Map<String, IClass> classes = createClasses(modelEditor, domain);
            Map<AssociationDef, IAssociation> associations = createAssociations(modelEditor, classes);

            createDiagram(accessor, domain, "01 - Visao Geral do Dominio", classes, associations, overviewPositions());
            createDiagram(accessor, domain, "02 - Clientes e Acesso", classes, associations, clientPositions());
            createDiagram(accessor, domain, "03 - Oficina e Catalogo", classes, associations, workshopPositions());
            createDiagram(accessor, domain, "04 - Pedidos e Historico", classes, associations, orderPositions());

            TransactionManager.endTransaction();
            accessor.save();
            System.out.println("Astah project generated: " + output);
        } catch (Throwable error) {
            if (TransactionManager.isInTransaction()) {
                TransactionManager.abortTransaction();
            }
            throw error;
        } finally {
            accessor.close();
        }
    }

    private static Map<String, IClass> createClasses(BasicModelEditor editor, IPackage domain) throws Exception {
        Map<String, IClass> result = new LinkedHashMap<>();
        for (ClassDef definition : CLASSES) {
            IClass modelClass = editor.createClass(domain, definition.name());
            modelClass.setDefinition("Entidade persistida do dominio SIGO.");
            for (AttributeDef attributeDefinition : definition.attributes()) {
                IAttribute attribute = editor.createAttribute(
                    modelClass,
                    attributeDefinition.name(),
                    attributeDefinition.type());
                attribute.setVisibility("private");
                if (attributeDefinition.primaryKey()) {
                    attribute.addStereotype("PK");
                }
            }
            result.put(definition.name(), modelClass);
        }
        return result;
    }

    private static Map<AssociationDef, IAssociation> createAssociations(
        BasicModelEditor editor,
        Map<String, IClass> classes) throws Exception {
        Map<AssociationDef, IAssociation> result = new LinkedHashMap<>();
        for (AssociationDef definition : ASSOCIATIONS) {
            IAssociation association = editor.createAssociation(
                classes.get(definition.left()),
                classes.get(definition.right()),
                definition.name(),
                definition.leftRole(),
                definition.rightRole());
            IAttribute[] ends = association.getMemberEnds();
            ends[0].setMultiplicityString(definition.leftMultiplicity());
            ends[1].setMultiplicityString(definition.rightMultiplicity());
            result.put(definition, association);
        }
        return result;
    }

    private static void createDiagram(
        ProjectAccessor accessor,
        IPackage domain,
        String name,
        Map<String, IClass> classes,
        Map<AssociationDef, IAssociation> associations,
        Map<String, Position> positions) throws Exception {
        ClassDiagramEditor editor = accessor.getDiagramEditorFactory().getClassDiagramEditor();
        editor.createClassDiagram(domain, name);
        Map<String, INodePresentation> nodes = new LinkedHashMap<>();

        for (Map.Entry<String, Position> entry : positions.entrySet()) {
            Position position = entry.getValue();
            INodePresentation node = editor.createNodePresentation(
                classes.get(entry.getKey()),
                new Point2D.Double(position.x(), position.y()));
            ClassDef definition = CLASSES.stream()
                .filter(candidate -> candidate.name().equals(entry.getKey()))
                .findFirst()
                .orElseThrow();
            node.setProperty("fill.color", definition.color());
            node.setWidth(260.0d);
            nodes.put(entry.getKey(), node);
        }

        for (Map.Entry<AssociationDef, IAssociation> entry : associations.entrySet()) {
            AssociationDef definition = entry.getKey();
            INodePresentation left = nodes.get(definition.left());
            INodePresentation right = nodes.get(definition.right());
            if (left != null && right != null) {
                editor.createLinkPresentation(entry.getValue(), left, right);
            }
        }
    }

    private static Map<String, Position> overviewPositions() {
        return positions(
            "Cliente", 40, 40, "ClienteConta", 390, 40, "ClienteContato", 740, 40,
            "AuditoriaSeguranca", 1090, 40, "Telefone", 1440, 40,
            "ClienteOficina", 40, 570, "Oficina", 390, 570, "Funcionario", 740, 570,
            "Funcionario_Servico", 1090, 570, "Servico", 1440, 570,
            "Marca", 40, 1050, "Peca", 390, 1050, "Pedido_Peca", 740, 1050,
            "Pedido", 1090, 1050, "Pedido_Servico", 1440, 1050,
            "Veiculo", 40, 1580, "VeiculoImagem", 390, 1580,
            "RegistroServico", 740, 1580, "PecaSubstituida", 1090, 1580);
    }

    private static Map<String, Position> clientPositions() {
        return positions(
            "Cliente", 420, 280, "ClienteConta", 40, 40, "ClienteContato", 420, 40,
            "AuditoriaSeguranca", 800, 40, "Telefone", 40, 620,
            "ClienteOficina", 420, 760, "Oficina", 800, 760, "Veiculo", 800, 320,
            "VeiculoImagem", 1160, 320);
    }

    private static Map<String, Position> workshopPositions() {
        return positions(
            "Oficina", 420, 280, "Funcionario", 40, 40, "Telefone", 420, 40,
            "ClienteOficina", 800, 40, "Funcionario_Servico", 40, 620,
            "Servico", 420, 620, "Peca", 800, 620, "Marca", 1160, 620);
    }

    private static Map<String, Position> orderPositions() {
        return positions(
            "Cliente", 40, 40, "ClienteOficina", 380, 40, "Oficina", 720, 40,
            "Funcionario", 1060, 40, "Veiculo", 1400, 40,
            "Pedido", 720, 520, "Pedido_Peca", 300, 940, "Peca", 40, 1240,
            "Pedido_Servico", 1120, 940, "Servico", 1460, 1240,
            "RegistroServico", 1400, 520, "PecaSubstituida", 1400, 900,
            "VeiculoImagem", 1760, 40);
    }

    private static Map<String, Position> positions(Object... values) {
        Map<String, Position> result = new LinkedHashMap<>();
        for (int index = 0; index < values.length; index += 3) {
            result.put(
                (String) values[index],
                new Position(((Number) values[index + 1]).doubleValue(), ((Number) values[index + 2]).doubleValue()));
        }
        return result;
    }
}
