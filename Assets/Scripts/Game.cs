using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour
{
    [Header("Resources")]
    public AnimationCurve SuccEffect;
    public AnimationCurve GameOverEffect;
    public AnimationCurve GameOverScreenshake;
    public GameObject BitPrefab;
    public GameObject PlusOnePrefab;
    public AudioClip Blip1;
    public AudioClip Blip2;
    public AudioClip MissBlip;
    public AudioClip Shatter;

    [Space]
    public AudioSource AudioSource;

    [Header("A")]
    public Transform RootA;
    public BoxCollider2D HomeA;
    public Transform SpawnPointA;
    public BoxCollider2D PressZoneA;
    public Transform ReserveRootA;

    [Header("B")]
    public Transform RootB;
    public BoxCollider2D HomeB;
    public Transform SpawnPointB;
    public BoxCollider2D PressZoneB;
    public Transform ReserveRootB;

    [Header("GameOver")]
    public SpriteRenderer BlackOverlay;
    public TMPro.TextMeshPro GameOverText;

    private bool _goingRight = true;
    private List<GameObject> _bits = new List<GameObject>();

    private int _reserveBitCountA = 1;
    private int _reserveBitCountB = 0;
    private float _bitSpeed = 2.5f;
    private bool _gameOver = false;
    private bool _canSend = true;

    private void Start()
    {
        UpdateReserveRoot(ReserveRootA, _reserveBitCountA);
        UpdateReserveRoot(ReserveRootB, _reserveBitCountB);

        StartCoroutine(SwitchHomesEffect(RootA, RootB));
    }

    private void Update()
    {
        if (_gameOver)
        {
            if (Input.GetButtonDown("Restart"))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            }

            return;
        }

        if (_goingRight)
        {
            Move("XboxA", "XboxB", Vector3.right, HomeB, SpawnPointA, PressZoneB,
                RootA, RootB,
                Blip1, Blip2,
                ReserveRootA, ReserveRootB,
                ref _reserveBitCountA, ref _reserveBitCountB);
        }
        else
        {
            Move("XboxB", "XboxA", Vector3.left, HomeA, SpawnPointB, PressZoneA, 
                RootB, RootA,
                Blip2, Blip1,
                ReserveRootB, ReserveRootA,
                ref _reserveBitCountB, ref _reserveBitCountA);
        }
    }

    private void Move(string sendKey, string recvKey, Vector3 dir, BoxCollider2D targetHome, Transform spawnPoint, BoxCollider2D pressZone,
        Transform sendRoot, Transform recvRoot,
        AudioClip sendSfx, AudioClip recvSfx,
        Transform senderReserveRoot, Transform recvReserveRoot,
        ref int sendReserv, ref int recvReserv)
    {
        if (Input.GetButtonDown(sendKey) && _canSend && sendReserv > 0)
        {
            PlaySound(sendSfx);

            sendReserv--;
            UpdateReserveRoot(senderReserveRoot, sendReserv);
            var bit = Instantiate(BitPrefab, spawnPoint.position, Quaternion.identity);
            _bits.Add(bit);
        }

        foreach (var bit in _bits)
        {
            bit.transform.position += dir * _bitSpeed * Time.deltaTime;
            LineRenderer lr = bit.GetComponent<LineRenderer>();
            lr.SetPositions(new[]
            {
                bit.transform.position,
                bit.transform.position - dir * 0.5f
            });
        }

        List<GameObject> removedBits = new List<GameObject>();
        if (Input.GetButtonDown(recvKey))
        {
            PlaySound(recvSfx);

            bool succ = false;
            foreach (var bit in _bits)
            {
                if (pressZone.OverlapPoint(bit.transform.position))
                {
                    // Succ
                    removedBits.Add(bit);
                    succ = true;

                    recvReserv++;
                    UpdateReserveRoot(recvReserveRoot, recvReserv);
                    StartCoroutine(RunSuccEffectAt(bit.transform.position));
                }
            }
            if (!succ)
            {
                // Bosa basti
                PlaySound(MissBlip);

                if (recvReserv > 0)
                {
                    // Reduce reserve count
                    recvReserv = Mathf.Max(recvReserv - 1, 0);
                    Vector3 lastBitPos = UpdateReserveRoot(recvReserveRoot, recvReserv);
                    StartCoroutine(RunPlusOneEffectAt(lastBitPos, "-1"));
                }
            }
        }

        foreach (var bit in _bits)
        {
            if (targetHome.OverlapPoint(bit.transform.position))
            {
                // Girdi
                removedBits.Add(bit);
                RunGameOverEffects(targetHome, bit.transform.position);
            }
        }
        foreach (var bit in removedBits)
        {
            Destroy(bit);
        }
        _bits = _bits.Except(removedBits).ToList();

        if (sendReserv == 0 && _bits.Count == 0)
        {
            recvReserv++;

            sendRoot.localScale = Vector3.one;
            recvRoot.localScale = Vector3.one * 0.8f;

            Vector3 lastBitPos = UpdateReserveRoot(recvReserveRoot, recvReserv);
            StartCoroutine(RunPlusOneEffectAt(lastBitPos, "+1"));

            StartCoroutine(SwitchHomesEffect(recvRoot, sendRoot));
            StartCoroutine(StartTurnCooldown());

            _goingRight = !_goingRight;
        }
    }

    private IEnumerator SwitchHomesEffect(Transform toBack, Transform toFront)
    {
        const float duration = 0.5f;
        Vector3 bigScale = Vector3.one;
        Vector3 smallScale = Vector3.one * 0.8f;

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = SuccEffect.Evaluate(f / duration);
            toBack.transform.localScale = Vector3.Lerp(bigScale, smallScale, t);
            toFront.transform.localScale = Vector3.Lerp(smallScale, bigScale, t);
            yield return null;
        }

        toBack.transform.localScale = smallScale;
        toFront.transform.localScale = bigScale;
    }

    private IEnumerator StartTurnCooldown()
    {
        _canSend = false;
        yield return new WaitForSeconds(0.5f);
        _canSend = true;
    }

    #region Home Effects
    private Vector3 UpdateReserveRoot(Transform root, int reservCount)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        for (int i = 0; i < reservCount; i++)
        {
            StartCoroutine(RunSpawnEffectAt(root.transform.position + Vector3.down * i * 0.5f, root));
        }

        return root.transform.position + Vector3.down * reservCount * 0.5f;
    }

    private IEnumerator RunSpawnEffectAt(Vector3 initPos, Transform root)
    {
        const float duration = 0.05f;
        Vector3 startScale = BitPrefab.transform.localScale * 3;
        Vector3 targetScale = BitPrefab.transform.localScale;
        GameObject go = Instantiate(BitPrefab, initPos, Quaternion.identity);
        go.transform.SetParent(root, true);
        SpriteRenderer r = go.GetComponent<SpriteRenderer>();
        Color startColor = new Color(r.color.r, r.color.g, r.color.b, 0);
        Color targetColor = r.color;

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            if (go == null)
            {
                yield break;
            }

            float t = SuccEffect.Evaluate(f / duration);
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            r.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        go.transform.SetParent(root);
    }

    private IEnumerator RunSuccEffectAt(Vector3 initPos)
    {
        const float duration = 0.25f;
        Vector3 startScale = BitPrefab.transform.localScale;
        Vector3 targetScale = BitPrefab.transform.localScale * 2;
        GameObject go = Instantiate(BitPrefab, initPos, Quaternion.identity);

        SpriteRenderer r = go.GetComponent<SpriteRenderer>();
        Color startColor = r.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = SuccEffect.Evaluate(f / duration);
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            r.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        Destroy(go);
    }

    private IEnumerator RunPlusOneEffectAt(Vector3 initPos, string text)
    {
        const float duration = 1.25f;
        Vector3 endPos = initPos + Vector3.down * 0.5f;
        GameObject go = Instantiate(PlusOnePrefab, initPos, Quaternion.identity);
        TMPro.TextMeshPro r = go.GetComponent<TMPro.TextMeshPro>();
        r.text = text;
        Color startColor = r.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = SuccEffect.Evaluate(f / duration);
            go.transform.position = Vector3.Lerp(initPos, endPos, t);
            r.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        Destroy(go);
    }
    #endregion

    #region Game Over
    private void RunGameOverEffects(BoxCollider2D shatteredHome, Vector3 shatterPos)
    {
        _gameOver = true;
        PlaySound(Shatter);
        shatteredHome.transform.position += Vector3.forward * 3;
        shatteredHome.GetComponent<Explodable>().explode();
        StartCoroutine(WaitAndExplodeAt(shatterPos));
        StartCoroutine(RunGameOverVfx());
        StartCoroutine(RunGameOverScreenshake());
        UpdateReserveRoot(ReserveRootA, 0);
        UpdateReserveRoot(ReserveRootB, 0);
    }

    private IEnumerator RunGameOverVfx()
    {
        const float duration = 1f;

        GameOverText.gameObject.SetActive(true);
        BlackOverlay.gameObject.SetActive(true);

        GameOverText.text = _goingRight ? "LEFT WON" : "RIGHT WON";

        Color startColor = new Color(BlackOverlay.color.r, BlackOverlay.color.g, BlackOverlay.color.b, 0);
        Color targetColor = BlackOverlay.color;

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = GameOverEffect.Evaluate(f / duration);
            BlackOverlay.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
    }

    private IEnumerator RunGameOverScreenshake()
    {
        const float duration = 0.4f;
        const float force = 0.2f;

        Vector3 startPos = Camera.main.transform.position;
        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = GameOverScreenshake.Evaluate(f / duration);
            Camera.main.transform.position = startPos + ((Vector3)Random.insideUnitCircle).normalized * t * force;
            yield return null;
        }

        Camera.main.transform.position = startPos;
    }

    private IEnumerator WaitAndExplodeAt(Vector3 pos)
    {
        yield return new WaitForFixedUpdate();
        const float radius = 5;
        const float explosionForce = 100f;
        const float upliftModifier = 3f;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(pos, radius);

        foreach (Collider2D coll in colliders)
        {
            Rigidbody2D rb = coll.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.drag = 1f;
                AddExplosionForce(rb, explosionForce, transform.position, radius, upliftModifier);
            }
        }
    }

    private void AddExplosionForce(Rigidbody2D body, float explosionForce, Vector3 explosionPosition, float explosionRadius, float upliftModifier = 0)
    {
        var dir = (body.transform.position - explosionPosition);
        float wearoff = 1 - (dir.magnitude / explosionRadius);
        Vector3 baseForce = dir.normalized * explosionForce * wearoff;
        baseForce.z = 0;
        body.AddForce(baseForce);

        if (3 != 0)
        {
            float upliftWearoff = 1 - upliftModifier / explosionRadius;
            Vector3 upliftForce = Vector2.up * explosionForce * upliftWearoff;
            upliftForce.z = 0;
            body.AddForce(upliftForce);
        }

    }
    #endregion

    private void PlaySound(AudioClip clip)
    {
        AudioSource.PlayOneShot(clip);
    }
}
