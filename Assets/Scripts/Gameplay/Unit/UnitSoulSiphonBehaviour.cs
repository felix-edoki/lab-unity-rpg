using UnityEngine;
using UnityEngine.Events;

public class UnitSoulSiphonBehaviour : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] UnitController self;

    [Header("Cost & Support")]
    [Tooltip("Fraction of max HP consumed by Giorgia on every cast.")]
    [SerializeField, Range(0f, 1f)] float hpCostPercentOfMax = 0.08f;

    [Tooltip("Fraction of final damage converted to healing for each ally (Giorgia never heals herself).")]
    [SerializeField, Range(0f, 1f)] float healPercentOfDamage = 0.5f;

    [Header("Escalating Damage")]
    [Tooltip("x = missing HP ratio (0..1), y = damage multiplier. Keep y(0)=1 and y(1) above 1.")]
    [SerializeField] AnimationCurve damageMultiplierByMissingHp =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 2f);

    [Header("Frenzy")]
    [SerializeField, Range(0f, 1f)] float frenzyHpThreshold = 0.35f;
    [Tooltip("Prefab instantiated as a child of this unit while HP is below frenzy threshold; destroyed when HP recovers.")]
    [SerializeField] GameObject frenzyVfxPrefab;
    [SerializeField] Vector3 frenzyVfxLocalOffset = new Vector3(0f, 1.5f, 0f);
    GameObject m_FrenzyInstance;
    bool m_FrenzyActive;

    [Header("Heal VFX")]
    [SerializeField] GameObject allyHealVfxPrefab;
    [SerializeField] float healVfxLifetime = 1.5f;
    [SerializeField] Vector3 healVfxLocalOffset = new Vector3(0f, 1f, 0f);

    UnitAbilityBehaviour[] m_HookedAbilities;

    void Awake()
    {
        if (self == null) self = GetComponent<UnitController>();
        SetFrenzyActive(false);
    }

    void Start()
    {
        HookIntoAbilities();
    }

    void OnEnable()
    {
        if (self != null && self.healthBehaviour != null)
            self.healthBehaviour.HealthChanged += OnHealthChanged;
    }

    void OnDisable()
    {
        if (self != null && self.healthBehaviour != null)
            self.healthBehaviour.HealthChanged -= OnHealthChanged;

        UnhookFromAbilities();
    }

    void HookIntoAbilities()
    {
        if (self == null || self.abilitiesBehaviour == null || self.abilitiesBehaviour.abilities == null) return;

        m_HookedAbilities = self.abilitiesBehaviour.abilities;
        for (int i = 0; i < m_HookedAbilities.Length; i++)
        {
            UnitAbilityBehaviour ability = m_HookedAbilities[i];
            if (ability == null || ability.applyAbilityValueToTargets == null) continue;

            int count = ability.applyAbilityValueToTargets.GetPersistentEventCount();
            for (int j = 0; j < count; j++)
                ability.applyAbilityValueToTargets.SetPersistentListenerState(j, UnityEventCallState.Off);

            ability.applyAbilityValueToTargets.AddListener(OnAbilityValue);
        }
    }

    void UnhookFromAbilities()
    {
        if (m_HookedAbilities == null) return;
        for (int i = 0; i < m_HookedAbilities.Length; i++)
        {
            UnitAbilityBehaviour ability = m_HookedAbilities[i];
            if (ability == null || ability.applyAbilityValueToTargets == null) continue;
            ability.applyAbilityValueToTargets.RemoveListener(OnAbilityValue);
        }
        m_HookedAbilities = null;
    }

    void OnHealthChanged(int _)
    {
        UpdateFrenzyVisual();
    }

    public void OnAbilityValue(int rawValue, TargetType targetType)
    {
        if (self == null || !self.IsAlive()) return;

        int maxHp = self.GetMaxHealth();
        int curHp = self.GetCurrentHealth();
        float missing = maxHp > 0 ? 1f - (float)curHp / maxHp : 0f;
        float mult = damageMultiplierByMissingHp.Evaluate(Mathf.Clamp01(missing));

        int finalDamage = Mathf.RoundToInt(rawValue * mult);

        self.AbilityHappened(finalDamage, targetType);

        int drain = Mathf.Max(1, Mathf.RoundToInt(maxHp * hpCostPercentOfMax));
        self.ReceiveAbilityValue(-drain);

        int healAmount = Mathf.RoundToInt(Mathf.Abs(finalDamage) * healPercentOfDamage);
        if (healAmount > 0 && self.allyUnits != null)
        {
            for (int i = 0; i < self.allyUnits.Count; i++)
            {
                UnitController ally = self.allyUnits[i];
                if (ally == null || ally == self || !ally.IsAlive()) continue;

                ally.ReceiveAbilityValue(healAmount, true);
                SpawnHealVfx(ally);
            }
        }

        UpdateFrenzyVisual();
    }

    void SpawnHealVfx(UnitController ally)
    {
        if (allyHealVfxPrefab == null) return;

        Vector3 pos = ally.transform.position + healVfxLocalOffset;
        GameObject fx = Instantiate(allyHealVfxPrefab, pos, Quaternion.identity, ally.transform);
        if (healVfxLifetime > 0f)
            Destroy(fx, healVfxLifetime);
    }

    void UpdateFrenzyVisual()
    {
        if (self == null) return;
        int maxHp = self.GetMaxHealth();
        if (maxHp <= 0) { SetFrenzyActive(false); return; }

        float ratio = (float)self.GetCurrentHealth() / maxHp;
        SetFrenzyActive(self.IsAlive() && ratio <= frenzyHpThreshold);
    }

    void SetFrenzyActive(bool active)
    {
        if (active == m_FrenzyActive) return;
        m_FrenzyActive = active;

        if (active)
        {
            if (frenzyVfxPrefab == null) return;
            if (m_FrenzyInstance == null)
            {
                m_FrenzyInstance = Instantiate(frenzyVfxPrefab, transform);
                m_FrenzyInstance.transform.localPosition = frenzyVfxLocalOffset;
            }
            m_FrenzyInstance.SetActive(true);
        }
        else if (m_FrenzyInstance != null)
        {
            m_FrenzyInstance.SetActive(false);
        }
    }
}
