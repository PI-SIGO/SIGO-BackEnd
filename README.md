# SIGO - BackEnd – Sistema de Informatização e Gerenciamento de Oficinas
----

O SIGO é um sistema web em desenvolvimento criado para modernizar e automatizar os processos internos de oficinas mecânicas. O sistema centraliza cadastros, ordens de serviço, veículos, estoque, funcionários e relatórios, oferecendo também um portal para que clientes acompanhem o status e o histórico do veículo.

----

## Objetivo do Sistema
----

O sistema foi desenvolvido com o propósito de:

- Centralizar informações e cadastros em uma única plataforma.
- Automatizar ordens de serviço, estoque e atendimentos.
- Aumentar a eficiência e reduzir erros operacionais.
- Melhorar a comunicação entre oficina e cliente.
- Permitir que o cliente visualize histórico e status dos serviços.
- Gerar relatórios detalhados para tomada de decisões.

----

## Principais Funcionalidades
----

### Gestão de Clientes
- Cadastro, edição, exclusão e listagem.
- Suporte a pessoa física e jurídica.
- Endereços e telefones associados.

### Gestão de Veículos
- Cadastro completo de veículos.
- Associação com clientes.
- Relatórios detalhados.

### Gestão de Funcionários
- Controle de funcionários, cargos e permissões.
- Associação de funcionários a ordens de serviço.

### Ordens de Serviço (Pedidos)
- Registro de serviços realizados.
- Inclusão de peças e quantidade utilizadas.
- Cálculo automático de valores e descontos.
- Controle de status do pedido.

### Estoque de Peças
- Cadastro e controle de peças.
- Garantia, fornecedor e quantidade.
- Associação a ordens de serviço.

### Serviços
- Cadastro de serviços com valor, descrição e garantia.

### Relatórios
- Relatório de pedidos.
- Relatório de veículos.
- Relatório de serviços.

### Portal do Cliente
- Acesso a histórico do veículo.
- Acompanhamento do status do atendimento.
- Geração de relatórios.

----

## Arquitetura do Sistema
----

Documentação já desenvolvida:

- Diagramas UML:
  - Diagrama de Classes
  - Diagramas de Caso de Uso (gerais e individuais)
  - Diagramas de Sequência

- Banco de Dados:
  - Modelo Entidade-Relacionamento
  - Scripts SQL
  - Mapeamento ORM

----

## Tecnologias Previstas
----

### [Front-end](https://github.com/PI-SIGO/SIGO-FrontEnd)
- Framework JavaScript moderno (React, Angular ou Vue)
- HTML5 e CSS3
- Layout responsivo

### [Back-end]()
- API REST
- Linguagem a definir: Java, C# ou Node.js

### Banco de Dados
- MySQL ou PostgreSQL

### Padrões
- DTO
- Services
- Repositories
- Arquitetura em camadas

----

## Perfis de Usuário
----

- Oficina (Administrador): controle completo do sistema.
- Funcionário: acesso operacional.
- Cliente: acesso ao próprio histórico, relatórios e status.

----

## Requisitos Funcionais (Resumo)
----

- Login de cliente, funcionário e oficina.
- CRUD de clientes, veículos, funcionários, marcas, peças, serviços e pedidos.
- Geração de relatórios.
- Controle de estoque.
- Visualização de status dos serviços.

----

## Requisitos Não Funcionais (Resumo)
----

- Usabilidade intuitiva.
- Acessibilidade.
- Tempo de resposta inferior a 2 segundos.
- Segurança e níveis de acesso.
- Backup automático.
- Compatibilidade com navegadores modernos.

----

## Status do Projeto
----

- Documentação concluída.
- Modelagem UML concluída.
- Banco de dados estruturado.
- Protótipos de interface criados.
- Desenvolvimento do back-end: em andamento.
- Desenvolvimento do front-end: em andamento.

----

## Contribuintes
----

- [Cristiano Ronaldo Ferreira Bueno](https://github.com/CristianoRFB)
- [Carlos Alberto Ferreira Cândido](https://github.com/Carlos-Candido)
- [João Antonio Gomes Cestonaro](https://github.com/JoaoCestonaro)
- [Gustavo Henrique Moreira Porto](https://github.com/Gustavo-sosu)

----

## Licença
----

Projeto acadêmico desenvolvido para o Projeto Integrador da Fatec Jales.
Licença a definir.
