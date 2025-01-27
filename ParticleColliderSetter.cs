using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleColliderSetter : MonoBehaviour
{
    private ParticleSystem particleSystem;
    private List<CircleCollider2D> colliders = new List<CircleCollider2D>();
    private ParticleSystem.Particle[] particles;

    // 対象とするパーティクルシステムの参照
    public ParticleSystem targetParticleSystem;

    // コライダーのサイズ倍率 (パーティクルサイズに対する倍率を設定)
    public float colliderSizeMultiplier = 0.5f;

    // コライダーの間引き率 (1で全てのパーティクルにコライダー生成、2で半分に間引き)
    public int colliderSkipRate = 1;

    // コライダーのトリガーのOn/Off設定
    public bool colliderIsTrigger = false;

    // コライダーのタグとレイヤーをエディターで設定できるようにする
    public string colliderTag = "Untagged";
    public int colliderLayer = 0; // レイヤー番号 (0〜31の範囲)

    // ワールド空間のシミュレーションに対応するオプション
    public bool useWorldSpaceSimulation = false;

    // 親を設定しないオプション
    public bool nulParent = false;



    // コピーするプレイヤーステータス
    public CharacterStats playerStats;

    void Start()
    {
        if (targetParticleSystem == null)
        {
            Debug.LogError("ターゲットのパーティクルシステムが指定されていません。");
            return;
        }

        particleSystem = targetParticleSystem;
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
    }

    void FixedUpdate()
    {
        if (particleSystem == null)
        {
            return;
        }

        int particleCount = particleSystem.GetParticles(particles);

        // 実際に必要なコライダー数を計算
        int requiredColliders = Mathf.CeilToInt((float)particleCount / colliderSkipRate);

        // パーティクル数に応じてコライダーの数を調整
        AdjustColliderCount(requiredColliders);

        for (int i = 0, j = 0; i < particleCount; i += colliderSkipRate, j++)
        {
            if (j >= colliders.Count) break;

            var particle = particles[i];
            var collider = colliders[j];

            // パーティクルのローカル位置またはワールド位置を取得
            Vector3 particlePosition = useWorldSpaceSimulation
                ? particle.position // ワールド空間の場合
                : particleSystem.transform.TransformPoint(particle.position); // ローカル空間の場合

            collider.transform.position = particlePosition;

            // パーティクルサイズを基にコライダーの半径を設定
            collider.radius = particle.GetCurrentSize(particleSystem) * colliderSizeMultiplier;

            // コライダーのトリガー設定を適用
            collider.isTrigger = colliderIsTrigger;
        }
    }

    // コライダーの数をパーティクル数に応じて調整
    void AdjustColliderCount(int count)
    {
        // コライダーが不足している場合、新しいコライダーを追加
        while (colliders.Count < count)
        {
            var newCollider = new GameObject("ParticleCircleCollider").AddComponent<CircleCollider2D>();

            // 新しいコライダーのタグとレイヤーを設定
            newCollider.tag = colliderTag;
            newCollider.gameObject.layer = colliderLayer; // レイヤー番号で設定



            // 親の設定を `nulParent` オプションで制御
            if (!nulParent == null)
            {
                newCollider.transform.SetParent(transform);
            }

            colliders.Add(newCollider);

            // 非アクティブ時に削除するためのコールバックを追加
            var colliderObject = newCollider.gameObject;
            colliderObject.AddComponent<ColliderCleaner>().Initialize(colliders, colliderObject);
        }

        // 余分なコライダーを非アクティブ化
        for (int i = count; i < colliders.Count; i++)
        {
            colliders[i].gameObject.SetActive(false);
        }

        // 必要なコライダーをアクティブ化
        for (int i = 0; i < count; i++)
        {
            colliders[i].gameObject.SetActive(true);
        }
    }
}

// 非アクティブ時にリストから削除し、自動的にオブジェクトを破棄するクラス
public class ColliderCleaner : MonoBehaviour
{
    private List<CircleCollider2D> colliderList;
    private GameObject colliderObject;

    public void Initialize(List<CircleCollider2D> colliders, GameObject colliderObj)
    {
        colliderList = colliders;
        colliderObject = colliderObj;
    }

    void OnDisable()
    {
        if (colliderList != null && colliderObject != null)
        {
            colliderList.Remove(colliderObject.GetComponent<CircleCollider2D>());
            Destroy(colliderObject);
        }
    }
}
