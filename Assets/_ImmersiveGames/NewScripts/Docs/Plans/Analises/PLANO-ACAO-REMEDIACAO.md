# PLANO DE AÇÃO - Remediação Arquitetural
**Data**: 2026-04-24
**Responsável**: Tim arquitetural
**Status**: 🚀 Pronto para implementação

---

## Executive Summary

| Aspecto | Diagnóstico | Prioridade | Timeline |
|---------|-----------|-----------|----------|
| **Trilhas Fantasmas** | 3 violations críticos encontrados | 🔴 AGORA | Sprint atual |
| **Saúde Geral** | 70% conforme, 30% com service locators | ⚠️ CRÍTICO | 1-2 sprints |
| **Risco Arquitetural** | Baixo (conceitos estão certos, apenas técnica) | 🟡 MÉDIO | Monitoramento |

---

## O Que Foi Encontrado?

### Trilhas Fantasmas (3 críticas)

1. **Service Locator em `SessionFlowActorsSemanticPortsAdapter`**
   - Resolve `ISceneFlowRouteActorSetRefContext` e `IActorSetSelectionService` via DI global
   - Cria dependência oculta que não está documentada
   - **Risco**: Se `SceneFlowBootstrap` não roda, atores não inicializam

2. **Composição Implícita em `GameplaySessionFlowCompletionGateComposer`**
   - Resolve `IGameplaySessionFlowPrepareOperationalHandoffService` sem declarar como parâmetro
   - Não está claro **onde** este serviço é registrado
   - **Risco**: Quebra silenciosa se composição reordenar

3. **Injeção de Dependência em Awake em `GameRunEndedEventBridge`**
   - MonoBehaviour resolve 3 serviços em `Awake()`
   - Frágil à ordem de inicialização
   - **Risco**: GameObject criado antes de bootstrap = falha em Awake

### Áreas Saudáveis ✅

- ✅ **ActorsSystem** com ownership claro
- ✅ **Operational Binding** explícito
- ✅ **SceneFlow baseline** enxuto
- ✅ **Session Integration** README bem documentado

---

## Por Que Importa?

### Violações dos ADRs Prescritos

```
ADR-0057 §15 proíbe explicitamente:
"- GameplaySessionFlowPrepareCompletionGate com dependencia explicita
   de handoff operacional (SEM service locator no hot path)."

ATUAL: 3 service locators estão VIOLANDO isto.
```

### Impacto em Desenvolvimento

| Cenário | Impacto | Severidade |
|---------|--------|-----------|
| Refatoração de bootstrap | Service locators podem quebrar silenciosamente | 🔴 Crítico |
| Novo desenvolvedor | Difícil entender "onde" dependências vêm | 🟡 Médio |
| Testes unitários | Impossível testar sem DI global | 🟡 Médio |
| Debugging | Stack trace confuso, erro em runtime não em compile | 🔴 Crítico |

---

## Plano de Ação (Priorizado)

### Fase 1: Crítico (Sprint Atual - 5-8 dias)

#### 1.1 Corrigir Service Locator em `SessionFlowActorsSemanticPortsAdapter`
**Arquivo**: `ActorsSystem/Integration/SessionFlow/SessionFlowActorsSemanticPortsAdapter.cs`

**Mudanças**:
```
- Adicionar 2 campos: _routeActorSetContext, _actorSetSelectionService
- Atualizar constructor para receber ambos
- Remover método TryResolveDefinitionsDependencies()
- Atualizar TryGetCurrent() para usar campos diretos
```

**Teste**: Verificar que `TryGetCurrent()` não contém `TryGetGlobal`

**Esforço**: ~1 hora

**Risco**: Baixo - apenas refatoração local

---

#### 1.2 Corrigir Composição Implícita em `GameplaySessionFlowCompletionGateComposer`
**Arquivo**: `SessionFlow/Integration/SceneFlow/GameplaySessionFlowCompletionGateComposer.cs`

**Mudanças**:
```
- Adicionar parâmetro `handoffService` a ComposeOrValidate()
- Remover método ResolveRequiredPrepareHandoffService()
- Atualizar chamador para passar dependência explicitamente
```

**Teste**: Verificar que `ComposeOrValidate()` tem assinatura com handoffService

**Esforço**: ~1 hora

**Risco**: Médio - exige encontrar e atualizar chamador

---

#### 1.3 Corrigir Injeção em Awake em `GameRunEndedEventBridge`
**Arquivo**: `SessionFlow/Integration/RunReset/GameRunEndedEventBridge.cs`

**Mudanças**:
```
- Adicionar método público Initialize(3 dependencies)
- Remover resolução em Awake()
- Adicionar validação em Awake() que Initialize foi chamado
- Remover método ResolveRequired<T>()
- Criar factory ou atualizar código que instantia este GameObject
```

**Teste**: Verificar que Awake() não contém ResolveRequired

**Esforço**: ~2 horas (1 para mudar classe, 1 para encontrar e atualizar instantiation)

**Risco**: Médio-Alto - exige encontrar onde GameObject é criado

---

#### Checklist Fase 1
- [ ] PR #1: Corrigir `SessionFlowActorsSemanticPortsAdapter`
- [ ] PR #2: Corrigir `GameplaySessionFlowCompletionGateComposer`
- [ ] PR #3: Corrigir `GameRunEndedEventBridge`
- [ ] Todos os testes passando
- [ ] Code review aprovado

**Timeline**: 2-3 dias

---

### Fase 2: Conformidade (Sprint +1 - 3-5 dias)

#### 2.1 Remover Service Locator de `RunEndBridgeRuntimeComposer`
**Arquivo**: `SessionFlow/Integration/RunReset/Installers/RunEndBridgeRuntimeComposer.cs`

**Mudanças**:
```
- Refatorar para receber todas as dependências já compostas
- Em lugar de ResolveRequired<T>(), verificar em parameter
- Documentar onde deve ser chamado no pipeline
```

**Esforço**: ~1-2 horas

**Risco**: Médio - refatoração de installer

---

#### 2.2 Documentar Pipeline de Composição
**Arquivo Novo**: `COMPOSIÇÃO-MAPA.md`

**Conteúdo**:
```
- Tabela com cada serviço crítico
- Onde é composto (arquivo, funcio)
- Dependências que precisa
- Ordem esperada
```

**Esforço**: ~1 hora

**Risco**: Muito Baixo - apenas documentação

---

#### 2.3 Padronizar Padrão de Composição
**Documento**: Adicionar à styleguide

```
Regra 1: Serviços OBRIGATÓRIOS usam ResolveRequired (exceção se missing)
Regra 2: Serviços OPCIONAIS usam Ensure (fallback se missing)
Regra 3: Nenhum ResolveRequired fora de Bootstrap/Installer
```

**Esforço**: ~30 minutos

**Risco**: Muito Baixo

---

#### Checklist Fase 2
- [ ] PR #4: Refatorar `RunEndBridgeRuntimeComposer`
- [ ] Documento: `COMPOSIÇÃO-MAPA.md` criado
- [ ] StyleGuide atualizado com padrão

**Timeline**: 1 sprint (3-5 dias)

---

### Fase 3: Robustez (Sprint +2 - Ongoing)

#### 3.1 Adicionar Testes de Composição
**Arquivo**: `Tests/ArchitectureConformanceTests.cs`

```csharp
[TestFixture]
public class CompositionConformanceTests
{
    [Test]
    public void NoServiceLocatorInSessionFlowIntegration()
    {
        var code = File.ReadAllText("SessionFlowActorsSemanticPortsAdapter.cs");
        Assert.That(code, Does.Not.Contain("TryGetGlobal"));
    }
}
```

**Esforço**: ~2 horas

---

#### 3.2 Integrar Validação em CI/CD
**Arquivo**: `.github/workflows/architecture-validation.yml` ou `azure-pipelines.yml`

```yaml
- name: Validate Architecture
  run: pwsh validate-architecture.ps1
```

**Esforço**: ~1 hora

---

#### 3.3 Dashboard de Métricas
**Para**: Monitorar drift ao longo do tempo

**Esforço**: ~2-3 horas

---

#### Checklist Fase 3
- [ ] Testes de conformidade adicionados
- [ ] CI/CD integrado
- [ ] Dashboard opcional criado

**Timeline**: Ongoing, próximos 2 sprints

---

## Recursos Fornecidos

### 1. Documentos de Diagnóstico
- ✅ `ARQUITETURA-SAUDE-DIAGNOSTICO-2026-04-24.md` - Análise completa
- ✅ `VALIDACAO-ARQUITETURAL-CHECKLIST.md` - Testes, métricas, validation
- ✅ `TRILHAS-FANTASMAS-ANATOMIA.md` - Detalhe de cada trilha + remediação

### 2. Scripts
Script PowerShell `validate-architecture.ps1` incluído em `VALIDACAO-ARQUITETURAL-CHECKLIST.md`

### 3. Testes Template
Template de testes unitários em `VALIDACAO-ARQUITETURAL-CHECKLIST.md`

---

## Checklist de Implementação (Fase 1)

### Antes de Começar
- [ ] Leia `TRILHAS-FANTASMAS-ANATOMIA.md` para entender cada trilha
- [ ] Confirme que branch está atualizado (main)
- [ ] Configure seu IDE para marcar `TryGetGlobal` como warning

### Durante
- [ ] Use branch feature para cada mudança
- [ ] Commit atômico com mensagem clara
- [ ] Run testes localmente antes de push
- [ ] Link este documento no PR

### Depois de Cada Mudança
- [ ] Verificar que não há `TryGetGlobal` em método
- [ ] Verificar que all constructor parâmetros estão documentados
- [ ] Update correspondente teste (se existir)
- [ ] Rodar suite completo de testes

### Antes de Merge
- [ ] Code review aprovado
- [ ] Teste CI/CD passando
- [ ] Documentação atualizada
- [ ] Confirmar remediação com esta checklist

---

## Risk Assessment

### Risco de Implementação: BAIXO

**Por quê**:
- Mudanças são localizadas (3 arquivos)
- Lógica de negócio não muda
- Apenas refatoração técnica
- Testes devem passar igual

### Risco de Regressão: BAIXO

**Por quê**:
- Não há mudança em runtime behavior
- Apenas dependency resolution muda
- Sistema já funciona (trilhas fantasmas "funcionam por acaso")
- Mais seguro depois das mudanças

### Risco de Não Fazer: MÉDIO-ALTO

**Por quê**:
- Cada refatoração futura risco de quebrar silenciosamente
- Novo dev vai copiar padrão errado
- Debt acumula ao longo do tempo
- ADRs explicitamente violados

---

## Métricas de Sucesso

### Sprint Atual (Fase 1)
- ✅ 3 PRs mergeados
- ✅ 0 service locators em hot path
- ✅ 100% testes passando
- ✅ Comentários de revisão resolvidos

### Próximas 2 Sprints (Fase 2-3)
- ✅ Documentação completa
- ✅ CI/CD integrado
- ✅ Nenhuma nova violation de ADR-0057
- ✅ Novo código adere ao padrão

---

## FAQ

### P: "Não funciona agora? Por que consertar?"
A: Funciona por "acaso" - dependências já estão registradas. Qualquer reorder de bootstrap quebra silenciosamente. É tech debt acumulando.

### P: "Quanto tempo vai levar?"
A: ~5-8 horas total (Fase 1). Fase 2-3 é consolidação ~2-3 horas cada.

### P: "Preciso parar de fazer feature?"
A: Recomendado. Trilhas fantasmas podem interferir com refatorações futuras.

### P: "E se algo quebrar?"
A: Muito improvável. Refatoração é local. Se quebra, é porque tinha bug oculto já. Tests vão pegar.

### P: "Preciso atualizar testes?"
A: Sim, alguns testes podem precisar atualizar DI setup. Minimal impact.

---

## Próximos Passos

### Imediato (hoje)
1. [ ] Compartilhar diagnóstico com time
2. [ ] Discutir timeline com PM
3. [ ] Alocar dev para Fase 1

### Esta Semana
1. [ ] Dev começa com `SessionFlowActorsSemanticPortsAdapter` (PR #1)
2. [ ] Code review + merge
3. [ ] Continua com PR #2

### Próxima Semana
1. [ ] PR #3 (GameRunEndedEventBridge)
2. [ ] Fase 1 completa

### Próximas 2 Semanas
1. [ ] Fase 2 (Consolidação)
2. [ ] Testes de conformidade adicionados

---

## Contatos

- **Arquitetura**: Consultar ADRs 0056-0059 em `Docs/ADRs/`
- **Documentação**: Ver links em seção "Recursos Fornecidos"
- **Dúvidas**: Consultar `TRILHAS-FANTASMAS-ANATOMIA.md` para detalhes de remediação

---

## Assinatura

**Data**: 2026-04-24
**Diagnóstico Realizado Por**: Análise Arquitetural Automatizada
**Status**: ✅ Pronto para implementação


