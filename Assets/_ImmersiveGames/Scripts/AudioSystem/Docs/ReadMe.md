
# ✅ **README.md – Versão Resumida**

# ImmersiveGames – Sistema de Áudio Integrado ao SkinSystem
### Versão Atual – Arquitetura Skin-Driven + Estratégias de Tiro

Este módulo implementa um sistema de áudio totalmente integrado ao SkinSystem.  
O som de tiro (e outros sons específicos de gameplay) é definido **exclusivamente pela Skin**,  
permitindo temas sonoros completos e independentes da lógica do Player.

## Conceito Central

SkinAudioConfigData (SoundRoot)
→ SkinAudioConfigurable (no ator)
→ IActorSkinAudioProvider
→ Estratégia de Tiro (SkinAudioKey)
→ PlayerShootController
→ EntityAudioEmitter.Play()

O Player não mantém mais referências diretas a `SoundData`.  
A escolha do som depende apenas da combinação:

- **Skin ativa**
- **Estratégia ativa**
- **Chave configurada na skin**

## Componentes Principais

- **SkinAudioConfigData**  
  Define pares `(SkinAudioKey → SoundData)`.

- **SkinAudioConfigurable**  
  Recebe a skin atual e expõe um provider de áudio.

- **ISpawnStrategy** (Single, MultipleLinear, Circular...)  
  Cada estratégia define **sua própria SkinAudioKey**.

- **PlayerShootController**  
  Executa spawn e solicita o som à skin via provider.

## Para Designers  
Consulte **Manual do Designer – Áudio por Skin**.

## Para Programadores  
Consulte **Manual do Programador – Arquitetura e Extensões**.

---

# 🎨 **MANUAL DO DESIGNER – Áudio por Skin**

### (Documento oficial para Game Designers, Level Designers e Sound Designers)


# Manual do Designer – Sistema de Áudio Integrado ao SkinSystem

Este manual explica **como configurar sons no Unity** usando o sistema de skins.  
Não é necessário entender código ou detalhes técnicos.

---

# 1. O Conceito Importante

O Player e inimigos **não possuem sons configurados diretamente no prefab**.  
Quem controla os sons agora é **a Skin**.

Cada skin pode ter:

- **som de tiro diferente**
- som para habilidades alternativas
- som temático para personagens / naves

Isso permite criar temas sonoros completos.

---

# 2. Onde configurar os sons

## 2.1. SkinAudioConfigData

No Project:

```

Right Click → Create → ImmersiveGames → Skin → Skin Audio Config

```

Neste asset, você verá uma lista:

```

| Key      | SoundData     |
| -------- | ------------- |
| Shoot    | Fire_Pistol   |
| ShootAlt | Fire_Shotgun  |
| Laser    | Laser_Player  |
| Ultimate | Boom_Ultimate |

```

### Cada entrada significa:
- **Key** → O nome da ação (ex.: Shoot, Shoot_Alt, Laser etc.).  
- **SoundData** → O áudio que será reproduzido para aquela ação.

Você pode criar quantas quiser.

---

# 3. Como aplicar este áudio no Player

O prefab do Player (ou Inimigo) deve ter:

- `SkinAudioConfigurable`
- `EntityAudioEmitter`
- `PlayerShootController`
- `ActorSkinController`

**Você NÃO precisa mexer neles.**

Eles já usam automaticamente o som definido na skin.

---

# 4. Como escolher sons diferentes por tipo de tiro

No PlayerShootController (Inspector), existe um bloco para cada estratégia:

### Exemplo:

```

Single Strategy
Shoot Audio Key: Shoot

Multiple Linear Strategy
Shoot Audio Key: ShootAlt

Circular Strategy
Shoot Audio Key: Ultimate

```

O Designer pode escolher QUAL chave a estratégia usa.

O sistema então:

1. Lê a chave da estratégia.
2. Busca essa chave na SkinAudioConfigData.
3. Toca o som correspondente.

---

# 5. Troca de Skin → Troca de Som

Se você mudar a skin do Player:

```

Skin A: Shoot → PistolSound
Skin B: Shoot → LaserSound

```

O Player automaticamente passa a usar o som da nova skin.

**Nenhuma modificação no Player é necessária.**

---

# 6. Regras fundamentais

- Cada som deve existir **na skin**, nunca no Player.
- Cada estratégia de tiro deve ter **uma chave configurada**.
- Se faltar uma entrada na skin, o console mostrará erro:
  “Som não configurado para a chave X na skin Y.”

---

# 7. Checklist rápido para Designers

| Tarefa | ✔️ |
|-------|----|
| Criar SkinAudioConfigData | ✔️ |
| Inserir Keys e SoundData | ✔️ |
| Adicionar SkinAudioConfigData na SkinCollectionData | ✔️ |
| No PlayerShootController → Configurar ShootAudioKey por estratégia | ✔️ |
| Testar em runtime | ✔️ |

---

# 8. Resumo

O sistema é simples para o designer:

1. **Configurar sons em SkinAudioConfigData**  
2. **Configurar chave de tiro em cada estratégia**  
3. **Dar Play**

Todo o resto é automático.

---

# 💻 **MANUAL DO PROGRAMADOR – Arquitetura, Extensões e Regras**

### (Documento oficial para devs do projeto)

# Manual do Programador – Sistema de Áudio Integrado ao SkinSystem

Este documento detalha a arquitetura, responsabilidades, pontos de extensão e padrões obrigatórios.


# 1. Objetivo Arquitetural

O sistema foi projetado para:

- **desacoplar completamente áudio de lógica de Player**
- mover todo conteúdo para **SkinAudioConfigData**
- permitir que cada estratégia de tiro mude o som
- tornar o sistema escalável e aderente ao SOLID

---

# 2. Fluxo Técnico Completo

```

SkinCollectionData
↓ resolve ModelType.SoundRoot
SkinAudioConfigData
↓ aplicada via SkinAudioConfigurable
IActorSkinAudioProvider
↓
PlayerShootController
↓ busca SkinAudioKey via strategy
ISpawnStrategy
↓
EntityAudioEmitter.Play(soundData)

````

---

# 3. Componentes Detalhados

---

## 3.1. SkinAudioConfigData

```csharp
Dictionary<SkinAudioKey, SoundData> AudioEntries;
````

Regras:

* Nunca deve conter lógica.
* Apenas define dados.
* Deve ser referenciada na SkinCollection.

---

## 3.2. SkinAudioConfigurable

Responsabilidades:

* Herdar de `SkinConfigurable`.
* Registrar o `ISkinAudioConfig` corrente.
* Expor `IActorSkinAudioProvider`.

Código chave:

```csharp
public bool TryGetSound(SkinAudioKey key, out SoundData sound)
```

Não instancia nada.
Não toca som.
Não tem acoplamento com prefab.

---

## 3.3. ISpawnStrategy

```csharp
SkinAudioKey ShootAudioKey { get; }
List<SpawnData> GetSpawnData();
```

Regras obrigatórias:

* Estratégias **não** devem ter `SoundData`.
* Estratégias definem *somente a key*.

---

## 3.4. PlayerShootController

Responsabilidades:

* Executar spawn.
* Consultar `ShootAudioKey` da estratégia ativa.
* Solicitar o áudio à skin via provider.
* Tocar via `EntityAudioEmitter`.

Não deve conter:

* `SoundData` referenciado diretamente.
* fallback de som.
* seleção manual de áudio.

---

# 4. Regras de Arquitetura

### 4.1. Som SEMPRE vem da Skin

O código **não pode** carregar áudio diretamente no controller.

### 4.2. Estratégias tratam SOMENTE de spawn

Nada de conteúdo (SoundData, prefabs etc.).

### 4.3. PlayerShootController depende apenas de:

* `ISpawnStrategy`
* `IActorSkinAudioProvider`
* `EntityAudioEmitter`

### 4.4. Provider é obrigatório

Se faltar `SkinAudioConfigurable` → erro

---

# 5. Extensões

### 5.1. Criar nova estratégia de tiro

1. Criar classe:

```csharp
class NovaEstrategia : ISpawnStrategy
```

2. Implementar:

```csharp
SkinAudioKey ShootAudioKey;
List<SpawnData> GetSpawnData();
```

3. Adicionar no PlayerShootController.

---

### 5.2. Criar novos sons

Adicionar novas chaves em `SkinAudioKey` e configurá-las na skin.
Não mexer no Player.

---

### 5.3. Criar novas ações sonoras

Seguir mesmo pattern:

1. Skin define key.
2. Componente chama provider:

```csharp
TryGetSound(key, out var sound)
```

3. Usa `EntityAudioEmitter` para tocar.

---

# 6. Debug / Logs

Erros são claros:

* Skin não contém a key →
  `"Som não configurado para a chave X na skin Y."`

* Falta do provider →
  `"IActorSkinAudioProvider não encontrado."`

---

# 7. Manutenção e Evolução

* Sistema não precisa de refactor para novas skins.
* Designers podem criar sons temáticos livres.
* Possível expansão futura:

    * Volume por skin.
    * Perfil de spatialization por skin.
    * Ações sonoras adicionais por strategy.

---

# 8. Resumo do Programador

O que você PRECISA saber e lembrar:

* Player **não deve ter SoundData direto**.
* Strategies **só configuram SkinAudioKey**.
* SkinAudioConfigData centraliza todo conteúdo.
* SkinAudioConfigurable fornece lookup dinâmico.
* PlayerShootController só combina:

    * estratégia ativa
    * skin ativa