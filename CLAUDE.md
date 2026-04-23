# Dragon Crashers — Guide Claude Code

RPG demo Unity (~112 scripts). Projet **UI Toolkit-first**, architecture **event-driven**, persistance **centralisée**.

## Stack & conventions

- **Namespace racine** : `UIToolkitDemo` (tous les scripts du gameplay/UI l'utilisent).
- **UI** : 100 % UI Toolkit (UXML + USS). **Aucun `Canvas`** classique, pas de `UnityEngine.UI`.
- **Communication inter-systèmes** : **events statiques** — classes `static` exposant `public static event Action<...>`. Pas de `UnityEvent`, pas de C# events d'instance.
- **Data** : `ScriptableObject` pour tout contenu (personnages, skills, items, niveaux, chats, mails).
- **Persistance** : JSON via `FileManager`, orchestré par `SaveManager`, état runtime dans `GameDataManager`.

## Managers (`Assets/Scripts/Managers/`)

| Manager | Rôle |
|---|---|
| **`GameDataManager`** | **Seul point de mutation** de l'or, des gemmes et des potions (`Gold`, `Gems`, `LevelUpPotions`, `HealthPotions`). Valide les achats (`CanAfford`), applique paiements/rewards, émet `ShopEvents.PotionsUpdated`/`GameDataUpdated`. |
| `SaveManager` | Sérialise/désérialise l'état de jeu (`GameData`, progression, settings). |
| `FileManager` | I/O disque (lecture/écriture JSON, chemins `Application.persistentDataPath`). |
| `AudioManager` | SFX/musique, réagit aux events UI. |
| `LocalizationManager` | Pont avec `UnityEngine.Localization`, réagit aux `SettingsEvents`. |

→ **Pour toute modification d'or/gemmes/potions : passer par `GameDataManager`.** Ne jamais muter `GameData` directement depuis une View.

## UI Toolkit (`Assets/Scripts/UI/UIViews/`)

- **Toutes les vues héritent de `UIView`** (classe de base qui gère `VisualElement` racine, show/hide, binding UXML).
- `UIManager` orchestre la pile de vues et leurs transitions.
- Écrans clés : `HomeView`, `ShopView`, `CharView`, `CharStatsView`, `InventoryView`, `MailView`/`MailboxView`/`MailContentView`/`MailTabView`, `SettingsView`, `ChatView`, `InfoView`, `LevelMeterView`, `MenuBarView`, `OptionsBarView`, `DebugLogView`.
- Les Views **écoutent** les events statiques et **émettent** des events utilisateur — elles ne référencent jamais directement les managers.

### Exemple de réponse rapide
> « Quel pattern UI utilisent HomeView et ShopView ? »
> → Elles héritent de `UIView` (UI Toolkit, UXML/USS). **Pas de Canvas.**

## Events statiques (`Assets/Scripts/UI/Events/`)

Classes `static` avec `public static event Action<...>` — invocation via `XxxEvents.Foo?.Invoke(...)`.

- `GameplayEvents` — flux combat/niveau (victoire, défaite, démarrage…).
- `ShopEvents` — achats, mise à jour d'inventaire, potions.
- `CharEvents` — sélection/équipement/level-up des personnages (inclut `LevelPotionUsed`).
- `MailEvents` — boîte mail (ouverture, réclamation de récompenses).
- `MainMenuUIEvents` — navigation menu principal.
- `HomeEvents`, `InventoryEvents`, `SettingsEvents`, `ThemeEvents`, `MediaQueryEvents` — domaines secondaires.

> 10 classes d'events statiques au total dans `UI/Events/`. Communication **émetteur → event statique → N abonnés**, toujours en one-way ; pas de référence directe entre systèmes.

## Combat (`Assets/Scripts/Gameplay/Unit/`)

- `UnitController` = orchestrateur par unité ; délègue à des sous-behaviours :
  - `UnitAbilitiesBehaviour` / `UnitAbilityBehaviour`
  - `UnitHealthBehaviour`
  - `UnitTargetsBehaviour`
  - `UnitAudioBehaviour`
  - `UnitCharacterAnimationBehaviour`
  - `UnitDamageDisplayBehaviour` (+ `UnitsDamageDisplayListener` global)
- Composition > héritage : chaque behaviour est un `MonoBehaviour` attaché à la même unité.

## ScriptableObjects (`Assets/Scripts/ScriptableObjects/`)

`CharacterBaseSO`, `SkillSO`, `EquipmentSO`, `ShopItemSO`, `LevelSO`, `MailMessageSO`, `ChatSO`, `GameIconsSO`, `ResourceLinkSO`.

## Règles pour Claude Code

1. **Ne jamais introduire de `Canvas`** ni `UnityEngine.UI`. Tout nouvel écran = `UXML` + `USS` + classe héritant de `UIView`.
2. **Ne jamais ajouter un `UnityEvent` ou un event d'instance** pour communiquer entre systèmes — créer/étendre une classe `XxxEvents` statique.
3. **Or / gemmes / potions** : toute modification passe par `GameDataManager` (jamais d'écriture directe sur `GameData`).
4. **Sauvegarde** : ne pas écrire sur disque depuis une View/Manager feuille — passer par `SaveManager` → `FileManager`.
5. **Namespace** : rester dans `UIToolkitDemo` pour tout nouveau code gameplay/UI.
6. **MCP Unity** : utiliser `read_console` après `manage_script`/`create_script` pour vérifier la compilation avant d'utiliser un nouveau type.
