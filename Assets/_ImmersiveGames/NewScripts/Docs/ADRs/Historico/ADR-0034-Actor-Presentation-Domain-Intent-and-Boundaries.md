# ADR-0034: Actor Presentation Domain Intent and Boundaries

## Status atual do runtime

- Leitura prática: histórico / intenção futura.
- O runtime atual ainda não consolidou Actor Presentation como domínio único; a apresentação runtime do ator continua distribuída entre Gameplay, Audio, IntroStage, PostRun e bridges locais.

## Status

- Estado: **Aceito**
- Data (decisão): **2026-03-26**
- Última atualização: **2026-03-26**
- Decisores: Time NewScripts
- Escopo: Definição do domínio de `Actor Presentation` em `NewScripts`

## Evidências canônicas (atualizado em 2026-03-26)

- Módulo legado/origem `SkinSystems` como fonte de intenção arquitetural
- Auditoria atual do stack de `Presentation / Actor`
- Organização lógica atual de `NewScripts`

---

## Contexto

O módulo legado `SkinSystems` aponta para uma intenção arquitetural clara: a apresentação runtime do ator deve ter **owner local por ator**, com um **controller local** responsável por orquestrar aplicação técnica, estado observável e resolução de contratos ligados à aparência do ator.

No stack atual de `NewScripts`, essa semântica não aparece consolidada em um domínio único. Elementos de apresentação estão distribuídos entre fluxos como `SceneFlow/Loading`, `LevelFlow`, `IntroStage`, `PostRun`, lifecycle local de cena e bridges auxiliares. Isso torna difuso quem é dono da apresentação runtime do ator e favorece acoplamento com fluxos que não deveriam definir essa semântica.

Antes de qualquer consolidação maior, o domínio precisa de um limite canônico explícito: **qual problema ele resolve, quem o possui, o que pertence a ele e o que permanece fora dele**.

---

## Decisão

1. O domínio de **Actor Presentation** resolve a **apresentação runtime do ator**.

2. O owner canônico do domínio é **local por ator**.

3. O domínio deve ser estruturado em torno de um **controller local por ator**, responsável por orquestrar o ciclo mínimo da apresentação:
    - `apply`
    - `reapply`
    - `clear`
    - `cleanup / dispose local`

4. O domínio inclui:
    - controller local por ator;
    - service de aplicação técnica;
    - runtime state / metrics / bounds;
    - profiles / configs de apresentação;
    - resolução local por ator e eventos locais de apresentação.

5. Outros módulos podem:
    - disparar,
    - consumir,
    - reagir,
    - observar
      a apresentação do ator, mas **não definem sua semântica**.

6. `Runtime State / Metrics` deste domínio são **fonte consultável** para outros módulos, mas isso **não transfere ownership** de gameplay, colisão, loop de jogo, navegação, reset ou fluxo macro para `Actor Presentation`.

7. `Audio` e `FX` pertencem a este domínio **somente quando forem actor-bound**.
   Sistemas globais de áudio/VFX permanecem fora deste domínio.

8. O domínio **não** inclui:
    - decisão de level/scene;
    - fluxo macro;
    - UI global;
    - ownership de transições globais;
    - loading global;
    - PostRun global;
    - reset macro;
    - state machine de game loop.

---

## Responsabilidades do domínio

### 1. Actor Presentation Controller
Owner local por ator da apresentação runtime.
Responsável por:
- aplicar apresentação;
- reaplicar apresentação;
- limpar apresentação;
- encerrar/descartar apresentação local;
- coordenar o service técnico sem absorver fluxo macro.

### 2. Application Service
Responsável pela aplicação técnica da apresentação do ator:
- criação;
- instanciação/reuso;
- wiring técnico local;
- atualização/remoção técnica de instâncias;
- organização de slots/containers locais do ator.

### 3. Runtime State / Metrics
Responsável por manter estado observável da apresentação do ator:
- bounds;
- radius;
- center;
- métricas runtime;
- estado consultável por outros módulos.

### 4. Presentation Profiles / Configs
Responsável por descrever perfis e variações de apresentação do ator:
- visual;
- audio actor-bound;
- fx actor-bound;
- parâmetros de apresentação do ator.

### 5. Actor-local Resolution / Events
Responsável por:
- resolução local por ator;
- contratos locais do ator;
- eventos locais de apresentação;
- acesso consultável sem depender de fluxo macro.

---

## Fora de escopo

Este domínio **não** é owner de:

- Loading HUD global
- SceneFlow timeline
- Navigation e route dispatch
- GameLoop state machine
- IntroStage como fluxo macro
- PostRun como fluxo macro
- Reset macro
- root global de UI
- decisão de level/scene
- sistemas globais de áudio
- sistemas globais de VFX

---

## Boundaries

| módulo externo | relação com Actor Presentation | ownership permanece onde |
|---|---|---|
| Gameplay | pode publicar lifecycle, spawn, despawn e consumir apresentação do ator | a semântica da apresentação permanece em `Actor Presentation` |
| LevelFlow | pode disparar ou reagir a contratos de level e hooks opcionais | ownership do ator não migra para `LevelFlow` |
| SceneFlow | pode consumir eventos e coordenar transições macro | timeline, gates e fluxo macro permanecem em `SceneFlow` |
| Navigation | pode iniciar intenções que afetem atores indiretamente | route/intent dispatch permanece em `Navigation` |
| Reset / WorldReset | pode limpar estado global e recriar contexto | reset macro permanece fora de `Actor Presentation` |
| SceneReset | pode acionar cleanup/recriação local de cena/ator | pipeline local de reset permanece fora do domínio |
| Loading | pode coexistir visualmente e consumir estado de loading | loading global permanece owner de loading |
| PostRun | pode reagir ao estado terminal da run | semântica de encerramento permanece em `RunOutcome` |
| IntroStage | pode consumir contratos do level ou do ator para projeção de entrada | fluxo de intro permanece fora do domínio |
| Audio global | pode coexistir e tocar cues globais | ownership global de áudio permanece fora |
| VFX global | pode coexistir com apresentação do ator | ownership global de VFX permanece fora |

---

## Racional

- `SkinSystems` indica uma intenção correta de ownership local por ator.
- O stack atual de `NewScripts` ainda não consolidou essa intenção em um domínio único.
- Sem esta decisão, a apresentação do ator tende a continuar espalhada por fluxos macro do jogo.
- Definir o domínio antes do plano evita criar um “mega-módulo de presentation” genérico demais ou deixar `SceneFlow`, `LevelFlow`, `IntroStage` ou `PostRun` assumirem ownership semântico por acidente.

---

## Consequências

### Positivas
- A apresentação runtime deixa de ser lida como responsabilidade difusa de fluxos macro.
- Callers externos passam a consumir o domínio em vez de definir sua semântica.
- O runtime state da apresentação ganha owner local mais claro por ator.
- Boundaries com fluxos globais ficam explícitos e auditáveis.
- Fica mais fácil separar apresentação do ator de loading, intro, PostRun e reset.

### Negativas / Trade-offs
- A implementação atual ainda pode permanecer espalhada por algum tempo.
- O ADR não resolve sozinho a organização física da árvore.
- A consolidação futura exigirá plano incremental para alinhar runtime, contracts e providers ao domínio definido aqui.

---

## Não-objetivos

Este ADR:

- não define a implementação completa do domínio;
- não obriga copiar `SkinSystems` literalmente;
- não cria um mega-módulo global de presentation;
- não move pastas automaticamente;
- não redefine stacks já consolidados (`SceneFlow`, `LevelFlow`, `Navigation`, `Reset`, `GameLoop`);
- não transforma `Audio` ou `FX` globais em parte de `Actor Presentation`.

---

## Evidência / Validação

**Esperado após esta decisão:**
- futuros planos e refactors tratam `Actor Presentation` como domínio local ao ator;
- fluxos como `SceneFlow`, `LevelFlow`, `IntroStage` e `PostRun` passam a ser consumidores/disparadores, não owners;
- qualquer introdução futura de visual/audio/fx actor-bound respeita owner local por ator;
- métricas/bounds da apresentação podem ser consultadas por outros sistemas sem transferir ownership de gameplay.

**Critério de leitura correta após a decisão:**
- “quem é dono da apresentação desse ator?” → o stack de `Actor Presentation`
- “quem pode disparar ou observar?” → módulos externos
- “quem define o significado da apresentação?” → não é `SceneFlow`, `LevelFlow`, `PostRun`, `IntroStage`, `Navigation` nem `Reset`

---

## Checklist de fechamento

- [x] Domínio definido como apresentação runtime do ator
- [x] Owner local por ator explicitado
- [x] Ciclo mínimo do controller explicitado (`apply`, `reapply`, `clear`, `cleanup/dispose`)
- [x] Runtime state / metrics definido como fonte consultável, sem transferir ownership de gameplay
- [x] Boundary explícito para `Audio/FX` actor-bound
- [x] Fora de escopo e boundaries com fluxos globais formalizados
- [x] Base preparada para plano futuro de consolidação de `Actor Presentation`

