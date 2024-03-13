using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

[Serializable]
public class PhaseData {
    public int BossHealth;
    public List<BulletSpawner> BulletSpawners;
    public float CellDestructionCooldown;
    public float BossStaggerTime;

    public PhaseData() {
        BulletSpawners = new();
        BossHealth = 100;
        CellDestructionCooldown = 3.0f;
        BossStaggerTime = 5.0f;
    }
}

public class BossController : Singleton<BossController> {
    [NonSerialized] public EntityStats Stats;
    [NonNullField] public Animator Animator;

    [NonNullField] public PostProcessOutline PostProcessOutlineRenderFeature;
    [NonNullField] public MeshRenderer BlackHoleRenderer;
    private Material _blackHoleMaterialInstance;
    [NonNullField] public MeshRenderer ShieldRenderer;
    private Material _shieldMaterialInstance;
    [NonNullField] public Animator ShieldAnimator;
    [NonNullField] public MeshRenderer BossMechRenderer;
    private Material _bossMechMaterialInstance;

    public List<GameObject> ThrusterObjects;

    public bool IsLocomotionEnabled = true;
    public bool IsAttackingEnabled = true;
    public float SlowMovementSpeed = 5.0f;
    public float FastMovementSpeed = 5.0f;
    public float IdleMaxMovementSpeed = 5.0f;
    public float OptimalDistanceToPlayer = 30.0f;
    public float ReturnToCenterThreshold = 150.0f;
    public float RetreatVectorBias = 1.0f;
    public int StaggerDamageThreshold = 50;
    public int MaxShieldHealth = 10;
    public float ShieldRecoveryTime = 1.0f;
    public int PlayerProjectileDamageToBossWhileStaggered = 5;
    public int PlayerSwordDamageToBoss = 25;

    private int MaxBossLives = 3;
    private int CurrentBossLives = 3;
    public event EventHandler<int> OnBossLivesChanged;

    // public List<BulletSpawner> BulletSpawners;
    public BulletSpawner spawner;
    private Transform _playerTarget = null;
    private Quaternion _targetRotation = Quaternion.identity;
    private Vector3 _targetPosition = Vector3.zero;
    private Vector3 ArenaCenter = Vector3.zero;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    public AnimationCurve IdleMovementCurve;
    public float IdleMovementCurveSampleSpeed = 0.1f;
    public AnimationCurve MovementSpeedScaleCurve;

    // Attacks
    public float InitialAttackCooldown = 10.0f;
    public float AttackCooldown = 5.0f;
    private float _attackCooldownCountdown = 0.0f;
    private bool _isAttacking = false;

    public List<PhaseData> BossPhaseData = new();
    private PhaseData _currentBossPhaseData;
    private List<BulletSpawner> _bulletSpawnersBag = new();
    private int _currentPhase = 0;
    private BulletSpawner _currentBulletSpawner = null;
    private int _shieldHealth;
    private bool _isIndestructible = false;
    private float _idleTime = 0;
    private float _restoreActionsCountdown = 0.0f;
    private int _damageTakenWhileStaggered = 0;
    private Coroutine _restoreShieldCoroutine = null;
    private bool _isProcessingEnabled = false;


    public void FireMissiles() {
        SetBossLives(0);

        // RestoreShield();
        // spawner.Play();

        // Quaternion verticalRotation = Quaternion.LookRotation(this.transform.up, -this.transform.forward);
        // Projectile missile = ProjectileController.Instance.SpawnMissile(ProjectileController.Owner.Enemy,
        //     this.transform.position,
        //     verticalRotation, 5.0f);
        //
        // // TODO: Do some smarter logic with the targeting.
        // // The targeting system should aim towards the player's movement based on how fast the character is moving.
        // StartCoroutine(MissileSecondStage(missile, PlayerManager.Instance.PlayerController.PlayerModel.transform));
    }

    IEnumerator MissileSecondStage(Projectile missile, Transform trackedTarget) {
        yield return new WaitForSeconds(1.0f);

        missile.TrackedTarget = trackedTarget;
        // missile.velocity = missile.velocity * 5;
        missile.Acceleration = 2.0f;
    }

    protected override void Awake() {
        base.Awake();
        Stats = GetComponent<EntityStats>();
        Stats.NotifyHealthChanged();
        Stats.OnDamageTaken += OnDamageTaken;

        _blackHoleMaterialInstance = BlackHoleRenderer.material;
        _shieldMaterialInstance = ShieldRenderer.material;
        _bossMechMaterialInstance = BossMechRenderer.sharedMaterial;
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        IdleMovementCurve.postWrapMode = WrapMode.Loop;
        IdleMovementCurve.preWrapMode = WrapMode.Loop;
        Animator.keepAnimatorStateOnDisable = true;
    }

    public void Reset() {
        ShieldAnimator.SetBool("IsBroken", false);
        ShieldAnimator.SetBool("IsImmune", false);
        Animator.SetBool("IsDead", false);
        _bossMechMaterialInstance.SetFloat("_DissolveTime", 0);
        Material postProcessOutlineMaterial = PostProcessOutlineRenderFeature.GetPostProcessMaterial();
        postProcessOutlineMaterial.SetFloat("_BlendTime", 0);
        postProcessOutlineMaterial.SetFloat("_BlendTime2", 0);
        ToggleThrusters(true);


        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        _attackCooldownCountdown = InitialAttackCooldown;
        _isAttacking = false;
        _shieldHealth = MaxShieldHealth;
        _isIndestructible = false;
        _isProcessingEnabled = true;
        SetBossLives(MaxBossLives);
        Stats.Reset();
        RestoreShield(ShieldRecoveryTime);
        IsAttackingEnabled = true;
        IsLocomotionEnabled = true;
        if (_currentBulletSpawner != null) {
            _currentBulletSpawner.StopAll();
            _currentBulletSpawner = null;
        }
    }

    private float _baseHealthPercent = 0;
    private bool _isHealing = false;
    private float _healingTimer = 0.0f;

    public void SetHealingPhase(bool isHealing) {
        _isHealing = isHealing;
        if (isHealing) {
            StopDoingStuff();
            _baseHealthPercent = Stats.GetHealthPercentage();
            _healingTimer = 0.0f;

            // We'll heal over time in the update loop.
            SetImmunity(true);
            ShieldAnimator.SetBool("IsBroken", false);
            // Animate
            Material postProcessOutlineMaterial = PostProcessOutlineRenderFeature.GetPostProcessMaterial();
            postProcessOutlineMaterial.DOFloat(1, "_BlendTime2", 1.0f)
                .SetLoops(4, LoopType.Yoyo) // Loop the animation 4 times: 0->1, 1->0, 0->1, 1->0
                .SetEase(Ease.InOutSine); // Use a linear ease to keep the transition smooth and consistent

            // Restore Shield
            _shieldHealth = MaxShieldHealth;
            _shieldMaterialInstance.SetFloat("_BlendTime", 0);
            ShieldAnimator.SetBool("IsBroken", false);
            ShieldAnimator.SetBool("IsHealing", true);
            Animator.SetBool("IsDead", false);
            Animator.SetBool("IsHealing", true);
        } else {
            ShieldAnimator.SetBool("IsHealing", false);
            Animator.SetBool("IsHealing", false);
            // Healing has stopped, we can do stuff again
            StartDoingStuff();
            SetImmunity(false);

            _attackCooldownCountdown = InitialAttackCooldown;
        }
    }

    private void Update() {
        // HACK: This is pretty bad to put here, but whatever lol
        float healingPhaseLength = GameLifecycleManager.Instance.HealingPhaseLength;
        if (_isHealing && _healingTimer < healingPhaseLength) {
            float healingT = _healingTimer / GameLifecycleManager.Instance.HealingPhaseLength;
            float missingHealthPercentage = Mathf.Lerp(0, 1 - _baseHealthPercent, healingT);
            Stats.SetHealthToPercentage(_baseHealthPercent + missingHealthPercentage);
            _healingTimer += Time.deltaTime;
        } else if (_isHealing && _healingTimer >= healingPhaseLength) {
            SetHealingPhase(false);
            Stats.SetHealthToPercentage(1);
        } else if (_isHealing) {
            // We still want the boss to look at the player
            Vector3 bossToTargetVector = _playerTarget.position - transform.position;
            bossToTargetVector.y = 0;
            Vector3 lookVector = bossToTargetVector;
            lookVector = lookVector.normalized;
            _targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);
        }

        // If the game is over, don't do anything
        if (!GameLifecycleManager.Instance.IsGamePlaying() || !_isProcessingEnabled) {
            return;
        }

        // If we're immune, this loses the immunity after a period of time.
        if (_restoreActionsCountdown <= 0.0f && _isIndestructible) {
            RestoreActions();
        } else {
            _restoreActionsCountdown -= Time.deltaTime;
        }

        if (IsLocomotionEnabled) {
            HandleLocomotion();
        }

        if (IsAttackingEnabled && !_isAttacking) {
            HandleAttack();
        }
    }

    private void Start() {
        _playerTarget = PlayerManager.Instance.PlayerController.EnemyAimTargetLocation;

        if (BossPhaseData.Count == 0) {
            Debug.LogError("[BossController] Boss Phase data is not set!");
        }

        MaxBossLives = BossPhaseData.Count;

        Reset();
    }

    private void ToggleThrusters(bool isEnabled) {
        foreach (GameObject thrusterObject in ThrusterObjects) {
            thrusterObject.SetActive(isEnabled);
        }
    }

    private void OnDamageTaken(object sender, float damageTaken) {
        PlayHitEffect();

        // If our shield is at 0, when we've taken over a cetain amount of damage. Instantly get up.
        if (_shieldHealth == 0) {
            _damageTakenWhileStaggered += Mathf.RoundToInt(damageTaken);

            if (_damageTakenWhileStaggered >= StaggerDamageThreshold) {
                if (_restoreShieldCoroutine != null) {
                    StopCoroutine(_restoreShieldCoroutine);
                    _restoreShieldCoroutine = null;
                }

                StartCoroutine(RestoreShieldAfterDelay(ShieldRecoveryTime));
            }
        }

        if (Stats.CurrentHealth <= 0) {
            ConsumeBossLife();
        }
    }

    private TweenerCore<Single, Single, FloatOptions> _shieldMaterialTween = null;

    public void PlayHitEffect() {
        float hitAnimationTime = 0.25f;
        _blackHoleMaterialInstance.DOFloat(1, "_BlendTime", hitAnimationTime).OnComplete(
            () => {
                _blackHoleMaterialInstance.DOFloat(0, "_BlendTime", hitAnimationTime);
            });
        Material postProcessOutlineMaterial = PostProcessOutlineRenderFeature.GetPostProcessMaterial();
        postProcessOutlineMaterial.DOFloat(1, "_BlendTime", hitAnimationTime).OnComplete(
            () => {
                postProcessOutlineMaterial.DOFloat(0, "_BlendTime", hitAnimationTime);
            });
    }

    public void PlayDissolveAnimation(float durationSec) {
        // Disable the thrusters.
        ToggleThrusters(false);

        // Fade away the boss.
        _bossMechMaterialInstance.DOFloat(1, "_DissolveTime", durationSec).OnComplete(
            () => {
                // Make it inactive? I guess it doesn't matter.
            });

        // TODO: Fade the outline as well
        // Material postProcessOutlineMaterial = PostProcessOutlineRenderFeature.GetPostProcessMaterial();
        // postProcessOutlineMaterial.DOFloat(1, "_BlendTime", hitAnimationTime).OnComplete(
        //     () => {
        //         postProcessOutlineMaterial.DOFloat(0, "_BlendTime", hitAnimationTime);
        //     });
    }

    public void PlayIndestructibleHitEffect() {
        // TODO: Play some special animation?
    }

    public void ApplySwordDamage() {
        if (_isIndestructible) {
            PlayIndestructibleHitEffect();
            return;
        }

        Stats.ApplyDamage(PlayerSwordDamageToBoss);
    }

    public void ApplyShieldDamage() {
        if (_isIndestructible) {
            PlayIndestructibleHitEffect();
            return;
        }

        if (_shieldHealth == 0) {
            // If the shield was already broken, then apply some damage.
            Stats.ApplyDamage(PlayerProjectileDamageToBossWhileStaggered);
        } else {
            _shieldHealth -= 1;

            ShieldAnimator.SetTrigger("Hit");
            // Multiply by 2 to accentuate the color and transition faster.
            float shieldT = (1 - _shieldHealth / (float)MaxShieldHealth) * 2;
            _shieldMaterialInstance.SetFloat("_BlendTime", shieldT);

            if (_shieldHealth <= 0) {
                BreakShield();
            }
        }
    }

    private void BreakShield() {
        ShieldAnimator.SetBool("IsBroken", true);
        Animator.SetBool("IsDead", true);

        // Disable boss from doing anything
        IsLocomotionEnabled = false;
        IsAttackingEnabled = false;
        if (_currentBulletSpawner != null) {
            _currentBulletSpawner.StopAll();
        }

        // Shield will restore after some time, or when the damage threshold is exceeded.
        _damageTakenWhileStaggered = 0;
        _restoreShieldCoroutine = StartCoroutine(RestoreShieldAfterDelay(_currentBossPhaseData.BossStaggerTime));
    }

    private IEnumerator RestoreShieldAfterDelay(float delaySeconds) {
        yield return new WaitForSeconds(delaySeconds);

        if (_isProcessingEnabled) {
            RestoreShield(ShieldRecoveryTime);
        }
    }

    private void RestoreShield(float immunityTime) {
        _shieldHealth = MaxShieldHealth;
        _shieldMaterialInstance.SetFloat("_BlendTime", 0);
        ShieldAnimator.SetBool("IsBroken", false);
        Animator.SetBool("IsDead", false);

        // Restore State. Boss is immune while this is happening.
        SetImmunity(true);
        RestoreActionsAfterDelay(immunityTime);
        _attackCooldownCountdown = InitialAttackCooldown;
    }

    private void RestoreActionsAfterDelay(float immunityTime) {
        _restoreActionsCountdown = immunityTime;
    }

    private void RestoreActions() {
        IsAttackingEnabled = true;
        IsLocomotionEnabled = true;
        SetImmunity(false);
    }

    private void SetImmunity(bool isImmune) {
        // TODO: Maybe shield should be a special color here?
        _isIndestructible = isImmune;
        ShieldAnimator.SetBool("IsImmune", _isIndestructible);
    }

    private void OnSpawningStopped() {
        _isAttacking = false;
        _currentBulletSpawner = null;
    }

    public void StopDoingStuff() {
        if (_restoreShieldCoroutine != null) {
            StopCoroutine(_restoreShieldCoroutine);
            _restoreShieldCoroutine = null;
        }

        if (_currentBulletSpawner != null) {
            _currentBulletSpawner.StopAll();
        }

        IsLocomotionEnabled = false;
        IsAttackingEnabled = false;
        _isIndestructible = true;
        _isProcessingEnabled = false;
    }

    public void StartDoingStuff() {
        IsLocomotionEnabled = true;
        IsAttackingEnabled = true;
        _isIndestructible = false;
        _isProcessingEnabled = true;
    }

    private void SetBossLives(int bossLives) {
        CurrentBossLives = Mathf.Clamp(bossLives, 0, MaxBossLives);
        if (CurrentBossLives == 0) {
            // Mark the boss as dead.
            Animator.SetBool("IsDead", true);
            ShieldAnimator.SetBool("IsBroken", true);
            ShieldAnimator.SetBool("IsImmune", false);

            // Player Wins!
            GameLifecycleManager.Instance.WinGame();
        } else {
            _currentPhase = MaxBossLives - CurrentBossLives;
            _currentBossPhaseData = BossPhaseData[_currentPhase];
            _bulletSpawnersBag.Clear();
            PanelsController.Instance.CellDestructionCooldown = _currentBossPhaseData.CellDestructionCooldown;
            if (!PanelsController.Instance.IsDestroyingLevel()) {
                PanelsController.Instance.StartDestroyingLevel();
            }

            Stats.MaxHealth = _currentBossPhaseData.BossHealth;
        }

        OnBossLivesChanged?.Invoke(this, CurrentBossLives);
    }

    private void ConsumeBossLife() {
        SetBossLives(CurrentBossLives - 1);
        if (CurrentBossLives > 0) {
            // Begin healing phase for self. Player can still move around.
            SetHealingPhase(true);
        }
    }

    private void HandleAttack() {
        // Basically every once in a while (lets say every COOLDOWN seconds), we will pick a random attack.
        // An attack can be by itself, or it can be changed together with a series of attacks.
        // While the boss is attacking, it cannot move.
        // You could say this is Choreographed.
        if (_attackCooldownCountdown <= 0.0f) {
            // Execute an attack.
            _attackCooldownCountdown = AttackCooldown;
            _isAttacking = true;
            PerformRandomAttack();
        } else {
            _attackCooldownCountdown -= Time.deltaTime;
        }
    }

    private void PerformRandomAttack() {
        // What is an attack? Perhaps we should be using a timeline for this?
        // Maybe not for now. Right now they are just a bunch of BulletSpawners. And each attack is we do 
        // BulletSpawner.Play(). 
        // When BulletSpawner stops, then the attack has ended.
        // TODO: Sometimes we will need to forcibly stop the bullet spawners. AKA cancel and stop the attack.
        // For each phase, we will have a list of bullet spawners. We will pick from them randomly "bag" style.
        // As in, add them to a list and randomly pick from the list. If we run out, we restock the bag.
        if (_bulletSpawnersBag.Count == 0) {
            // Reload the bag
            foreach (BulletSpawner spawner in _currentBossPhaseData.BulletSpawners) {
                _bulletSpawnersBag.Add(spawner);
            }
        }

        // Pick a random spawner
        int spawnerIdx = UnityEngine.Random.Range(0, _bulletSpawnersBag.Count);
        _currentBulletSpawner = _bulletSpawnersBag[spawnerIdx];

        // Unsubcribe first just in case we've already subscribed (duplicate reference).
        _currentBulletSpawner.OnSpawningStopped -= OnSpawningStopped;
        _currentBulletSpawner.OnSpawningStopped += OnSpawningStopped;

        // Remove it so it's not picked again
        _bulletSpawnersBag.RemoveAt(spawnerIdx);
        _currentBulletSpawner.Play();
    }

    // TODO: Could try improving this by making the behavior always where the boss tries to the center of the arena
    // between it and you. But alas no time!.
    private void HandleLocomotion() {
        Vector3 bossToTargetVector = _playerTarget.position - transform.position;
        bossToTargetVector.y = 0;
        Vector3 bossToCenterVector = ArenaCenter - transform.position;
        Vector2 playerTargetPosXZ = new Vector2(_playerTarget.position.x, _playerTarget.position.z);
        float distanceToPlayer = bossToTargetVector.magnitude;
        float distanceToCenter = bossToCenterVector.magnitude;

        float movementSpeed = 0;

        // Whether the player is on the closer or far side of the arena
        Vector2 bossToCenterOrthogonalVector = new Vector2(bossToCenterVector.z, -bossToCenterVector.x);
        int closeFarSideOfCenter = PointSideOfLine(Vector2.zero, bossToCenterOrthogonalVector,
            new Vector2(_playerTarget.position.x, _playerTarget.position.z));
        // ReactUnityBridge.Instance.UpdateDebugString("closeFarSideOfCenter",
        //     closeFarSideOfCenter.ToString());
        VectorDebug.Instance.DrawDebugVector("BossToCenterOrthogonalVector",
            new Vector3(bossToCenterOrthogonalVector.x, 0, bossToCenterOrthogonalVector.y), ArenaCenter,
            Color.magenta);

        // Compute the shortest vector from the Player to bossToCenterVector.
        Vector2 playerToBossToCenterLineVector = PointToLineVector(Vector2.zero,
            new Vector2(transform.position.x, transform.position.z),
            playerTargetPosXZ);

        // Aim towards player
        Vector3 lookVector = bossToTargetVector;
        lookVector = lookVector.normalized;
        _targetRotation = Quaternion.LookRotation(lookVector, Vector3.up);

        // ===== Compute Target Position =====

        float playerDistanceToCenter = (ArenaCenter - _playerTarget.position).magnitude;
        bool bossIsCloserToCenterThanPlayer = distanceToCenter < playerDistanceToCenter;

        // ReactUnityBridge.Instance.UpdateDebugString("IsGreaterThanOptimalDistance",
        //     (distanceToPlayer > OptimalDistanceToPlayer).ToString());
        // VectorDebug.Instance.DrawDebugVector("bossToCenterVector", bossToCenterVector, transform.position,
        //     Color.magenta);
        int behaviorTreeCase = -1;
        // if (distanceToPlayer < CriticalThreshold) {
        // Case 0: Boss is critically close to player. Get out
        // }
        if (distanceToPlayer > OptimalDistanceToPlayer) {
            if (distanceToCenter < ReturnToCenterThreshold) {
                // Case 0: Boss is far from the player but doesn't feel like it needs to return to the center.
                // It will move to the left/right according to a curve.

                // Idle
                behaviorTreeCase = 5;

                // Move randomly left/right around the player.
                float velocityCurveValue = IdleMovementCurve.Evaluate(_idleTime * IdleMovementCurveSampleSpeed);
                Vector3 velocity = Vector3.zero;
                if (velocityCurveValue < 0) {
                    velocity = transform.rotation * Vector3.left;
                } else {
                    velocity = transform.rotation * Vector3.right;
                }

                _targetPosition = transform.position + velocity;
                movementSpeed = Mathf.Abs(velocityCurveValue) * IdleMaxMovementSpeed;

                // Increment the time we've spent idle.
                _idleTime += Time.deltaTime;
            } else {
                _idleTime = 0;

                // Case 1: Boss is far from the player
                // Two more cases depending on if the player is on the boss' side of the center or not.
                if (closeFarSideOfCenter > 0) {
                    // Case 1.A: Player is on the other side of the arena.
                    // We'll just move towards the center.
                    _targetPosition = ArenaCenter;
                    behaviorTreeCase = 0;
                } else {
                    // Two more cases depending on the length
                    if (playerToBossToCenterLineVector.magnitude > OptimalDistanceToPlayer) {
                        // Case 1.B: which is same as case A. Player is too far for this to matter.
                        _targetPosition = ArenaCenter;
                        behaviorTreeCase = 1;
                    } else {
                        // Case 1.C: We need to navigate away from the player as well as make progress towards the center.
                        playerToBossToCenterLineVector = playerToBossToCenterLineVector.normalized;
                        Vector3 playerToBossToCenterLineVec3 = new Vector3(playerToBossToCenterLineVector.x, 0,
                            playerToBossToCenterLineVector.y);
                        _targetPosition = playerToBossToCenterLineVec3 * OptimalDistanceToPlayer;
                        behaviorTreeCase = 2;
                    }
                }

                // Adjust the speed based on the distance to the center.
                // If distance is larger, we want to be faster.
                // Curve is defined as 0 = largest, 1 = smallest.
                float t = 1 - distanceToCenter / 300;
                float adjustedT = MovementSpeedScaleCurve.Evaluate(t);
                movementSpeed = Mathf.Lerp(SlowMovementSpeed, FastMovementSpeed, adjustedT);
            }
        } else {
            _idleTime = 0;

            // Case 2: Two more cases depending on if the player is on the boss' side of the center or not.
            // We will need to back up to get away from the player, but also be mindful of trying to go to the center.
            // The theory here is that there are two possible vectors we can take.
            // If we want to go back to the optimum distance ASAP, then we'd want to take the retreat vector directly away from player.
            // If we were farther away (almost at the optimal distance), then we would want to go left or right to possibly open up a path to the center.

            Vector3 retreatVector = -bossToTargetVector.normalized;
            VectorDebug.Instance.DrawDebugVector("retreatVector",
                retreatVector, transform.position,
                Color.green);

            float distanceT = Mathf.Clamp(distanceToPlayer / OptimalDistanceToPlayer, 0, 1);

            if (closeFarSideOfCenter > 0 || bossIsCloserToCenterThanPlayer) {
                // Case 2.A: Player is on the other side of the arena.
                // We'll just move backwards away from the player. The center is between us and the player.
                // HACK: bossIsCloserToCenterThanPlayer check is a hack to get around a bug.
                // There is some case where this is true when it shouldn't be and the boss is closer to the center
                // than the player is. If that's the case, also do this.
                _targetPosition = transform.position + retreatVector;
                behaviorTreeCase = 3;
            } else {
                // Case 2.B: Player is on the close side of the arena.

                // Figure out which side of the center the player is on (left or right)
                Vector2 bossToCenterVector2 = new Vector2(bossToCenterVector.x, bossToCenterVector.z);
                int sideOfCenter = PointSideOfLine(Vector2.zero, bossToCenterVector2,
                    playerTargetPosXZ);
                // ReactUnityBridge.Instance.UpdateDebugString("sideOfCenter",
                //     sideOfCenter.ToString());


                // This vector would take us on a better path towards reaching the center.
                Vector3 tangentialVector = Vector3.zero;
                if (sideOfCenter < 0) {
                    // Left of center
                    tangentialVector = transform.rotation * Vector3.left;
                } else {
                    // On or right of center
                    tangentialVector = transform.rotation * Vector3.right;
                }

                VectorDebug.Instance.DrawDebugVector("TangentialVector",
                    tangentialVector, transform.position,
                    Color.magenta);

                // Based on the idea that if the boss is right next to the player, it would want to take the retreatVector.
                // And if it was far away, it'd take the sideways tangential vector, we will LERP between them.
                // ReactUnityBridge.Instance.UpdateDebugString("Distance Ratio",
                //     distanceT.ToString());
                // We can bias towards the retreat vector by making it larger.
                Vector3 targetVector = Vector3.Slerp(retreatVector * RetreatVectorBias, tangentialVector, distanceT);
                behaviorTreeCase = 4;

                _targetPosition = transform.position + targetVector;
                VectorDebug.Instance.DrawDebugVector("TargetVector",
                    _targetPosition, transform.position,
                    Color.red);
            }

            // Adjust the speed based on the distance to the player.
            // If distance is smaller, we want to be faster.
            // Curve is defined as 0 = largest, 1 = smallest.
            float adjustedT = MovementSpeedScaleCurve.Evaluate(distanceT);
            movementSpeed = Mathf.Lerp(SlowMovementSpeed, FastMovementSpeed, adjustedT);
        }

        // ===== Apply Movement =====
        //  TODO: If distance is too far, then dash?
        // Move towards target position
        Vector3 movementVector = _targetPosition - transform.position;
        Vector3 absoluteMovementVector = movementVector.normalized * (movementSpeed * Time.deltaTime);
        transform.position += absoluteMovementVector;
        if ((_targetPosition - transform.position).magnitude < 0.1f) {
            _targetPosition = absoluteMovementVector;
        }

        VectorDebug.Instance.DrawDebugVector("absoluteMovementVector",
            absoluteMovementVector, transform.position,
            Color.red);

        // ReactUnityBridge.Instance.UpdateDebugString("Boss Movement Speed",
        //     movementSpeed.ToString());
        // ReactUnityBridge.Instance.UpdateDebugString("Behavior Tree Case",
        //     behaviorTreeCase.ToString());
        // ReactUnityBridge.Instance.UpdateDebugString("IdleTime",
        //     _idleTime.ToString());

        // ===== Apply Rotation =====
        // Rotate towards player gradually
        // transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, TurningSpeed * Time.fixedDeltaTime);
        transform.rotation = _targetRotation;

        // ===== Animations =====
        Vector3 relativeMovementVector = transform.rotation * absoluteMovementVector;
        Animator.SetBool("IsMoving", absoluteMovementVector.magnitude > 0);
        Animator.SetBool("IsMovingRight", relativeMovementVector.x > 0);
        Animator.SetBool("IsMovingLeft", relativeMovementVector.x < 0);
        Animator.SetBool("IsMovingForward", relativeMovementVector.z > 0);
        Animator.SetBool("IsMovingBackward", relativeMovementVector.z < 0);
    }

    private int PointSideOfLine(Vector2 A, Vector2 B, Vector2 P) {
        float determinant = (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);

        if (determinant > 0) return 1; // P is on the left side of the line
        if (determinant < 0) return -1; // P is on the right side of the line

        return 0; // P is on the line
    }

    public Vector2 PointToLineVector(Vector2 A, Vector2 B, Vector2 P) {
        // Line's direction vector
        Vector2 lineDir = B - A;
        lineDir.Normalize();

        // Vector from A to P
        Vector2 AP = P - A;

        // Project AP onto the line's direction vector to find the magnitude of the projection
        float magnitude = Vector2.Dot(AP, lineDir);

        // Get the point Q on the line and construct the vector PQ
        Vector2 Q = A + lineDir * magnitude;
        Vector2 PQ = Q - P;

        // Return the PQ vector, which is orthogonal to AB and points from P to the line
        return PQ;
    }
}
