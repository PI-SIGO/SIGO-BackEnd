# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Layout

This is a single-context backend repo for the SIGO ASP.NET Core API.

## Before exploring, read these

- `CONTEXTO_PROJETO.md` for the concise project overview, architecture, stack, endpoints, and roadmap.
- `DOCUMENTACAO_CONTEXTO_SISTEMA.md` for the detailed system context, access rules, domain entities, services, repositories, and current implementation notes.
- `CONTEXT.md` if it is created later by a producer skill.
- `docs/adr/` if ADRs are added later. Read ADRs that touch the area you are about to work in.

If any optional files do not exist, proceed silently. Do not flag their absence or suggest creating them upfront.

## Use the project's vocabulary

When your output names a domain concept, use the terms already used by the repo and its docs, such as `Cliente`, `Funcionario`, `Oficina`, `Veiculo`, `Pedido`, `Peca`, `Servico`, `Marca`, `Telefone`, and `ViaCEP`.

If a concept you need is not documented yet, note the gap when it matters for the task instead of inventing new terminology.

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding it.
