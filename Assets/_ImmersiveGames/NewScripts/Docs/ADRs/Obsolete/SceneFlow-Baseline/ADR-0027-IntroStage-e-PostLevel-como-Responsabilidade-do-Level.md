> [!NOTE]
> Nome historico preservado. Este arquivo e a referencia canonica do fluxo de IntroStage/LevelFlow no runtime atual.

# ADR-0027 - IntroStage como Responsabilidade Canonica do LevelFlow

## Status

- Estado: **Aceito (fonte canonica do IntroStage)**
- Data (decisao): **2026-03-27**
- Ultima atualizacao: **2026-03-27**

## Contexto

O runtime atual consolidou o fluxo de intro como responsabilidade canonica de `LevelFlow`.

Antes disso, a intro oscilava entre trilhos de `GameLoop`, pipeline macro e presenters concretos.
Isso criou duplicacao de policy, timing ambguo e acoplamento desnecessario ao mecanismo de execucao.

## Decisao

### 1) Ownership

`LevelFlow` e o owner canonico da `IntroStage`.
`GameLoop` nao e gate canonico da intro.
`GameLoop` apenas consome o handoff final quando necessario.

### 2) Hook de entrada

`LevelEnteredEvent` e o hook oficial de entrada do fluxo de intro.
Ele nasce somente depois que o level foi efetivamente aplicado e esta ativo.
Nao deve ser usado antes desse ponto.

### 3) Hook de saida

`LevelIntroCompletedEvent` e o handoff oficial de saida da intro.
Ele e publicado quando a intro conclui ou e pulada de forma canonica.
Esse evento e o seam que permite `Ready -> Playing` sem recolocar o GameLoop como owner da intro.

### 4) Presenter canonico

O presenter de intro e resolvido por contrato, via `ILevelIntroStagePresenter`.
O host adota a instância valida, faz bind da session atual, valida queryability e devolve a mesma instancia pronta.

O host nao depende de `LevelIntroStageMockPresenter` como tipo concreto.

### 5) Escopo de descoberta

O escopo de descoberta do presenter pertence a uma abstracao do proprio `LevelFlow`.
O host consome o resolver canonico do modulo e nao conhece a topologia concreta de carregamento.

Se houver mais de um presenter valido no mesmo contexto de levelSignature, o fluxo falha cedo.

### 6) Unicidade por levelSignature

Existe no maximo uma intro por level.
Existe no maximo uma execucao por `levelSignature`.
`Continue` e `Skip` sao one-shot e concluem exatamente uma vez por assinatura.

### 7) SimulationGate

`sim.gameplay` e bloqueado durante a intro e liberado no fim pelo executor canonico da fase.
O `GameLoop` nao abre nem fecha esse gate como owner da intro.

### 8) Relação com MacroPrepare e Swap Local

`LevelMacroPrepareService` e `LevelSwapLocalService` publicam `LevelEnteredEvent` apenas depois da aplicacao efetiva do level.
Isso vale para:
- `Menu -> Gameplay`
- `LevelSwapLocal` com intro
- `LevelSwapLocal` sem intro
- `ResetCurrentLevel`
- `RestartFromFirstLevel`

### 9) Fail-fast

O runtime deve falhar cedo em:
- ambiguidade de presenter no escopo do level;
- presenter nao queryable apos bind/adoption;
- ausencia de presenter canonico quando `HasIntroStage=true`;
- configuracao invalida da session canonica.

### 10) Observabilidade minima

A ordem esperada no runtime e:
1. `LevelEnteredEvent`
2. `IntroPresenterRegistered` ou `IntroPresenterAdopted`
3. `IntroStageStartRequested`
4. `IntroStageStarted`
5. `GameplaySimulationBlocked`
6. confirmacao canonica `CompleteIntroStage(...)` ou skip canonico
7. `LevelIntroCompletedEvent`
8. `GameplaySimulationUnblocked`

## Consequencias

### Positivas

- ownership da intro fica claro e unico;
- o GameLoop deixa de competir como gate da intro;
- o presenter canônico passa a ser contrato e nao tipo concreto;
- o fluxo fica consistente entre menu, restart e swap local.

### Trade-offs

- o host passa a depender de uma abstracao de escopo do LevelFlow, nao da topologia concreta de scenes;
- a descoberta de presenter deixa de ser oportunistica e passa a ser fail-fast.

## Relacoes

- `ADR-0030`: fronteiras canonicas entre `SceneFlow`, `Navigation` e `LevelFlow`.
- `ADR-0037`: lista oficial de hooks e pontos de extensao.
- `ADR-0025`: pipeline macro prepara/aplica o level antes do fade out.
- `ADR-0026`: swap local segue no dominio de `LevelFlow`, sem nova transicao macro.

