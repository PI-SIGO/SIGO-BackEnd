import java.awt.geom.Point2D;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Map;

import com.change_vision.jude.api.inf.AstahAPI;
import com.change_vision.jude.api.inf.editor.BasicModelEditor;
import com.change_vision.jude.api.inf.editor.ModelEditorFactory;
import com.change_vision.jude.api.inf.editor.TransactionManager;
import com.change_vision.jude.api.inf.editor.UseCaseDiagramEditor;
import com.change_vision.jude.api.inf.editor.UseCaseModelEditor;
import com.change_vision.jude.api.inf.model.IAssociation;
import com.change_vision.jude.api.inf.model.IClass;
import com.change_vision.jude.api.inf.model.IGeneralization;
import com.change_vision.jude.api.inf.model.IModel;
import com.change_vision.jude.api.inf.model.IPackage;
import com.change_vision.jude.api.inf.model.IUseCase;
import com.change_vision.jude.api.inf.presentation.INodePresentation;
import com.change_vision.jude.api.inf.project.ProjectAccessor;

/** Generates the SIGO use-case diagram as a native Astah UML project. */
public final class SigoUseCaseDiagramGenerator {
    private static final String ACTOR_COLOR = "#E8F1FF";
    private static final String USE_CASE_COLOR = "#E8F7EE";
    private static final String BOUNDARY_COLOR = "#F8FAFC";

    private SigoUseCaseDiagramGenerator() {}

    public static void main(String[] args) throws Exception {
        if (args.length != 1) {
            throw new IllegalArgumentException("Usage: SigoUseCaseDiagramGenerator <output.asta>");
        }

        Path output = Path.of(args[0]).toAbsolutePath().normalize();
        Files.createDirectories(output.getParent());
        Files.deleteIfExists(output);

        ProjectAccessor accessor = AstahAPI.getAstahAPI().getProjectAccessor();
        try {
            accessor.create(output.toString());
            IModel project = accessor.getProject();

            TransactionManager.beginTransaction();
            BasicModelEditor basic = ModelEditorFactory.getBasicModelEditor();
            UseCaseModelEditor model = ModelEditorFactory.getUseCaseModelEditor();
            IPackage scope = basic.createPackage(project, "SIGO - Casos de Uso");

            Map<String, IClass> actors = createActors(model, scope);
            Map<String, IUseCase> useCases = createUseCases(model, scope);
            Map<String, IAssociation> associations = createAssociations(basic, actors, useCases);
            Map<String, IGeneralization> generalizations = createGeneralizations(basic, actors);

            createDiagram(accessor, scope, actors, useCases, associations, generalizations);

            TransactionManager.endTransaction();
            accessor.save();
            System.out.println("Astah use-case project generated: " + output);
        } catch (Throwable error) {
            if (TransactionManager.isInTransaction()) {
                TransactionManager.abortTransaction();
            }
            throw error;
        } finally {
            accessor.close();
        }
    }

    private static Map<String, IClass> createActors(UseCaseModelEditor editor, IPackage scope) throws Exception {
        Map<String, IClass> actors = new LinkedHashMap<>();
        addActor(editor, scope, actors, "usuario", "Usuário do SIGO");
        addActor(editor, scope, actors, "cliente", "Cliente");
        addActor(editor, scope, actors, "operador", "Operador da Oficina");
        addActor(editor, scope, actors, "funcionario", "Funcionário");
        addActor(editor, scope, actors, "gestor", "Gestor da Oficina");
        addActor(editor, scope, actors, "administrador", "Administrador");
        addActor(editor, scope, actors, "oficina", "Oficina");
        addActor(editor, scope, actors, "viacep", "ViaCEP");
        return actors;
    }

    private static void addActor(
        UseCaseModelEditor editor,
        IPackage scope,
        Map<String, IClass> actors,
        String key,
        String name) throws Exception {
        actors.put(key, editor.createActor(scope, name));
    }

    private static Map<String, IUseCase> createUseCases(UseCaseModelEditor editor, IPackage scope) throws Exception {
        Map<String, IUseCase> useCases = new LinkedHashMap<>();
        addUseCase(editor, scope, useCases, "autenticar", "Autenticar-se");
        addUseCase(editor, scope, useCases, "cep", "Consultar endereço por CEP");
        addUseCase(editor, scope, useCases, "cadastrar", "Cadastrar-se como cliente");
        addUseCase(editor, scope, useCases, "perfil", "Gerenciar perfil e telefones");
        addUseCase(editor, scope, useCases, "meusVeiculos", "Gerenciar próprios veículos");
        addUseCase(editor, scope, useCases, "meusPedidos", "Consultar pedidos e histórico");
        addUseCase(editor, scope, useCases, "clientes", "Gerenciar clientes e vínculos");
        addUseCase(editor, scope, useCases, "veiculos", "Gerenciar veículos");
        addUseCase(editor, scope, useCases, "catalogo", "Gerenciar catálogo de peças e serviços");
        addUseCase(editor, scope, useCases, "pedidos", "Gerenciar pedidos");
        addUseCase(editor, scope, useCases, "equipe", "Gerenciar equipe, oficina e marcas");
        addUseCase(editor, scope, useCases, "relatorio", "Emitir relatório do veículo");
        return useCases;
    }

    private static void addUseCase(
        UseCaseModelEditor editor,
        IPackage scope,
        Map<String, IUseCase> useCases,
        String key,
        String name) throws Exception {
        useCases.put(key, editor.createUseCase(scope, name));
    }

    private static Map<String, IAssociation> createAssociations(
        BasicModelEditor editor,
        Map<String, IClass> actors,
        Map<String, IUseCase> useCases) throws Exception {
        Map<String, IAssociation> result = new LinkedHashMap<>();

        associate(editor, result, actors, useCases, "usuario", "autenticar");
        associate(editor, result, actors, useCases, "usuario", "cep");

        associate(editor, result, actors, useCases, "cliente", "cadastrar");
        associate(editor, result, actors, useCases, "cliente", "perfil");
        associate(editor, result, actors, useCases, "cliente", "meusVeiculos");
        associate(editor, result, actors, useCases, "cliente", "meusPedidos");
        associate(editor, result, actors, useCases, "cliente", "relatorio");

        associate(editor, result, actors, useCases, "operador", "clientes");
        associate(editor, result, actors, useCases, "operador", "veiculos");
        associate(editor, result, actors, useCases, "operador", "catalogo");
        associate(editor, result, actors, useCases, "operador", "relatorio");

        associate(editor, result, actors, useCases, "gestor", "pedidos");
        associate(editor, result, actors, useCases, "gestor", "equipe");

        associate(editor, result, actors, useCases, "viacep", "cep");
        return result;
    }

    private static void associate(
        BasicModelEditor editor,
        Map<String, IAssociation> associations,
        Map<String, IClass> actors,
        Map<String, IUseCase> useCases,
        String actor,
        String useCase) throws Exception {
        String key = actor + ":" + useCase;
        associations.put(key, editor.createAssociation(actors.get(actor), useCases.get(useCase), "", "", ""));
    }

    private static Map<String, IGeneralization> createGeneralizations(
        BasicModelEditor editor,
        Map<String, IClass> actors) throws Exception {
        Map<String, IGeneralization> result = new LinkedHashMap<>();
        generalize(editor, result, actors, "cliente", "usuario");
        generalize(editor, result, actors, "operador", "usuario");
        generalize(editor, result, actors, "funcionario", "operador");
        generalize(editor, result, actors, "gestor", "operador");
        generalize(editor, result, actors, "administrador", "gestor");
        generalize(editor, result, actors, "oficina", "gestor");
        return result;
    }

    private static void generalize(
        BasicModelEditor editor,
        Map<String, IGeneralization> generalizations,
        Map<String, IClass> actors,
        String child,
        String parent) throws Exception {
        String key = child + ":" + parent;
        generalizations.put(key, editor.createGeneralization(actors.get(child), actors.get(parent), ""));
    }

    private static void createDiagram(
        ProjectAccessor accessor,
        IPackage scope,
        Map<String, IClass> actors,
        Map<String, IUseCase> useCases,
        Map<String, IAssociation> associations,
        Map<String, IGeneralization> generalizations) throws Exception {
        UseCaseDiagramEditor editor = accessor.getDiagramEditorFactory().getUseCaseDiagramEditor();
        editor.createUseCaseDiagram(scope, "01 - Casos de Uso do SIGO");

        INodePresentation boundary = editor.createRect(point(330, 40), 1050, 1300);
        boundary.setProperty("fill.color", BOUNDARY_COLOR);
        boundary.setProperty("line.color", "#475569");

        INodePresentation title = editor.createText("SIGO — Sistema de Gestão de Oficina", point(650, 65));
        title.setProperty("font.color", "#0F172A");

        Map<String, INodePresentation> actorNodes = new LinkedHashMap<>();
        addNode(editor, actorNodes, "usuario", actors.get("usuario"), 40, 80, 165, ACTOR_COLOR);
        addNode(editor, actorNodes, "cliente", actors.get("cliente"), 40, 300, 140, ACTOR_COLOR);
        addNode(editor, actorNodes, "operador", actors.get("operador"), 40, 560, 175, ACTOR_COLOR);
        addNode(editor, actorNodes, "funcionario", actors.get("funcionario"), 20, 790, 140, ACTOR_COLOR);
        addNode(editor, actorNodes, "gestor", actors.get("gestor"), 175, 790, 165, ACTOR_COLOR);
        addNode(editor, actorNodes, "administrador", actors.get("administrador"), 135, 1040, 150, ACTOR_COLOR);
        addNode(editor, actorNodes, "oficina", actors.get("oficina"), 210, 1210, 120, ACTOR_COLOR);
        addNode(editor, actorNodes, "viacep", actors.get("viacep"), 1435, 170, 120, "#FFF1D6");

        Map<String, INodePresentation> useCaseNodes = new LinkedHashMap<>();
        addNode(editor, useCaseNodes, "autenticar", useCases.get("autenticar"), 450, 150, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "cep", useCases.get("cep"), 950, 150, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "cadastrar", useCases.get("cadastrar"), 450, 350, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "perfil", useCases.get("perfil"), 950, 350, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "meusVeiculos", useCases.get("meusVeiculos"), 450, 550, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "meusPedidos", useCases.get("meusPedidos"), 950, 550, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "clientes", useCases.get("clientes"), 450, 750, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "veiculos", useCases.get("veiculos"), 950, 750, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "catalogo", useCases.get("catalogo"), 450, 950, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "pedidos", useCases.get("pedidos"), 950, 950, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "equipe", useCases.get("equipe"), 450, 1150, 300, USE_CASE_COLOR);
        addNode(editor, useCaseNodes, "relatorio", useCases.get("relatorio"), 950, 1150, 300, USE_CASE_COLOR);

        for (Map.Entry<String, IAssociation> entry : associations.entrySet()) {
            String[] keys = entry.getKey().split(":", 2);
            editor.createLinkPresentation(entry.getValue(), actorNodes.get(keys[0]), useCaseNodes.get(keys[1]));
        }

        for (Map.Entry<String, IGeneralization> entry : generalizations.entrySet()) {
            String[] keys = entry.getKey().split(":", 2);
            editor.createLinkPresentation(entry.getValue(), actorNodes.get(keys[0]), actorNodes.get(keys[1]));
        }
    }

    private static void addNode(
        UseCaseDiagramEditor editor,
        Map<String, INodePresentation> nodes,
        String key,
        com.change_vision.jude.api.inf.model.IElement element,
        double x,
        double y,
        double width,
        String color) throws Exception {
        INodePresentation node = editor.createNodePresentation(element, point(x, y));
        node.setWidth(width);
        node.setProperty("fill.color", color);
        nodes.put(key, node);
    }

    private static Point2D point(double x, double y) {
        return new Point2D.Double(x, y);
    }
}
